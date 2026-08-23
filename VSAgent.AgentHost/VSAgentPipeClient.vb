Imports System.IO
Imports System.IO.Pipes
Imports System.Text
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports VSAgent.Protocol.DTO
Imports VSAgent.Protocol.Messages

Public Class VSAgentPipeClient
    Implements IDisposable

    Private Const PipeName As String = "VSAgent"

    Private _pipe As NamedPipeClientStream
    Private _reader As StreamReader
    Private _writer As StreamWriter

    Public Async Function ConnectAsync() As Task

        If _pipe IsNot Nothing AndAlso _pipe.IsConnected Then
            Return
        End If

        _pipe = New NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous)

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

    Public Async Function CallToolAsync(toolName As String, parameters As JObject) As Task(Of AgentResponse)

        If _pipe Is Nothing OrElse Not _pipe.IsConnected Then
            Await ConnectAsync()
        End If

        Dim request As New AgentRequest With {
            .Id = Guid.NewGuid().ToString(),
            .Tool = toolName,
            .Parameters = If(parameters, New JObject)
        }

        Dim json = JsonConvert.SerializeObject(request)

        Await _writer.WriteLineAsync(json)

        Dim responseJson = Await _reader.ReadLineAsync()

        If responseJson Is Nothing Then
            Throw New IOException("VSAgent server closed the pipe.")
        End If

        Return JsonConvert.DeserializeObject(Of AgentResponse)(responseJson)

    End Function

    Public Async Function GetAvailableToolsAsync() As Task(Of IReadOnlyList(Of ToolDescriptor))
        Dim response = Await CallToolAsync("getAvailableTools", New JObject)

        If Not response.Success Then
            Throw New InvalidOperationException(response.ErrorMessage)
        End If

        Return response.GetResult(Of List(Of ToolDescriptor))()
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