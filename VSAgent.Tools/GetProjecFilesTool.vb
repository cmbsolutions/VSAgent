Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Tools

Namespace Tools
    Public Class GetProjectFilesTool
        Implements ITool

        Private ReadOnly _projectService As IRoslynWorkspaceService

        Public Sub New(ProjectService As IRoslynWorkspaceService)

            If ProjectService Is Nothing Then
                Throw New ArgumentNullException(NameOf(ProjectService))
            End If

            _projectService = ProjectService
        End Sub


        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "getProjectFiles"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Returns physical files beneath a project's directory, including resources and files not represented as Roslyn documents. Excludes build and source-control directories such as bin, obj, .vs and .git. Use this tool to discover assets, resources, configuration files and other non-source files."
            End Get
        End Property

        Public ReadOnly Property ParametersSchema As ToolParameterSchema Implements ITool.ParametersSchema
            Get
                Return New ToolParameterSchema With {
                    .Type = "object",
                    .Properties = New Dictionary(Of String, ToolPropertySchema) From {
                        {"projectid", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "The roslyn projectID of the project."
                        }}
                    },
                    .Required = New List(Of String) From {"projectid"}
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
                Return "Listing project files."
            End Get
        End Property

        Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync
            Try
                Dim files = Await _projectService.GetProjectsFilesAsync(request.Parameters("projectid").ToString()).ConfigureAwait(False)

                Return AgentResponse.Ok(request.Id, Version, files)

            Catch ex As Exception
                Return AgentResponse.Failed(request.Id, Version, ex.Message)
            End Try

        End Function
    End Class
End Namespace