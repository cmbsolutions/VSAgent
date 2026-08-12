Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices
Imports Microsoft.VisualStudio.Shell
Imports VSAgent.Protocol.DTO

Public Class VisualStudioFindSymbolsService
    Implements ISymbolService

    Private ReadOnly _package As AsyncPackage
    Private _cancellationToken As CancellationToken

    Public Sub New(package As AsyncPackage, cancellationToken As CancellationToken)

        If package Is Nothing Then
            Throw New ArgumentNullException(NameOf(package))
        End If

        _package = package
        _cancellationToken = cancellationToken
    End Sub

    Public Async Function FindSymbolsAsync(SymbolName As String) As Task(Of IReadOnlyList(Of RoslynSymbolInfo)) Implements ISymbolService.FindSymbolsAsync
        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)

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

    Public Async Function FindReferencesAsync(documentId As String, line As Integer, column As Integer) As Task(Of IReadOnlyList(Of RoslynSymbolReferenceInfo)) Implements ISymbolService.FindReferencesAsync
        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)
        Dim solution = workspace.CurrentSolution

        Dim document = solution.Projects _
        .SelectMany(Function(p) p.Documents) _
        .FirstOrDefault(
            Function(d) String.Equals(
                d.Id.Id.ToString(),
                documentId,
                StringComparison.OrdinalIgnoreCase))

        If document Is Nothing Then
            Throw New InvalidOperationException($"Document '{documentId}' was not found.")
        End If

        Dim text = Await document.GetTextAsync(_cancellationToken)

        ' Protocol uses 1-based line/column numbers.
        Dim lineIndex = line - 1
        Dim columnIndex = column - 1

        If lineIndex < 0 OrElse lineIndex >= text.Lines.Count Then
            Throw New ArgumentOutOfRangeException(NameOf(line))
        End If

        Dim sourceLine = text.Lines(lineIndex)

        If columnIndex < 0 OrElse columnIndex > sourceLine.Span.Length Then
            Throw New ArgumentOutOfRangeException(NameOf(column))
        End If

        Dim position = sourceLine.Start + columnIndex

        Dim semanticModel = Await document.GetSemanticModelAsync(_cancellationToken)

        If semanticModel Is Nothing Then
            Throw New InvalidOperationException("Could not obtain the semantic model.")
        End If

        Dim root = Await document.GetSyntaxRootAsync(_cancellationToken)

        If root Is Nothing Then
            Throw New InvalidOperationException("Could not obtain the syntax tree.")
        End If

        Dim token = root.FindToken(position)
        Dim node = token.Parent

        If node Is Nothing Then
            Throw New InvalidOperationException("No syntax node exists at the requested position.")
        End If

        Dim symbolInfo = semanticModel.GetSymbolInfo(node, _cancellationToken)

        Dim symbol As ISymbol = symbolInfo.Symbol

        If symbol Is Nothing Then
            symbol = semanticModel.GetDeclaredSymbol(node, _cancellationToken)
        End If

        If symbol Is Nothing Then
            Throw New InvalidOperationException("No symbol could be resolved at the requested position.")
        End If

        Dim referencedSymbols = Await SymbolFinder.FindReferencesAsync(symbol, solution, _cancellationToken)

        Dim result As New List(Of RoslynSymbolReferenceInfo)

        For Each referencedSymbol In referencedSymbols

            For Each referenceLocation In referencedSymbol.Locations

                Dim referenceDocument = solution.GetDocument(referenceLocation.Document.Id)

                If referenceDocument Is Nothing Then
                    Continue For
                End If

                Dim referenceText = Await referenceDocument.GetTextAsync(_cancellationToken)

                Dim span = referenceLocation.Location.SourceSpan

                Dim linePosition = referenceText.Lines.GetLinePosition(span.Start)

                Dim containingLine = referenceText.Lines(linePosition.Line)

                result.Add(New RoslynSymbolReferenceInfo With {
                    .ProjectName = referenceDocument.Project.Name,
                    .DocumentId = referenceDocument.Id.Id.ToString(),
                    .FilePath = referenceDocument.FilePath,
                    .Line = linePosition.Line + 1,
                    .Column = linePosition.Character + 1,
                    .Text = containingLine.ToString().Trim()
                })

            Next
        Next

        Return result
    End Function
End Class
