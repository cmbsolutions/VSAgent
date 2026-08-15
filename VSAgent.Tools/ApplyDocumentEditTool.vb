Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Parameters
Imports VSAgent.Protocol.Tools

Namespace Tools
    Public Class ApplyDocumentEditTool
        Implements ITool

        Private ReadOnly _editService As IDocumentEditService

        Public Sub New(editService As IDocumentEditService)
            _editService = editService
        End Sub

        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "applyDocumentEdit"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Applies a source-code edit directly to a document in the currently loaded Visual Studio solution. The tool handles Roslyn and Visual Studio threading requirements internally. Use this tool when you need to modify source code; do not ask the user to make the edit manually."
            End Get
        End Property

        Public ReadOnly Property ParametersSchema As ToolParameterSchema Implements ITool.ParametersSchema
            Get
                Return New ToolParameterSchema With {
                    .Type = "object",
                    .Properties = New Dictionary(Of String, ToolPropertySchema) From {
                        {"documentId", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "Roslyn document ID."
                        }},
                        {"filePath", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "Document path used as fallback."
                        }},
                        {"oldText", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "Exact existing source text to replace. It must occur exactly once."
                        }},
                        {"newText", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "Replacement source text."
                        }}
                    },
                    .Required = New List(Of String) From {
                        "documentId",
                        "oldText",
                        "newText"
                    }
                }
            End Get
        End Property

        Public ReadOnly Property Version As Integer Implements ITool.Version
            Get
                Return 1
            End Get
        End Property

        Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync
            Try
                Dim parameters = request.GetParameters(Of ApplyDocumentEditParameters)()

                Dim result = Await _editService.ApplyDocumentEditAsync(
                    parameters.DocumentId,
                    parameters.FilePath,
                    parameters.OldText,
                    parameters.NewText)

                Return AgentResponse.Ok(request.Id, Version, result)

            Catch ex As Exception
                Return AgentResponse.Failed(request.Id, Version, ex.Message)
            End Try
        End Function
    End Class
End Namespace
