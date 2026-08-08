Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Tools

Namespace Tools
    Public Class ReadDocumentTool
        Implements ITool

        Private ReadOnly _documentService As IDocumentService

        Public Sub New(documentService As IDocumentService)

            If documentService Is Nothing Then
                Throw New ArgumentNullException(NameOf(documentService))
            End If

            _documentService = documentService
        End Sub

        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "readDocument"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Reads the content of a document and returns it as a string."
            End Get
        End Property

        Public ReadOnly Property ParametersSchema As ToolParameterSchema Implements ITool.ParametersSchema
            Get
                Return New ToolParameterSchema With {
                    .Type = "object",
                    .Properties = New Dictionary(Of String, ToolPropertySchema) From {
                        {"documentId", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "The ID of the document to read."
                        }},
                        {"filePath", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "The full file path of the document to read."
                        }}
                    },
                    .Required = New List(Of String) From {"At least one of 'documentId' or 'filePath' is required"}
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
                Dim filePathObj As Object = Nothing
                Dim documentIdObj As Object = Nothing
                Dim documentId As String = ""
                Dim filePath As String = ""

                If request.Parameters.TryGetValue("documentId", documentIdObj) Then
                    documentId = documentIdObj.ToString()
                End If

                If request.Parameters.TryGetValue("filePath", filePathObj) Then
                    filePath = filePathObj.ToString()
                End If

                Dim document = Await _documentService.ReadDocumentAsync(filePath, documentId).ConfigureAwait(False)

                Return AgentResponse.Ok(request.Id, Version, document)

            Catch ex As Exception
                Return AgentResponse.Failed(request.Id, Version, ex.Message)
            End Try
        End Function
    End Class
End Namespace