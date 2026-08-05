Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Tools

Namespace Tools
    Public Class PingTool
        Implements ITool

        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "ping"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Checks whether the VSAgent server is available."
            End Get
        End Property

        Public ReadOnly Property ParametersSchema As ToolParameterSchema Implements ITool.ParametersSchema
            Get
                Return New ToolParameterSchema With {
                    .Type = "object",
                    .Properties = New Dictionary(Of String, ToolPropertySchema)(),
                    .Required = New List(Of String)()
                }
            End Get
        End Property

        Public Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync
            Return Task.FromResult(AgentResponse.Ok(request.Id, "pong"))
        End Function
    End Class
End Namespace