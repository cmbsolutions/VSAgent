Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices
Imports Microsoft.VisualStudio.Shell

Friend Class RoslynWorkspaceProvider
    Public Shared Async Function GetWorkspaceAsync(package As AsyncPackage) As Task(Of VisualStudioWorkspace)

        ' We need the UI thread from visual studio to get the workspace service, so we switch to it here. When we have it we can switch back to the background thread to do the rest of the work.
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()

        Dim componentModel = TryCast(Await package.GetServiceAsync(GetType(SComponentModel)), IComponentModel)

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
