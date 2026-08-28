Imports System.IO
Imports System.IO.Pipes
Imports System.Text
Imports System.Threading
Imports Newtonsoft.Json

Public Class TransportPipeServer(Of TRequest, TResponse)
    Implements IDisposable

    Private ReadOnly _pipeName As String
    Private ReadOnly _handler As Func(Of TRequest, Task(Of TResponse))

    Private ReadOnly _cancellationTokenSource As New CancellationTokenSource()
    Private _serverTask As Task

    Private disposedValue As Boolean

    Public Sub New(PipeName As String, Handler As Func(Of TRequest, Task(Of TResponse)))
        _pipeName = PipeName
        _handler = Handler
    End Sub

    Public Sub Start()
        Debug.WriteLine("VSAgent: Starting server")
        If _serverTask IsNot Nothing Then
            Throw New InvalidOperationException("The VSAgent server has already been started.")
        End If

        Debug.WriteLine("VSAgent: Run server")
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
        Dim reader As StreamReader = Nothing
        Dim writer As StreamWriter = Nothing

        Try
            reader = New StreamReader(pipe, New UTF8Encoding(False), detectEncodingFromByteOrderMarks:=False, bufferSize:=4096, leaveOpen:=True)

            writer = New StreamWriter(pipe, New UTF8Encoding(False), bufferSize:=4096, leaveOpen:=True) With {
                .AutoFlush = True
            }

            While pipe.IsConnected AndAlso Not cancellationToken.IsCancellationRequested

                Dim json As String

                Try
                    json = Await reader.ReadLineAsync().ConfigureAwait(False)

                Catch ex As IOException
                    Exit While
                End Try

                If json Is Nothing Then
                    Exit While
                End If

                Dim response = Await HandleRequestAsync(json).ConfigureAwait(False)

                Dim responseJson = JsonConvert.SerializeObject(response)

                Try
                    Await writer.WriteLineAsync(responseJson).ConfigureAwait(False)

                Catch ex As IOException
                    Exit While
                End Try

            End While

        Finally
            If writer IsNot Nothing Then
                Try
                    writer.Dispose()
                Catch ex As IOException
                    ' Normal when the client closes immediately after receiving
                    ' the final response.
                End Try
            End If

            If reader IsNot Nothing Then
                Try
                    reader.Dispose()
                Catch ex As IOException
                    ' Client already disconnected.
                End Try
            End If
        End Try
    End Function

    Private Async Function HandleRequestAsync(json As String) As Task(Of TResponse)
        Dim request = JsonConvert.DeserializeObject(Of TRequest)(json)

        If request Is Nothing Then
            Throw New InvalidOperationException("The request could not be deserialized.")
        End If

        Return Await _handler(request)
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