Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Tools

Namespace Tools
    Public Class BuildSolutionTool
        Implements ITool

        Private ReadOnly _buildService As IBuildService

        Public Sub New(buildService As IBuildService)
            _buildService = buildService
        End Sub

        Public ReadOnly Property Name As String Implements ITool.Name
            Get
                Return "buildSolution"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ITool.Description
            Get
                Return "Builds the currently loaded Visual Studio solution and returns whether the build succeeded."
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
                Dim result = Await _buildService.BuildSolutionAsync()

                Return AgentResponse.Ok(request.Id, Version, result)

            Catch ex As Exception
                Return AgentResponse.Failed(request.Id, Version, ex.Message)
            End Try
        End Function
    End Class
End Namespace
