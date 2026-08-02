Imports VSAgent.Protocol.Messages

Namespace Tools
    Public Class PingTool
        Implements ITool

        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "ping"
            End Get
        End Property

        Public Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync
            Return Task.FromResult(
            AgentResponse.Ok(
                request.Id,
                "pong"))
        End Function
    End Class
End Namespace