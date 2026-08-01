Imports VSAgent.Protocol.Messages

Public Class GetSolutionInfoTool
    Implements ITool

    Private ReadOnly _solutionService As ISolutionService

    Public Sub New(solutionService As ISolutionService)
        _solutionService = solutionService
    End Sub

    Public ReadOnly Property Name As String Implements ITool.Name
        Get
            Return "getSolutionInfo"
        End Get
    End Property

    Public Async Function ExecuteAsync(request As AgentRequest) As Task(Of AgentResponse) Implements ITool.ExecuteAsync

        Dim solutionInfo = Await _solutionService.GetSolutionInfoAsync()

        Return AgentResponse.Ok(request.Id, solutionInfo)
    End Function
End Class
