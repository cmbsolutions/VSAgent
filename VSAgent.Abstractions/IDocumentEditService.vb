Imports VSAgent.Protocol.DTO

Public Interface IDocumentEditService
    Function ApplyDocumentEditAsync(documentId As String, filePath As String, oldText As String, newText As String) As Task(Of DocumentEditResult)
    Function AddDocumentAsync(projectId As String, name As String, text As String, folders As IReadOnlyList(Of String)) As Task(Of AddDocumentResult)
End Interface
