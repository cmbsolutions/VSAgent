Imports VSAgent.Protocol.Messages

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

        Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync
            Dim projects = Await _solutionService.GetProjectsAsync()

            Return AgentResponse.Ok(request.Id, projects)
        End Function
    End Class
End Namespace

