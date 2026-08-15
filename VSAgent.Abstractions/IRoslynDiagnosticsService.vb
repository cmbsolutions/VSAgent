Imports VSAgent.Protocol.DTO

Public Interface IRoslynDiagnosticsService

    Function GetDiagnosticsAsync() As Task(Of IReadOnlyList(Of RoslynDiagnosticInfo))
End Interface