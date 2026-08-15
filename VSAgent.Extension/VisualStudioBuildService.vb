Imports System.Threading
Imports EnvDTE
Imports EnvDTE80
Imports Microsoft.VisualStudio.Shell
Imports VSAgent.Protocol.DTO

Public Class VisualStudioBuildService
    Implements IBuildService

    Private ReadOnly _package As AsyncPackage
    Private ReadOnly _cancellationToken As CancellationToken

    Public Sub New(package As AsyncPackage, cancellationToken As CancellationToken)

        _package = package
        _cancellationToken = cancellationToken
    End Sub

    Public Async Function BuildSolutionAsync() As Task(Of BuildResult) Implements IBuildService.BuildSolutionAsync

        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_cancellationToken)

        Dim dte = TryCast(Await _package.GetServiceAsync(GetType(DTE)), DTE2)

        If dte Is Nothing Then
            Throw New InvalidOperationException("The Visual Studio DTE service is unavailable.")
        End If

        Dim solutionBuild = dte.Solution.SolutionBuild

        solutionBuild.Build(WaitForBuildToFinish:=True)

        Return New BuildResult With {
            .Success = solutionBuild.LastBuildInfo = 0,
            .LastBuildInfo = solutionBuild.LastBuildInfo,
            .Configuration = solutionBuild.ActiveConfiguration.Name
        }
    End Function

    Public Async Function BuildProjectAsync(ProjectId As String) As Task(Of BuildResult) Implements IBuildService.BuildProjectAsync
        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)

        Dim roslynProject = workspace.CurrentSolution.Projects.
        FirstOrDefault(
            Function(p)
                Return String.Equals(
                    p.Id.Id.ToString(),
                    ProjectId,
                    StringComparison.OrdinalIgnoreCase)
            End Function)

        If roslynProject Is Nothing Then
            Throw New InvalidOperationException("The requested project could not be found.")
        End If

        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_cancellationToken)

        Dim dte = TryCast(Await _package.GetServiceAsync(GetType(EnvDTE.DTE)), EnvDTE80.DTE2)

        If dte Is Nothing Then
            Throw New InvalidOperationException("The Visual Studio DTE service is unavailable.")
        End If

        Dim solutionBuild = dte.Solution.SolutionBuild

        Dim configuration = solutionBuild.ActiveConfiguration.Name

        Dim dteProject = FindDteProjectByFilePath(dte.Solution.Projects, roslynProject.FilePath)

        If dteProject Is Nothing Then
            Throw New InvalidOperationException($"Could not locate project '{roslynProject.Name}' in the Visual Studio project hierarchy.")
        End If

        solutionBuild.BuildProject(configuration, dteProject.UniqueName, True)

        Return New BuildResult With {
            .Success = solutionBuild.LastBuildInfo = 0,
            .LastBuildInfo = solutionBuild.LastBuildInfo,
            .Configuration = configuration
        }
    End Function

    Private Function FindDteProjectByFilePath(projects As EnvDTE.Projects, projectFilePath As String) As EnvDTE.Project

        ThreadHelper.ThrowIfNotOnUIThread()

        If projects Is Nothing OrElse String.IsNullOrWhiteSpace(projectFilePath) Then
            Return Nothing
        End If

        For Each project As EnvDTE.Project In projects
            If project Is Nothing Then
                Continue For
            End If

            Try
                If Not String.IsNullOrWhiteSpace(project.FullName) AndAlso String.Equals(IO.Path.GetFullPath(project.FullName), IO.Path.GetFullPath(projectFilePath), StringComparison.OrdinalIgnoreCase) Then
                    Return project
                End If
            Catch
                ' Solution folders and some virtual projects
                ' may not expose FullName normally.
            End Try

            If project.ProjectItems IsNot Nothing Then
                For Each item As EnvDTE.ProjectItem In project.ProjectItems

                    If item.SubProject Is Nothing Then
                        Continue For
                    End If

                    Dim found = FindDteProjectRecursive(item.SubProject, projectFilePath)

                    If found IsNot Nothing Then
                        Return found
                    End If
                Next
            End If
        Next

        Return Nothing
    End Function

    Private Function FindDteProjectRecursive(project As EnvDTE.Project, projectFilePath As String) As EnvDTE.Project

        ThreadHelper.ThrowIfNotOnUIThread()

        Try
            If Not String.IsNullOrWhiteSpace(project.FullName) AndAlso
               String.Equals(
                   IO.Path.GetFullPath(project.FullName),
                   IO.Path.GetFullPath(projectFilePath),
                   StringComparison.OrdinalIgnoreCase) Then

                Return project
            End If
        Catch
        End Try

        If project.ProjectItems Is Nothing Then
            Return Nothing
        End If

        For Each item As EnvDTE.ProjectItem In project.ProjectItems
            If item.SubProject Is Nothing Then
                Continue For
            End If

            Dim found = FindDteProjectRecursive(item.SubProject, projectFilePath)

            If found IsNot Nothing Then
                Return found
            End If
        Next

        Return Nothing
    End Function
End Class