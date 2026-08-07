Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Tools
Namespace Tools
    Public Class GetActiveDocumentTool
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
                Return "getActiveDocument"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Returns information about the currently active document in Visual Studio, including caret and selection information."
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

        Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync
            Try
                Dim ActiveDocument = Await _documentService.GetActiveDocumentAsync().ConfigureAwait(False)

                Return AgentResponse.Ok(request.Id, Version, ActiveDocument)

            Catch ex As Exception
                Return AgentResponse.Failed(request.Id, Version, ex.Message)
            End Try

        End Function
    End Class
End Namespace