Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Tools
Namespace Tools
    Public Class GetProjectDocumentsTool
        Implements ITool

        Private ReadOnly _roslynWorkspaceService As IRoslynWorkspaceService

        Public Sub New(roslynWorkspaceService As IRoslynWorkspaceService)

            If roslynWorkspaceService Is Nothing Then
                Throw New ArgumentNullException(NameOf(roslynWorkspaceService))
            End If

            _roslynWorkspaceService = roslynWorkspaceService
        End Sub


        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "getProjectDocuments"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Returns all source documents in a Roslyn project. " &
               "Use this tool to discover document IDs before calling " &
               "readDocument or other document-based tools."
            End Get
        End Property

        Public ReadOnly Property ParametersSchema As ToolParameterSchema Implements ITool.ParametersSchema
            Get
                Return New ToolParameterSchema With {
                    .Type = "object",
                    .Properties = New Dictionary(Of String, ToolPropertySchema) From {
                        {"projectid", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "The ID of the project."
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
                Return "Listing project documents."
            End Get
        End Property

        Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync
            Try
                Dim documents = Await _roslynWorkspaceService.GetProjectDocumentsAsync(request.Parameters("projectid").ToString()).ConfigureAwait(False)

                Return AgentResponse.Ok(request.Id, Version, documents)

            Catch ex As Exception
                Return AgentResponse.Failed(request.Id, Version, ex.Message)
            End Try

        End Function
    End Class
End Namespace