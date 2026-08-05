Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices
Imports Microsoft.VisualStudio.Shell
Imports VSAgent.Protocol.DTO

Public Class VisualStudioRoslynWorkspaceService
    Implements IRoslynWorkspaceService

    Private ReadOnly _package As AsyncPackage
    Private ReadOnly _threadingService As IVisualStudioThreadingService

    Public Sub New(package As AsyncPackage, threadingService As IVisualStudioThreadingService)

        If package Is Nothing Then
            Throw New ArgumentNullException(NameOf(package))
        End If

        _package = package
        _threadingService = threadingService
    End Sub

    Public Async Function GetProjectsAsync() As Task(Of IReadOnlyList(Of RoslynProjectInfo)) Implements IRoslynWorkspaceService.GetProjectsAsync

        Dim workspace = Await GetWorkspaceAsync()

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

    Private Async Function GetWorkspaceAsync() As Task(Of VisualStudioWorkspace)

        ' We need the UI thread from visual studio to get the workspace service, so we switch to it here. When we have it we can switch back to the background thread to do the rest of the work.
        Await _threadingService.SwitchToMainThreadAsync()

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