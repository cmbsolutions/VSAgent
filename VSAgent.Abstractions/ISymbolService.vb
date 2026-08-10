Imports VSAgent.Protocol.DTO

Public Interface ISymbolService
    Function FindSymbolsAsync(Name As String) As Task(Of IReadOnlyList(Of RoslynSymbolInfo))
End Interface
