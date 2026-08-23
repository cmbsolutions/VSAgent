Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Tools

Namespace Tools

    Public Class GetProjectsTool
        Implements ITool

        Private ReadOnly _solutionService As ISolutionService

        Public Sub New(solutionService As ISolutionService)
            _solutionService = solutionService
        End Sub

        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "getProjects"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Gets a list of all projects in the current solution SDK Style."
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
                Return "Retrieving all projects."
            End Get
        End Property

        Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync
            Dim projects = Await _solutionService.GetProjectsAsync()

            Return AgentResponse.Ok(request.Id, Version, projects)
        End Function
    End Class
End Namespace

