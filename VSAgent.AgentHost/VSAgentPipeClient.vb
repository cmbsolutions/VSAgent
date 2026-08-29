Imports System.IO
Imports System.IO.Pipes
Imports System.Text
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports VSAgent.Protocol.DTO
Imports VSAgent.Protocol.Messages

Public Class VSAgentPipeClient
    Implements IDisposable

    Private ReadOnly _transport As Transport.TransportPipeClient(Of AgentRequest, AgentResponse)

    Public Sub New(PipeName As String)
        _transport = New Transport.TransportPipeClient(Of AgentRequest, AgentResponse)(PipeName)
    End Sub

    Public Function ConnectAsync() As Task
        Return _transport.ConnectAsync()
    End Function

    Public Function CallToolAsync(toolName As String, parameters As JObject) As Task(Of AgentResponse)

        Dim request As New AgentRequest With {
           .Id = Guid.NewGuid().ToString(),
           .Tool = toolName,
           .Parameters = If(parameters, New JObject)
        }

        Return _transport.SendAsync(request)
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
            _transport?.Dispose()
        Catch
        End Try
    End Sub
End Class