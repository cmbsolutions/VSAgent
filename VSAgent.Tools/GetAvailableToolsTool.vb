Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Tools

Namespace Tools
    Public Class GetAvailableToolsTool
        Implements ITool

        Private ReadOnly _registry As ToolRegistry

        Public Sub New(registry As ToolRegistry)
            _registry = registry
        End Sub


        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "getAvailableTools"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Returns all tools exposed by the VSAgent server."
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

        Public ReadOnly Property Version As Integer Implements ITool.Version
            Get
                Return 1
            End Get
        End Property

        Public ReadOnly Property ActionDescription As String Implements ITool.ActionDescription
            Get
                Return "Getting all available tools."
            End Get
        End Property

        Public Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync
            Dim tools = _registry.GetAvailableTools()

            Return Task.FromResult(AgentResponse.Ok(request.Id, Version, tools))
        End Function
    End Class
End Namespace