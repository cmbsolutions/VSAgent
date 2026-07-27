Imports Microsoft.VisualStudio.Shell
Imports System
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Threading.Tasks

Namespace VSAgent.Extension
    <PackageRegistration(UseManagedResourcesOnly:=True, AllowsBackgroundLoading:=True)>
    <Guid(VSAgent.ExtensionPackage.PackageGuidString)>
    Public NotInheritable Class VSAgentExtensionPackage
        Inherits AsyncPackage

        Public Const PackageGuidString As String = "431c89ff-77e1-4be3-bb40-ab3b50892bae"

        Protected Overrides Async Function InitializeAsync(ByVal cancellationToken As CancellationToken, ByVal progress As IProgress(Of ServiceProgressData)) As Task
            Await Me.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken)
        End Function
    End Class
End Namespace