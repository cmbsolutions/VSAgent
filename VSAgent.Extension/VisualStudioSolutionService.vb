Imports System.IO
Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports VSAgent.Protocol.DTO

Public Class VisualStudioSolutionService
    Implements ISolutionService

    Private ReadOnly _package As AsyncPackage

    Public Sub New(package As AsyncPackage)
        _package = package
    End Sub

    Public Async Function GetSolutionInfoAsync() As Task(Of SolutionInfo) _
        Implements ISolutionService.GetSolutionInfoAsync

        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()

        Dim solution =
            TryCast(
                Await _package.GetServiceAsync(GetType(SVsSolution)),
                IVsSolution)

        If solution Is Nothing Then
            Throw New InvalidOperationException(
                "The Visual Studio solution service is unavailable.")
        End If

        Dim solutionDirectory As String = Nothing
        Dim solutionFile As String = Nothing
        Dim userOptionsFile As String = Nothing

        Dim result = solution.GetSolutionInfo(
            solutionDirectory,
            solutionFile,
            userOptionsFile)

        ErrorHandler.ThrowOnFailure(result)

        Dim isOpen = Not String.IsNullOrWhiteSpace(solutionFile)

        Return New SolutionInfo With {
            .Name = If(
                isOpen,
                Path.GetFileNameWithoutExtension(solutionFile),
                Nothing),
            .FilePath = solutionFile,
            .DirectoryPath = solutionDirectory,
            .isOpen = isOpen
        }

    End Function

End Class