Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Parameters
Imports VSAgent.Protocol.Tools

Namespace Tools
    Public Class AddDocumentTool
        Implements ITool

        Private ReadOnly _documentEditService As IDocumentEditService

        Public Sub New(documentEditService As IDocumentEditService)
            _documentEditService = documentEditService
        End Sub

        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "addDocument"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Creates a new source document in a Visual Studio project. Use this when code should be moved into a new class, module, interface, or helper file instead of placing everything in an existing document."
            End Get
        End Property

        Public ReadOnly Property ParametersSchema As ToolParameterSchema Implements ITool.ParametersSchema
            Get
                Return New ToolParameterSchema With {
                    .Type = "object",
                    .Properties = New Dictionary(Of String, ToolPropertySchema) From {
                        {"projectid", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "Roslyn project ID where the document will be created."
                        }},
                        {"name", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "File name including extension. For example CustomerService.vb, CustomerHelper.cs"
                        }},
                        {"text", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "Initial source text for the new document."
                        }},
                        {"folders", New ToolPropertySchema With {
                            .Type = "array",
                            .Description = "Optional project folder hierarchy.",
                            .Items = New ToolPropertySchema With {
                                .Type = "string"
                            }
                        }}
                    },
                    .Required = New List(Of String) From {
                        "projectid",
                        "name",
                        "text"
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
                Dim parameters = request.GetParameters(Of AddDocumentParameters)()

                Dim result = Await _documentEditService.AddDocumentAsync(parameters.ProjectId, parameters.Name, parameters.Text, parameters.Folders)

                Return AgentResponse.Ok(request.Id, Version, result)

            Catch ex As Exception
                Return AgentResponse.Failed(request.Id, Version, ex.Message)
            End Try
        End Function
    End Class
End Namespace
