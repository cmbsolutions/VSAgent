Imports VSAgent.Protocol.DTO

Public Interface IBuildService
    Function BuildSolutionAsync() As Task(Of BuildResult)
    Function BuildProjectAsync(ProjectId As String) As Task(Of BuildResult)
End Interface
