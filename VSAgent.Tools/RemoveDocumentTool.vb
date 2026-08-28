Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Parameters
Imports VSAgent.Protocol.Tools

Namespace Tools
    Public Class RemoveDocumentTool
        Implements ITool

        Private ReadOnly _documentEditService As IDocumentEditService

        Public Sub New(documentEditService As IDocumentEditService)
            _documentEditService = documentEditService
        End Sub

        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "removeDocument"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Removes a source document in a Visual Studio project. Use this when source documents that are created with the addDocument tool need to be removed. Use this tool with great care! Gone is gone forever!"
            End Get
        End Property

        Public ReadOnly Property ParametersSchema As ToolParameterSchema Implements ITool.ParametersSchema
            Get
                Return New ToolParameterSchema With {
                    .Type = "object",
                    .Properties = New Dictionary(Of String, ToolPropertySchema) From {
                        {"projectid", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "Roslyn project ID where the document lives."
                        }},
                        {"documentname", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "The name of the document."
                        }}
                    },
                    .Required = New List(Of String) From {
                        "projectid",
                        "documentname"
                    }
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
                Return "Removing a document from the project"
            End Get
        End Property

        Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync
            Try
                Dim parameters = request.GetParameters(Of RemoveDocumentParameters)()

                Dim result = Await _documentEditService.RemoveDocumentAsync(parameters.ProjectId, parameters.DocumentName)

                Return AgentResponse.Ok(request.Id, Version, result)

            Catch ex As Exception
                Return AgentResponse.Failed(request.Id, Version, ex.Message)
            End Try
        End Function
    End Class
End Namespace
