Imports System.IO
Imports System.IO.Pipes
Imports System.Text
Imports System.Threading
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports VSAgent.Protocol.Messages

Public Class TransportPipeClient(Of TRequest, TResponse)
    Implements IDisposable

    Private ReadOnly _pipeName As String

    Private _pipe As NamedPipeClientStream
    Private _reader As StreamReader
    Private _writer As StreamWriter
    Private ReadOnly _writeLock As New SemaphoreSlim(1, 1)

    Private ReadOnly _pendingRequests As New Dictionary(Of String, TaskCompletionSource(Of TResponse))
    Private ReadOnly _pendingLock As New Object()

    Private _readertask As Task

    Public Event EventReceived(payload As JObject)

    Public Sub New(PipeName As String)
        _pipeName = PipeName
    End Sub

    Public Async Function ConnectAsync() As Task

        If _pipe IsNot Nothing AndAlso _pipe.IsConnected Then
            Return
        End If

        _pipe = New NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous)

        Await _pipe.ConnectAsync(5000)

        _reader = New StreamReader(
                _pipe,
                New UTF8Encoding(False),
                detectEncodingFromByteOrderMarks:=False,
                bufferSize:=4096,
                leaveOpen:=True)

        _writer = New StreamWriter(
                _pipe,
                New UTF8Encoding(False),
                bufferSize:=4096,
                leaveOpen:=True) With {
                .AutoFlush = True
            }

        _readerTask = ReadLoopAsync()

    End Function

    Public Async Function SendAsync(request As TRequest) As Task(Of TResponse)

        Dim requestId = Guid.NewGuid().ToString("N")

        Dim completion = New TaskCompletionSource(Of TResponse)()

        SyncLock _pendingLock
            _pendingRequests.Add(requestId, completion)
        End SyncLock

        Dim message As New TransportMessage With {
            .MessageType = "request",
            .RequestId = requestId,
            .Payload = JObject.FromObject(request)
        }

        Await WriteMessageAsync(message)

        Return Await completion.Task
    End Function

    Private Async Function ReadLoopAsync() As Task
        While _pipe IsNot Nothing AndAlso _pipe.IsConnected

            Dim line = Await _reader.ReadLineAsync()

            If line Is Nothing Then
                Exit While
            End If

            If String.IsNullOrWhiteSpace(line) Then
                Continue While
            End If

            Dim message = JsonConvert.DeserializeObject(Of TransportMessage)(line)

            If message Is Nothing Then
                Continue While
            End If

            Select Case message.MessageType
                Case "response"
                    HandleResponse(message)
                Case "event"
                    RaiseEvent EventReceived(message.Payload)
                Case Else
                    ' Unknown
            End Select
        End While
    End Function

    Private Sub HandleResponse(message As TransportMessage)
        Dim completion As TaskCompletionSource(Of TResponse) = Nothing

        SyncLock _pendingLock
            If _pendingRequests.TryGetValue(message.RequestId, completion) Then
                _pendingRequests.Remove(message.RequestId)
            End If
        End SyncLock

        If completion Is Nothing Then
            Return
        End If

        Dim response = message.Payload.ToObject(Of TResponse)()

        completion.SetResult(response)
    End Sub

    Private Async Function WriteMessageAsync(message As TransportMessage) As Task
        ' Only one write action!
        Await _writeLock.WaitAsync()
        Try

            Dim json = JsonConvert.SerializeObject(message)

            Await _writer.WriteLineAsync(json)
            Await _writer.FlushAsync()
        Finally
            _writeLock.Release()
        End Try
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            _writer?.Dispose()
        Catch
        End Try

        Try
            _reader?.Dispose()
        Catch
        End Try

        Try
            _pipe?.Dispose()
        Catch
        End Try
    End Sub
End Class
