Imports System.IO
Imports System.IO.Pipes
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports VSAgent.Protocol.Messages

Public Class AgentPipeServer
    Implements IDisposable

    Private Const PipeName As String = "VSAgent"

    Private ReadOnly _cancellationTokenSource As New CancellationTokenSource()
    Private _serverTask As Task
    Private disposedValue As Boolean

    Public Sub Start()
        If _serverTask IsNot Nothing Then
            Throw New InvalidOperationException(
                "The VSAgent server has already been started.")
        End If

        _serverTask = RunServerAsync(_cancellationTokenSource.Token)
    End Sub

    Private Async Function RunServerAsync(cancellationToken As CancellationToken) As Task
        While Not cancellationToken.IsCancellationRequested
            Try
                Await AcceptClientAsync(cancellationToken).ConfigureAwait(False)
            Catch ex As OperationCanceledException _
                When cancellationToken.IsCancellationRequested

                Exit While
            Catch ex As Exception
                ' Replace this with proper logging later.
                Debug.WriteLine($"VSAgent server error: {ex}")
            End Try
        End While
    End Function

    Private Async Function AcceptClientAsync(cancellationToken As CancellationToken) As Task
        Debug.WriteLine("VSAgent: Creating named pipe")

        Using pipe = New NamedPipeServerStream(
            PipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous)

            Debug.WriteLine("VSAgent: Waiting for client")

            Await pipe.WaitForConnectionAsync(cancellationToken).
                ConfigureAwait(False)

            Debug.WriteLine("VSAgent: Client connected")

            Try
                Await ProcessClientAsync(pipe, cancellationToken).
                    ConfigureAwait(False)
            Catch ex As Exception
                Debug.WriteLine($"VSAgent: Error processing client: {ex}")
            End Try
        End Using

    End Function

    Private Async Function ProcessClientAsync(pipe As NamedPipeServerStream, cancellationToken As CancellationToken) As Task
        Using reader = New StreamReader(
            pipe,
            New UTF8Encoding(False),
            detectEncodingFromByteOrderMarks:=False,
            bufferSize:=4096,
            leaveOpen:=True)

            Using writer = New StreamWriter(
                pipe,
                New UTF8Encoding(False),
                bufferSize:=4096,
                leaveOpen:=True)

                writer.AutoFlush = True

                While pipe.IsConnected AndAlso Not cancellationToken.IsCancellationRequested

                    Dim json = Await reader.ReadLineAsync()

                    If json Is Nothing Then
                        Exit While
                    End If

                    Dim response = HandleRequest(json)

                    Dim responseJson = JsonSerializer.Serialize(response)

                    Await writer.WriteLineAsync(responseJson)
                End While
            End Using
        End Using
    End Function

    Private Shared Function HandleRequest(json As String) As AgentResponse

        Try
            Dim request = JsonSerializer.Deserialize(Of AgentRequest)(json)

            If request Is Nothing Then
                Return AgentResponse.Failed(
                    Nothing,
                    "The request could not be deserialized.")
            End If

            Select Case request.Method?.ToLowerInvariant()

                Case "ping"
                    Return AgentResponse.Ok(request.Id, "pong")

                Case Else
                    Return AgentResponse.Failed(
                        request.Id,
                        $"Unknown method: {request.Method}")

            End Select

        Catch ex As JsonException
            Return AgentResponse.Failed(
                Nothing,
                $"Invalid JSON: {ex.Message}")

        Catch ex As Exception
            Return AgentResponse.Failed(
                Nothing,
                ex.Message)
        End Try

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