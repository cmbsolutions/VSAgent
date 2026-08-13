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

        Dim firstIndex = fullText.IndexOf(oldText, StringComparison.Ordinal)

        If firstIndex < 0 Then
            Throw New InvalidOperationException("The expected text was not found in the document.")
        End If

        Dim secondIndex = fullText.IndexOf(oldText, firstIndex + oldText.Length, StringComparison.Ordinal)

        If secondIndex >= 0 Then
            Throw New InvalidOperationException("The expected text occurs more than once. A more specific oldText value is required.")
        End If

        Dim span As New TextSpan(firstIndex, oldText.Length)

        Dim newSourceText = sourceText.WithChanges(New TextChange(span, If(newText, String.Empty)))

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
            .OldText = oldText,
            .NewText = newText
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
End Class
