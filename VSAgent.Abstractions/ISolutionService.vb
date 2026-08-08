Imports VSAgent.Protocol.DTO

Public Interface ISolutionService
    Function GetSolutionInfoAsync() As Task(Of SolutionInfo)

    Function GetProjectsAsync() As Task(Of IReadOnlyList(Of ProjectInfo))
End Interface