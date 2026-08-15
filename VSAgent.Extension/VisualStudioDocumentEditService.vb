Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Shell
Imports VSAgent.Protocol.DTO

Public Class VisualStudioDocumentEditService
    Implements IDocumentEditService

    Private ReadOnly _package As AsyncPackage
    Private ReadOnly _cancellationToken As CancellationToken

    Public Sub New(package As AsyncPackage, cancellationToken As CancellationToken)
        _package = package
        _cancellationToken = cancellationToken
    End Sub

    Public Async Function ApplyDocumentEditAsync(documentId As String, filePath As String, oldText As String, newText As String) As Task(Of DocumentEditResult) Implements IDocumentEditService.ApplyDocumentEditAsync

        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)
        Dim solution = workspace.CurrentSolution

        Dim document = FindDocument(solution, documentId, filePath)

        If document Is Nothing Then
            Throw New InvalidOperationException("The requested document could not be found.")
        End If

        Dim sourceText = Await document.GetTextAsync(_cancellationToken)

        Dim fullText = sourceText.ToString

        Dim match = FindTextMatch(fullText, oldText)

        Dim span = New TextSpan(match.Index, match.Length)

        Dim documentNewLine = GetDocumentNewLine(fullText)
        Dim replacementText = NormalizeNewLines(newText, documentNewLine)

        Dim newSourceText = sourceText.WithChanges(New TextChange(span, replacementText))

        Dim newSolution = solution.WithDocumentText(document.Id, newSourceText)

        If Not workspace.CanApplyChange(ApplyChangesKind.ChangeDocument) Then
            Throw New InvalidOperationException("The Visual Studio workspace does not support document text changes.")
        End If

        ' Important: TryApplyChanges must run on the UI thread.
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_cancellationToken)

        If Not workspace.TryApplyChanges(newSolution) Then
            Throw New InvalidOperationException("Visual Studio rejected the document edit.")
        End If

        Return New DocumentEditResult With {
            .Success = True,
            .DocumentId = document.Id.Id.ToString(),
            .FilePath = document.FilePath,
            .OldText = match.Value,
            .NewText = replacementText
        }

    End Function

    Private Shared Function FindDocument(solution As Solution, documentId As String, filePath As String) As Document

        If Not String.IsNullOrWhiteSpace(documentId) Then
            Dim byId = solution.Projects _
                    .SelectMany(Function(p) p.Documents) _
                    .FirstOrDefault(
                        Function(d)
                            Return String.Equals(
                                d.Id.Id.ToString(),
                                documentId,
                                StringComparison.OrdinalIgnoreCase)
                        End Function)

            If byId IsNot Nothing Then
                Return byId
            End If
        End If

        If Not String.IsNullOrWhiteSpace(filePath) Then
            Return solution.Projects _
                .SelectMany(Function(p) p.Documents) _
                .FirstOrDefault(
                    Function(d)
                        Return String.Equals(
                            d.FilePath,
                            filePath,
                            StringComparison.OrdinalIgnoreCase)
                    End Function)
        End If

        Return Nothing
    End Function

    Private Shared Function FindTextMatch(source As String, expectedText As String) As Match
        If String.IsNullOrEmpty(expectedText) Then
            Throw New ArgumentException("Expected text cannot be empty.", NameOf(expectedText))
        End If

        ' Normalize only the search text so we can split it consistently.
        Dim normalized = expectedText.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)

        Dim lines = normalized.Split(New String() {vbLf}, StringSplitOptions.None)

        ' Escape every line so source code characters are interpreted literally.
        Dim escapedLines = lines.Select(Function(line) Regex.Escape(line))

        ' Newlines may be CRLF, LF or CR in the actual document.
        Dim pattern = String.Join("(?:\r\n|\n|\r)", escapedLines)

        Dim matches = Regex.Matches(source, pattern, RegexOptions.CultureInvariant)

        If matches.Count = 0 Then
            Throw New InvalidOperationException("The expected text was not found in the document.")
        End If

        If matches.Count > 1 Then
            Throw New InvalidOperationException($"The expected text occurs {matches.Count} times. Provide a larger, unique source block.")
        End If

        Return matches(0)
    End Function

    Private Shared Function GetDocumentNewLine(text As String) As String
        If text.Contains(vbCrLf) Then
            Return vbCrLf
        End If

        If text.Contains(vbLf) Then
            Return vbLf
        End If

        If text.Contains(vbCr) Then
            Return vbCr
        End If

        ' New/single-line document: Windows default.
        Return vbCrLf
    End Function

    Private Shared Function NormalizeNewLines(text As String, newLine As String) As String

        If text Is Nothing Then
            Return String.Empty
        End If

        Return text.Replace(vbCrLf, vbLf) _
            .Replace(vbCr, vbLf) _
            .Replace(vbLf, newLine)
    End Function
End Class
