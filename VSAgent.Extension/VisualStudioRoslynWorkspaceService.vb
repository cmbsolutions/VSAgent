Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.Shell
Imports VSAgent.Protocol.DTO

Public Class VisualStudioRoslynWorkspaceService
    Implements IRoslynWorkspaceService

    Private ReadOnly _package As AsyncPackage

    Public Sub New(package As AsyncPackage)

        If package Is Nothing Then
            Throw New ArgumentNullException(NameOf(package))
        End If

        _package = package
    End Sub

    Public Async Function GetProjectsAsync() As Task(Of IReadOnlyList(Of RoslynProjectInfo)) Implements IRoslynWorkspaceService.GetProjectsAsync

        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)

        Dim result As New List(Of RoslynProjectInfo)

        For Each project As Project In workspace.CurrentSolution.Projects

            result.Add(
                New RoslynProjectInfo With {
                    .Id = project.Id.Id.ToString(),
                    .Name = project.Name,
                    .AssemblyName = project.AssemblyName,
                    .Language = project.Language,
                    .FilePath = project.FilePath,
                    .OutputFilePath = project.OutputFilePath,
                    .DocumentCount = project.DocumentIds.Count,
                    .ProjectReferenceCount = project.ProjectReferences.Count(),
                    .MetadataReferenceCount = project.MetadataReferences.Count()
                })

        Next

        Return result

    End Function

    Public Async Function GetProjectDocumentsAsync(projectId As String) As Task(Of IReadOnlyList(Of RoslynDocumentInfo)) Implements IRoslynWorkspaceService.GetProjectDocumentsAsync
        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)

        Dim result As New List(Of RoslynDocumentInfo)

        Dim solution = workspace.CurrentSolution

        Dim project = solution.Projects.FirstOrDefault(
            Function(p)
                Return String.Equals(p.Id.Id.ToString(), projectId, StringComparison.OrdinalIgnoreCase)
            End Function)

        If project Is Nothing Then
            Throw New InvalidOperationException("The requested project could not be found.")
        End If

        For Each document As Document In project.Documents
            result.Add(
                New RoslynDocumentInfo With {
                    .DocumentID = document.Id.Id.ToString(),
                    .Name = document.Name,
                    .FilePath = document.FilePath,
                    .ProjectID = document.Project.Id.Id.ToString(),
                    .Folders = document.Folders.ToList
                })
        Next

        Return result
    End Function
End Class