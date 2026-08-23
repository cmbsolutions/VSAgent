Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Parameters
Imports VSAgent.Protocol.Tools

Namespace Tools
    Public Class FindReferencesTool
        Implements ITool

        Private ReadOnly _symbolService As ISymbolService

        Public Sub New(symbolService As ISymbolService)

            If symbolService Is Nothing Then
                Throw New ArgumentNullException(NameOf(symbolService))
            End If

            _symbolService = symbolService
        End Sub

        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "findReferences"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Finds all source references to a symbol identified by its document and source position."
            End Get
        End Property

        Public ReadOnly Property ParametersSchema As ToolParameterSchema Implements ITool.ParametersSchema
            Get
                Return New ToolParameterSchema With {
                    .Type = "object",
                    .Properties = New Dictionary(Of String, ToolPropertySchema) From {
                        {"documentId", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "Roslyn document ID containing the symbol."
                        }},
                        {"line", New ToolPropertySchema With {
                            .Type = "integer",
                            .Description = "1-based line containing the symbol."
                        }},
                        {"column", New ToolPropertySchema With {
                            .Type = "integer",
                            .Description = "1-based column inside the symbol."
                        }}
                    },
                    .Required = New List(Of String) From {
                        "documentId",
                        "line",
                        "column"
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
                Return "Finding references."
            End Get
        End Property

        Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync
            Try
                Dim parameters = request.GetParameters(Of FindReferenceParameters)()

                Dim symbol = Await _symbolService.FindReferencesAsync(parameters.DocumentId, parameters.Line, parameters.Column).ConfigureAwait(False)

                Return AgentResponse.Ok(request.Id, Version, symbol)

            Catch ex As Exception
                Return AgentResponse.Failed(request.Id, Version, ex.Message)
            End Try
        End Function
    End Class
End Namespace
