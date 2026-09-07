Imports System.IO
Imports System.IO.Pipes
Imports System.Threading
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports VSAgent.Protocol
Imports VSAgent.Protocol.Messages

Public Class TransportPipeServer(Of TRequest, TResponse)
    Implements IDisposable

    Private ReadOnly _pipeName As String
    Private ReadOnly _handler As Func(Of TRequest, Task(Of TResponse))

    Private _reader As StreamReader = Nothing
    Private _writer As StreamWriter = Nothing
    Private ReadOnly _writeLock As New SemaphoreSlim(1, 1)

    Private ReadOnly _cancellationTokenSource As New CancellationTokenSource()
    Private _serverTask As Task

    Private ReadOnly _serverName As String

    Private disposedValue As Boolean

    Public Sub New(PipeName As String, Handler As Func(Of TRequest, Task(Of TResponse)), Servername As String)
        _pipeName = PipeName
        _handler = Handler
        _serverName = Servername
    End Sub

    Public Sub Start()
        Debug.WriteLine($"VSAgent: Starting server {_serverName}")
        If _serverTask IsNot Nothing Then
            Throw New InvalidOperationException("The VSAgent server has already been started.")
        End If

        Debug.WriteLine($"VSAgent: Run server {_serverName}")
        _serverTask = RunServerAsync(_cancellationTokenSource.Token)
    End Sub

    Private Async Function RunServerAsync(cancellationToken As CancellationToken) As Task
        While Not cancellationToken.IsCancellationRequested
            Try
                Await AcceptClientAsync(cancellationToken).ConfigureAwait(False)
            Catch ex As OperationCanceledException When cancellationToken.IsCancellationRequested
                Exit While
            Catch ex As Exception
                ' Replace this with proper logging later.
                Debug.WriteLine($"VSAgent server error: {ex}")
            End Try
        End While
    End Function

    Private Async Function AcceptClientAsync(cancellationToken As CancellationToken) As Task
        Debug.WriteLine($"VSAgent: Creating named pipe for server {_serverName}")

        Using pipe = New NamedPipeServerStream(
            _pipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous)

            Debug.WriteLine("VSAgent: Waiting for client")

            Await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(False)

            Debug.WriteLine("VSAgent: Client connected")

            Try
                Await ProcessClientAsync(pipe, cancellationToken).ConfigureAwait(False)
            Catch ex As Exception
                Debug.WriteLine($"VSAgent: Error processing client: {ex}")
            End Try

        End Using

    End Function

    Private Async Function ProcessClientAsync(pipe As NamedPipeServerStream, cancellationToken As CancellationToken) As Task
        Using reader As New StreamReader(pipe)
            Using writer As New StreamWriter(pipe) With {
                    .AutoFlush = True
                }

                _writer = writer

                Debug.WriteLine($"VSAgent {_serverName}: client connected")

                Try
                    While pipe.IsConnected
                        If cancellationToken.IsCancellationRequested Then
                            Exit While
                        End If

                        Debug.WriteLine($"VSAgent {_serverName}: waiting for request")

                        Dim line = Await reader.ReadLineAsync()

                        Debug.WriteLine($"VSAgent {_serverName}: received: " & If(line, "<null>"))

                        If line Is Nothing Then
                            Exit While
                        End If

                        If String.IsNullOrWhiteSpace(line) Then
                            Continue While
                        End If

                        Dim requestMessage = JsonConvert.DeserializeObject(Of TransportMessage)(line)
                        Debug.WriteLine($"VSAgent {_serverName}: type={requestMessage.MessageType}, id={requestMessage.RequestId}")

                        If requestMessage Is Nothing Then
                            Continue While
                        End If

                        If requestMessage.MessageType <> "request" Then
                            Continue While
                        End If

                        Dim response = Await HandleRequestAsync(requestMessage)

                        Debug.WriteLine($"VSAgent {_serverName}: sending response id={response.RequestId}")

                        Await WriteMessageAsync(response)
                        Debug.WriteLine($"VSAgent {_serverName}: response sent id={response.RequestId}")
                    End While
                Finally
                    _writer = Nothing
                End Try
            End Using
        End Using
    End Function

    Private Async Function HandleRequestAsync(requestMessage As TransportMessage) As Task(Of TransportMessage)
        Dim request = requestMessage.Payload.ToObject(Of TRequest)()

        If request Is Nothing Then
            Throw New InvalidOperationException("Could not deserialize request payload.")
        End If

        Dim response = Await _handler(request)

        Return New TransportMessage With {
            .MessageType = "response",
            .RequestId = requestMessage.RequestId,
            .Payload = JObject.FromObject(response)
        }
    End Function

    Private Async Function WriteMessageAsync(message As TransportMessage) As Task
        If _writer Is Nothing Then
            Return
        End If

        Debug.WriteLine($"{_serverName}: waiting for write lock")
        ' Write only one message at a time, this is to prevent json entanglement.
        Await _writeLock.WaitAsync()

        Debug.WriteLine($"{_serverName}: write lock acquired")

        Try
            Dim json = JsonConvert.SerializeObject(message)
            Debug.WriteLine($"{_serverName}: writing {json.Length} chars")
            Await _writer.WriteLineAsync(json)
            Debug.WriteLine($"{_serverName}: WriteLineAsync completed")
            Await _writer.FlushAsync()
            Debug.WriteLine($"{_serverName}: FlushAsync completed")
        Finally
            Debug.WriteLine($"{_serverName}: releasing write lock")
            _writeLock.Release()
        End Try
    End Function

    Public Function SendEventAsync(payload As Object) As Task
        Dim message As New TransportMessage With {
            .MessageType = "event",
            .Payload = JObject.FromObject(payload)
        }

        Return WriteMessageAsync(message)
    End Function

    Public Async Function StopAsync() As Task

        If _serverTask Is Nothing Then
            Return
        End If

        _cancellationTokenSource.Cancel()

        Try
            Await _serverTask.ConfigureAwait(False)
        Catch ex As OperationCanceledException
            ' Normal shutdown.
        End Try
    End Function

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                _cancellationTokenSource.Cancel()
                _cancellationTokenSource.Dispose()

                Try
                    _reader?.Dispose()
                Catch
                End Try

                Try
                    _writer?.Dispose()
                Catch
                End Try
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override finalizer
            ' TODO: set large fields to null
            disposedValue = True
        End If
    End Sub

    ' ' TODO: override finalizer only if 'Dispose(disposing As Boolean)' has code to free unmanaged resources
    ' Protected Overrides Sub Finalize()
    '     ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
    '     Dispose(disposing:=False)
    '     MyBase.Finalize()
    ' End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub
End Class