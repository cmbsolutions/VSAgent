Imports System.Text
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

    Private AddedDocumentIds As New List(Of String)

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

        ' First try to get the document by documentId
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

        ' Find by filepath as fallback
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

    Private Shared Function FindTextMatch(source As String, oldText As String) As Match
        If String.IsNullOrEmpty(oldText) Then
            Throw New ArgumentException("Expected text cannot be empty.", NameOf(oldText))
        End If

        ' Normalize only the search text so we can split it consistently.
        Dim normalized = NormalizeNewLines(oldText)

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

        text = NormalizeNewLines(text)

        Return text.Replace(vbCrLf, vbLf) _
            .Replace(vbCr, vbLf) _
            .Replace(vbLf, newLine)
    End Function

    Private Shared Function NormalizeNewLines(text As String) As String

        If text Is Nothing Then
            Return String.Empty
        End If

        ' First normalize \r\n, \r, \n to vbLf
        text = text.Replace("\r\n", vbLf).Replace("\r", vbLf).Replace("\n", vbLf)

        ' then in case of the agent sending actualy vbCrLf or vbCr
        Return text.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
    End Function

    Public Async Function AddDocumentAsync(projectId As String, name As String, text As String, folders As IReadOnlyList(Of String)) As Task(Of AddDocumentResult) Implements IDocumentEditService.AddDocumentAsync
        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)

        Dim solution = workspace.CurrentSolution

        Dim project = solution.Projects.FirstOrDefault(
            Function(p)
                Return String.Equals(p.Id.Id.ToString(), projectId, StringComparison.OrdinalIgnoreCase)
            End Function)

        If project Is Nothing Then
            Throw New InvalidOperationException("The requested project could not be found.")
        End If

        If String.IsNullOrWhiteSpace(name) Then
            Throw New ArgumentException("Document name is required.", NameOf(name))
        End If

        Dim existing = project.Documents.FirstOrDefault(
            Function(d)
                Return String.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase)
            End Function)

        If existing IsNot Nothing Then
            Throw New InvalidOperationException($"A document named '{name}' already exists in project '{project.Name}'.")
        End If

        Dim folderList = If(folders, Array.Empty(Of String)())

        Dim documentNewLine = GetDocumentNewLine(text)
        Dim newText = NormalizeNewLines(text, documentNewLine)

        Dim document = project.AddDocument(name, SourceText.From(If(newText, String.Empty), Encoding.UTF8), folders:=folderList)

        Dim newSolution = document.Project.Solution

        ' Important: TryApplyChanges must run on the UI thread.
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_cancellationToken)

        If Not workspace.TryApplyChanges(newSolution) Then
            Throw New InvalidOperationException("Visual Studio rejected the document creation.")
        End If

        ' Resolve again from the updated workspace.
        Dim updatedDocument = workspace.CurrentSolution.GetDocument(document.Id)

        AddedDocumentIds.Add(document.Id.Id.ToString())

        Return New AddDocumentResult With {
            .Success = True,
            .DocumentId = document.Id.Id.ToString(),
            .ProjectId = project.Id.Id.ToString(),
            .ProjectName = project.Name,
            .Name = name,
            .FilePath = updatedDocument?.FilePath
        }
    End Function

    Public Async Function RemoveDocumentAsync(projectId As String, documentId As String) As Task(Of RemoveDocumentResult) Implements IDocumentEditService.RemoveDocumentAsync
        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)

        Dim solution = workspace.CurrentSolution

        Dim project = solution.Projects.FirstOrDefault(
            Function(p)
                Return String.Equals(p.Id.Id.ToString(), projectId, StringComparison.OrdinalIgnoreCase)
            End Function)

        If project Is Nothing Then
            Throw New InvalidOperationException("The requested project could not be found.")
        End If

        Dim existing = project.Documents.FirstOrDefault(
            Function(d)
                Return String.Equals(d.Id.Id.ToString, documentId, StringComparison.OrdinalIgnoreCase)
            End Function)

        If existing Is Nothing Then
            Throw New InvalidOperationException($"Document ID {documentId} does not exists in project '{project.Name}'.")
        End If

        If Not AddedDocumentIds.Contains(existing.Id.Id.ToString) Then
            Throw New InvalidOperationException($"Document ID {documentId} was not created by you. You can only remove documents you created.")
        End If

        Dim document = project.RemoveDocument(existing.Id)

        Dim newSolution = project.Solution

        ' Important: TryApplyChanges must run on the UI thread.
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_cancellationToken)

        If Not workspace.TryApplyChanges(newSolution) Then
            Throw New InvalidOperationException("Visual Studio rejected the removal of the document.")
        End If

        AddedDocumentIds.Remove(documentId)

        Return New RemoveDocumentResult With {
            .Success = True
        }
    End Function
End Class
