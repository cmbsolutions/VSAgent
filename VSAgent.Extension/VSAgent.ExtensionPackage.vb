Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop

Namespace VSAgent.Extension
    <PackageRegistration(UseManagedResourcesOnly:=True, AllowsBackgroundLoading:=True)>
    <ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)>
    <Guid(VSAgentExtensionPackage.PackageGuidString)>
    Public NotInheritable Class VSAgentExtensionPackage
        Inherits AsyncPackage

        Public Const PackageGuidString As String = "431c89ff-77e1-4be3-bb40-ab3b50892bae"
        Private _agentServer As AgentPipeServer

        Protected Overrides Async Function InitializeAsync(ByVal cancellationToken As CancellationToken, ByVal progress As IProgress(Of ServiceProgressData)) As Task
            Await Me.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken)
            'Await MyBase.InitializeAsync(cancellationToken, progress)

            Dim solutionService As ISolutionService = New VisualStudioSolutionService(Me)

            Dim _registry = New ToolRegistry()
            _registry.Register(New Tools.PingTool())
            _registry.Register(New Tools.GetSolutionInfoTool(solutionService))
            _registry.Register(New Tools.GetProjectsTool(solutionService))

            _agentServer = New AgentPipeServer(_registry)
            _agentServer.Start()
        End Function

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing AndAlso _agentServer IsNot Nothing Then

                Try
                    _agentServer.StopAsync().
                        GetAwaiter().
                        GetResult()
                Catch ex As Exception
                    Debug.WriteLine($"Could not stop VSAgent server: {ex}")
                End Try

                _agentServer = Nothing
            End If

            MyBase.Dispose(disposing)
        End Sub
    End Class
End Namespace