Imports VSAgent.Protocol.DTO

Public Interface ISolutionService
    Function GetSolutionInfoAsync() As Task(Of SolutionInfo)
End Interface