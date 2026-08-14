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
End Class