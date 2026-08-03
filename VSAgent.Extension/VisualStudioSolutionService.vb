Imports System.IO
Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Debugger.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports VSAgent.Protocol.DTO

Public Class VisualStudioSolutionService
    Implements ISolutionService

    Private ReadOnly _package As AsyncPackage

    Private Shared ReadOnly MiscellaneousFilesProjectGuid As New Guid("A2FE74E1-B743-11D0-AE1A-00A0C90FFFC3")

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
            .IsOpen = isOpen
        }

    End Function

    Public Async Function GetProjectsAsync() As Task(Of IReadOnlyList(Of ProjectInfo)) Implements ISolutionService.GetProjectsAsync
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()

        Dim solution = TryCast(Await _package.GetServiceAsync(GetType(SVsSolution)), IVsSolution)

        If solution Is Nothing Then
            Throw New InvalidOperationException("The Visual Studio solution service is unavailable.")
        End If

        Dim enumerator As IEnumHierarchies = Nothing

        ErrorHandler.ThrowOnFailure(solution.GetProjectEnum(CUInt(__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION), Guid.Empty, enumerator))

        Dim projects As New List(Of ProjectInfo)
        Dim hierarchies(0) As IVsHierarchy
        Dim fetched As UInteger

        Do
            fetched = 0

            Dim result = enumerator.Next(1UI, hierarchies, fetched)

            If result <> VSConstants.S_OK OrElse fetched = 0 Then
                Exit Do
            End If

            Dim hierarchy = hierarchies(0)

            projects.Add(GetProjectInfo(hierarchy))

        Loop

        Return projects

    End Function

    Private Shared Function GetProjectInfo(hierarchy As IVsHierarchy) As ProjectInfo

        ThreadHelper.ThrowIfNotOnUIThread()

        Dim projectGuid As Guid

        If ErrorHandler.Failed(hierarchy.GetGuidProperty(VSConstants.VSITEMID_ROOT, CInt(__VSHPROPID.VSHPROPID_ProjectIDGuid), projectGuid)) Then
            Return Nothing
        End If

        If projectGuid = MiscellaneousFilesProjectGuid Then
            Return Nothing
        End If

        Dim name As String = GetHierarchyProperty(hierarchy, __VSHPROPID.VSHPROPID_Name)
        Dim projectFilePath As String = GetHierarchyProperty(hierarchy, __VSHPROPID.VSHPROPID_ProjectDir)

        Dim targetFramework As String = GetHierarchyProperty(hierarchy, __VSHPROPID4.VSHPROPID_TargetFrameworkMoniker)

        Dim projectTypeGuid As Guid
        hierarchy.GetGuidProperty(VSConstants.VSITEMID_ROOT, CInt(__VSHPROPID.VSHPROPID_TypeGuid), projectTypeGuid)

        Dim filepath As String = GetProjectFilePath(hierarchy)

        If String.IsNullOrWhiteSpace(projectFilePath) OrElse String.Equals(projectFilePath, "UNDEFINED", StringComparison.OrdinalIgnoreCase) Then
            Return Nothing
        End If

        Dim language As String = "Unknown"

        Select Case IO.Path.GetExtension(filepath).ToLowerInvariant()
            Case ".csproj"
                language = "C#"
            Case ".vbproj"
                language = "VB.NET"
            Case ".fsproj"
                language = "F#"
            Case ".vcxproj"
                language = "C++"
        End Select

        Return New ProjectInfo With {
            .Name = name,
            .UniqueName = name,
            .FilePath = filepath,
            .Language = language,
            .ProjectGuid = projectGuid.ToString(),
            .ProjectTypeGuid = projectTypeGuid.ToString(),
            .TargetFramework = targetFramework
        }
    End Function

    Private Shared Function GetHierarchyProperty(hierarchy As IVsHierarchy, propertyId As __VSHPROPID) As String

        ThreadHelper.ThrowIfNotOnUIThread()

        Dim value As Object = Nothing

        Dim result = hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, CInt(propertyId), value)

        If ErrorHandler.Failed(result) OrElse value Is Nothing Then
            Return Nothing
        End If

        Return value.ToString()
    End Function

    Private Shared Function GetProjectFilePath(hierarchy As IVsHierarchy) As String

        ThreadHelper.ThrowIfNotOnUIThread()

        Dim project = TryCast(hierarchy, IVsProject)

        If project Is Nothing Then
            Return Nothing
        End If

        Dim projectFilePath As String = Nothing

        Dim result = project.GetMkDocument(VSConstants.VSITEMID_ROOT, projectFilePath)

        If ErrorHandler.Failed(result) Then
            Return Nothing
        End If

        Return projectFilePath
    End Function
End Class