Imports VSAgent.Protocol.Messages

Public Class AgentPipeServer

    Private ReadOnly _transport As Transport.TransportPipeServer(Of AgentRequest, AgentResponse)
    Private ReadOnly _toolRegistry As ToolRegistry

    Sub New(Tools As ToolRegistry)
        _toolRegistry = Tools

        _transport = New Transport.TransportPipeServer(Of AgentRequest, AgentResponse)("VSAgent", AddressOf HandleAgentRequestAsync)

        _transport.Start()
    End Sub

    Public Async Function StopAsync() As Task
        If _transport Is Nothing Then
            Return
        End If

        Await _transport.StopAsync
    End Function

    Public Async Function HandleAgentRequestAsync(request As AgentRequest) As Task(Of AgentResponse)

        Dim tool = _toolRegistry.GetTool(request.Tool)

        If tool Is Nothing Then
            Return AgentResponse.Failed(request.Id, 1, $"Unknown tool: {request.Tool}")
        End If

        Try
            Return Await tool.ExecuteAsync(request)

        Catch ex As Exception
            Return AgentResponse.Failed(request.Id, 1, ex.Message)
        End Try

    End Function
End Class