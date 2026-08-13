Imports EnvDTE
Imports EnvDTE80
Imports Microsoft.VisualStudio.Shell
Imports VSAgent.Protocol.DTO

Public Class VisualStudioDocumentService
    Implements IDocumentService

    Private ReadOnly _package As AsyncPackage

    Public Sub New(package As AsyncPackage)

        If package Is Nothing Then
            Throw New ArgumentNullException(NameOf(package))
        End If

        _package = package
    End Sub

    Public Async Function GetActiveDocumentAsync() As Task(Of ActiveDocumentInfo) Implements IDocumentService.GetActiveDocumentAsync
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()

        Dim dte = TryCast(Await _package.GetServiceAsync(GetType(DTE)), DTE2)

        If dte Is Nothing Then
            Throw New InvalidOperationException("The Visual Studio DTE service is unavailable.")
        End If

        Dim document = dte.ActiveDocument

        If document Is Nothing Then
            Return Nothing
        End If


        Dim result As New ActiveDocumentInfo With {
            .Name = document.Name,
            .FilePath = document.FullName
        }

        Dim textDocument = TryCast(document.Object("TextDocument"), TextDocument)

        If textDocument IsNot Nothing Then

            Dim selection = textDocument.Selection

            If selection IsNot Nothing Then

                result.CaretLine = selection.ActivePoint.Line
                result.CaretColumn = selection.ActivePoint.DisplayColumn

                result.HasSelection = Not selection.IsEmpty

                If result.HasSelection Then
                    result.SelectionText = selection.Text

                    result.SelectionStartLine = selection.TopPoint.Line

                    result.SelectionStartColumn = selection.TopPoint.DisplayColumn

                    result.SelectionEndLine = selection.BottomPoint.Line

                    result.SelectionEndColumn = selection.BottomPoint.DisplayColumn
                End If

            End If

        End If

        If document.ProjectItem IsNot Nothing AndAlso document.ProjectItem.ContainingProject IsNot Nothing Then
            result.ProjectName = document.ProjectItem.ContainingProject.Name
        End If

        result.Language = document.Language

        Return result
    End Function

    Private Shared Function GetLanguageFromFilePath(filePath As String) As String
        If String.IsNullOrWhiteSpace(filePath) Then
            Return "Unknown"
        End If

        Select Case IO.Path.GetExtension(filePath).ToLowerInvariant()
            Case ".vb"
                Return "VB.NET"
            Case ".cs"
                Return "C#"
            Case ".fs"
                Return "F#"
            Case ".cpp", ".cc", ".cxx", ".h", ".hpp"
                Return "C++"
            Case ".js"
                Return "JavaScript"
            Case ".ts"
                Return "TypeScript"
            Case ".php"
                Return "PHP"
            Case ".json"
                Return "JSON"
            Case ".xml"
                Return "XML"
            Case Else
                Return "Unknown"
        End Select

    End Function

    Public Async Function ReadDocumentAsync(Optional filePath As String = Nothing, Optional documentId As String = Nothing) As Task(Of RoslynDocument) Implements IDocumentService.ReadDocumentAsync

        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)

        Dim document As Microsoft.CodeAnalysis.Document = Nothing

        If documentId IsNot Nothing Then
            document = workspace.CurrentSolution.Projects.
                SelectMany(Function(p) p.Documents).
                FirstOrDefault(Function(d) String.Equals(d.Id.Id.ToString, documentId, StringComparison.OrdinalIgnoreCase))
        End If

        If document Is Nothing AndAlso filePath IsNot Nothing Then
            document = workspace.CurrentSolution.Projects.
                SelectMany(Function(p) p.Documents).
                FirstOrDefault(Function(d) String.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
        End If

        If document Is Nothing Then
            Return Nothing
        End If

        Dim docText = Await document.GetTextAsync().ConfigureAwait(False)

        Return New RoslynDocument With {
            .DocumentID = document.Id.Id.ToString,
            .FilePath = document.FilePath,
            .Language = GetLanguageFromFilePath(document.FilePath),
            .Name = document.Name,
            .ProjectName = document.Project.Name,
            .Text = docText.ToString,
            .Version = "1"
        }
    End Function
End Class
