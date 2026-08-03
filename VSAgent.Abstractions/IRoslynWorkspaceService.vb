Imports VSAgent.Protocol.DTO

Public Interface IRoslynWorkspaceService

    Function GetProjectsAsync() As Task(Of IReadOnlyList(Of RoslynProjectInfo))
End Interface