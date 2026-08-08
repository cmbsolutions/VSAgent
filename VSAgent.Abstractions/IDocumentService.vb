Imports VSAgent.Protocol.DTO

Public Interface IDocumentService
    Function GetActiveDocumentAsync() As Task(Of ActiveDocumentInfo)

    Function ReadDocumentAsync(Optional filePath As String = Nothing, Optional documentId As String = Nothing) As Task(Of RoslynDocument)
End Interface
