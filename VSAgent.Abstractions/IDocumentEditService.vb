Imports VSAgent.Protocol.DTO

Public Interface IDocumentEditService
    Function ApplyDocumentEditAsync(documentId As String, filePath As String, oldText As String, newText As String) As Task(Of DocumentEditResult)
End Interface
