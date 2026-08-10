Imports VSAgent.Protocol.DTO

Public Interface ISymbolService
    Function FindSymbolsAsync(SymbolName As String) As Task(Of IReadOnlyList(Of RoslynSymbolInfo))
    Function FindReferencesAsync(documentId As String, line As Integer, column As Integer) As Task(Of IReadOnlyList(Of RoslynSymbolReferenceInfo))
End Interface
