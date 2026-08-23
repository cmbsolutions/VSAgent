Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Parameters
Imports VSAgent.Protocol.Tools

Namespace Tools
    Public Class FindSymbolsTool
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
                Return "findSymbol"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Search by symbol name and returns matching classes, methods, properties, fields, etc."
            End Get
        End Property

        Public ReadOnly Property ParametersSchema As ToolParameterSchema Implements ITool.ParametersSchema
            Get
                Return New ToolParameterSchema With {
                    .Type = "object",
                    .Properties = New Dictionary(Of String, ToolPropertySchema) From {
                        {"SymbolName", New ToolPropertySchema With {
                            .Type = "string",
                            .Description = "The name of the symbol to search for."
                        }}
                    },
                    .Required = New List(Of String) From {"SymbolName"}
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
                Return "Finding a symbol."
            End Get
        End Property

        Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync
            Try
                Dim parameters = request.GetParameters(Of FindSymbolParameters)()

                Dim symbol = Await _symbolService.FindSymbolsAsync(parameters.SymbolName).ConfigureAwait(False)

                Return AgentResponse.Ok(request.Id, Version, symbol)

            Catch ex As Exception
                Return AgentResponse.Failed(request.Id, Version, ex.Message)
            End Try
        End Function
    End Class
End Namespace
