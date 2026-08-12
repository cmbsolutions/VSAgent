Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices
Imports Microsoft.VisualStudio.Shell
Imports VSAgent
Imports VSAgent.Protocol
Imports VSAgent.Protocol.DTO

Public Class VisualStudioDiagnosticsService
    Implements IRoslynDiagnosticsService

    Private ReadOnly _cancellationToken As CancellationToken
    Private ReadOnly _package As AsyncPackage

    Public Sub New(package As AsyncPackage, cancellationToken As CancellationToken)

        If package Is Nothing Then
            Throw New ArgumentNullException(NameOf(package))
        End If

        _package = package
        _cancellationToken = cancellationToken
    End Sub

    Public Async Function GetDiagnosticsAsync() As Task(Of IReadOnlyList(Of RoslynDiagnosticInfo)) Implements IRoslynDiagnosticsService.GetDiagnosticsAsync
        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)
        Dim solution = workspace.CurrentSolution
        Dim result As New List(Of RoslynDiagnosticInfo)

        For Each project In solution.Projects
            Dim compilation = Await project.GetCompilationAsync(_cancellationToken)

            If compilation Is Nothing Then
                Continue For
            End If

            Dim diagnostics = compilation.GetDiagnostics(_cancellationToken)

            For Each diagnostic In diagnostics

                Dim info As New RoslynDiagnosticInfo With {
                    .Id = diagnostic.Id,
                    .Severity = diagnostic.Severity.ToString(),
                    .Message = diagnostic.GetMessage(),
                    .ProjectName = project.Name
                }

                If diagnostic.Location IsNot Nothing AndAlso diagnostic.Location.IsInSource Then

                    Dim location = diagnostic.Location
                    Dim span = location.GetLineSpan()

                    info.FilePath = span.Path
                    info.Line = span.StartLinePosition.Line + 1
                    info.Column = span.StartLinePosition.Character + 1

                    Dim document = solution.GetDocument(location.SourceTree)

                    If document IsNot Nothing Then
                        info.DocumentId = document.Id.Id.ToString()
                    End If

                End If

                result.Add(info)
            Next
        Next

        Return result
    End Function
End Class