Imports VSAgent.Protocol.DTO

Public Interface IRoslynWorkspaceService

    Function GetProjectsAsync() As Task(Of IReadOnlyList(Of RoslynProjectInfo))
    Function GetProjectDocumentsAsync(projectId As String) As Task(Of IReadOnlyList(Of RoslynDocumentInfo))
    Function GetProjectsFilesAsync(projectId As String) As Task(Of IReadOnlyList(Of ProjectFileInfo))

    Function InspectTypeAsync(projectId As String, typeName As String, memberName As String) As Task(Of InspectTypeInfo)
End Interface