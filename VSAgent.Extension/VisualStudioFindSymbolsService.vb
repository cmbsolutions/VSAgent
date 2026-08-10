Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices
Imports Microsoft.VisualStudio.Shell
Imports VSAgent.Protocol.DTO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.FindSymbols

Public Class VisualStudioFindSymbolsService
    Implements ISymbolService

    Private ReadOnly _package As AsyncPackage

    Public Sub New(package As AsyncPackage)

        If package Is Nothing Then
            Throw New ArgumentNullException(NameOf(package))
        End If

        _package = package
    End Sub

    Public Async Function FindSymbolsAsync(SymbolName As String) As Task(Of IReadOnlyList(Of RoslynSymbolInfo)) Implements ISymbolService.FindSymbolsAsync
        Dim workspace = Await GetWorkspaceAsync()

        Dim solution = workspace.CurrentSolution

        Dim symbols = Await SymbolFinder.FindSourceDeclarationsAsync(solution, SymbolName, ignoreCase:=True)

        Dim results As New List(Of RoslynSymbolInfo)

        For Each symbol As ISymbol In symbols

            For Each location In symbol.Locations

                If Not location.IsInSource Then
                    Continue For
                End If

                Dim lineSpan = location.GetLineSpan()

                Dim document = solution.GetDocument(location.SourceTree)

                Dim info As New RoslynSymbolInfo With {
                    .Name = symbol.Name,
                    .Kind = symbol.Kind.ToString(),
                    .FullyQualifiedName = symbol.ToDisplayString(),
                    .FilePath = lineSpan.Path,
                    .Line = lineSpan.StartLinePosition.Line + 1,
                    .Column = lineSpan.StartLinePosition.Character + 1
                }

                If document IsNot Nothing Then
                    info.DocumentId = document.Id.Id.ToString()
                    info.ProjectId = document.Project.Id.Id.ToString()
                    info.ProjectName = document.Project.Name
                End If

                results.Add(info)
            Next
        Next

        Return results

    End Function

    Private Async Function GetWorkspaceAsync() As Task(Of VisualStudioWorkspace)

        ' We need the UI thread from visual studio to get the workspace service, so we switch to it here. When we have it we can switch back to the background thread to do the rest of the work.
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()

        Dim componentModel = TryCast(Await _package.GetServiceAsync(GetType(SComponentModel)), IComponentModel)

        If componentModel Is Nothing Then
            Throw New InvalidOperationException("The Visual Studio component model is unavailable.")
        End If

        Dim workspace = componentModel.GetService(Of VisualStudioWorkspace)()

        If workspace Is Nothing Then
            Throw New InvalidOperationException("The Visual Studio Roslyn workspace is unavailable.")
        End If

        Return workspace

    End Function

End Class
