Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Tools

Namespace Tools
    Public Class GetSolutionInfoTool
        Implements ITool

        Private ReadOnly _solutionService As ISolutionService

        Public Sub New(solutionService As ISolutionService)
            _solutionService = solutionService
        End Sub

        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "getSolutionInfo"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Gets information about the current solution."
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

        Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync

            Dim solutionInfo = Await _solutionService.GetSolutionInfoAsync()

            Return AgentResponse.Ok(request.Id, solutionInfo)
        End Function
    End Class
End Namespace
