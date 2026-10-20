Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Tools

Namespace Tools
    Public Class InspectTypeTool
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
                Return "inspectType"
            End Get

        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Gets a list of all Roslyn projects in the current solution."
            End Get
        End Property

        Public ReadOnly Property ParametersSchema As ToolParameterSchema Implements ITool.ParametersSchema
            Get
                Return New ToolParameterSchema With {
                    .Type = "object",
                    .Properties = New Dictionary(Of String, ToolPropertySchema) From {
                        {"projectid", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "The roslyn projectID."
                        }},
                        {"typename", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "The name of the type to inspect."
                        }},
                        {"membername", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "The name of a member to inspect."
                        }}
                    },
                    .Required = New List(Of String) From {"projectid", "typename"}}
            End Get
        End Property

        Public ReadOnly Property Version As Integer Implements ITool.Version
            Get
                Return 1
            End Get
        End Property

        Public ReadOnly Property ActionDescription As String Implements ITool.ActionDescription
            Get
                Return "Inspecting a type in Roslyn."
            End Get
        End Property

        Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync

            Try
                Dim projects = Await _workspaceService.InspectTypeAsync(request.Parameters("projectid").ToString, request.Parameters("typename").ToString, request.Parameters("membername").ToString).ConfigureAwait(False)

                Return AgentResponse.Ok(request.Id, Version, projects)

            Catch ex As Exception
                Return AgentResponse.Failed(request.Id, Version, ex.Message)
            End Try

        End Function
    End Class
End Namespace
