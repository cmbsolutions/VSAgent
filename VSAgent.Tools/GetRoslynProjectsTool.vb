Imports VSAgent.Protocol.Messages

Namespace Tools
    Public Class GetRoslynProjectsTool
        Implements ITool

        Private ReadOnly _workspaceService As IRoslynWorkspaceService

        Public Sub New(workspaceService As IRoslynWorkspaceService)

            If workspaceService Is Nothing Then
                Throw New ArgumentNullException(NameOf(workspaceService))
            End If

            _workspaceService = workspaceService
        End Sub

        Public ReadOnly Property Name As String Implements ITool.Name

            Get
                Return "getRoslynProjects"
            End Get

        End Property

        Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync

            Try
                Dim projects = Await _workspaceService.GetProjectsAsync().ConfigureAwait(False)

                Return AgentResponse.Ok(request.Id, projects)

            Catch ex As Exception
                Return AgentResponse.Failed(request.Id, ex.Message)
            End Try

        End Function
    End Class
End Namespace