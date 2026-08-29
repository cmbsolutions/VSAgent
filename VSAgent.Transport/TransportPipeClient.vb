Imports System.IO
Imports System.IO.Pipes
Imports System.Text
Imports Newtonsoft.Json

Public Class TransportPipeClient(Of TRequest, TResponse)
    Implements IDisposable

    Private ReadOnly _pipeName As String

    Private _pipe As NamedPipeClientStream
    Private _reader As StreamReader
    Private _writer As StreamWriter

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

    End Function

    Public Async Function SendAsync(request As TRequest) As Task(Of TResponse)

        If _pipe Is Nothing OrElse Not _pipe.IsConnected Then
            Await ConnectAsync()
        End If

        Dim json = JsonConvert.SerializeObject(request)

        Await _writer.WriteLineAsync(json)

        Dim responseJson = Await _reader.ReadLineAsync()

        If responseJson Is Nothing Then
            Throw New IOException("VSAgent server closed the pipe.")
        End If

        Return JsonConvert.DeserializeObject(Of TResponse)(responseJson)
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
