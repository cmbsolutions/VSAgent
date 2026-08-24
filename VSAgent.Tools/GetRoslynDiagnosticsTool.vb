Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Tools

Namespace Tools
    Public Class GetRoslynDiagnosticsTool
        Implements ITool

        Private ReadOnly _diagnosticsService As IRoslynDiagnosticsService

        Public Sub New(diagnosticsService As IRoslynDiagnosticsService)
            _diagnosticsService = diagnosticsService
        End Sub

        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "getDiagnostics"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Returns compiler diagnostics for the currently loaded Visual Studio solution."
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

        Public ReadOnly Property ActionDescription As String Implements ITool.ActionDescription
            Get
                Return "Getting diagnostics."
            End Get
        End Property

        Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync
            Try

                Dim diagnostics = Await _diagnosticsService.GetDiagnosticsAsync()

                Return AgentResponse.Ok(request.Id, Version, diagnostics)

            Catch ex As Exception
                Return AgentResponse.Failed(request.Id, Version, ex.Message)
            End Try
        End Function
    End Class
End Namespace
