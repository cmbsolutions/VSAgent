Imports VSAgent.Protocol.DTO

Public Interface IDocumentService
    Function GetActiveDocumentAsync() As Task(Of ActiveDocumentInfo)
End Interface
