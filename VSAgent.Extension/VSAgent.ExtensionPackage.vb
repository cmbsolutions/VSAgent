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

            ' All available services
            Dim solutionService As ISolutionService = New VisualStudioSolutionService(Me)
            Dim roslynWorkspaceService As IRoslynWorkspaceService = New VisualStudioRoslynWorkspaceService(Me)
            Dim documentService As IDocumentService = New VisualStudioDocumentService(Me)
            Dim findSymbolsService As ISymbolService = New VisualStudioFindSymbolsService(Me, cancellationToken)
            Dim roslynDiagnosticsService As IRoslynDiagnosticsService = New VisualStudioDiagnosticsService(Me, cancellationToken)
            Dim documentEditService As IDocumentEditService = New VisualStudioDocumentEditService(Me, cancellationToken)
            Dim buildService As IBuildService = New VisualStudioBuildService(Me, cancellationToken)

            ' All available tools
            Dim _registry = New ToolRegistry()
            _registry.Register(New Tools.PingTool())
            _registry.Register(New Tools.GetSolutionInfoTool(solutionService))
            _registry.Register(New Tools.GetProjectsTool(solutionService))
            _registry.Register(New Tools.GetRoslynProjectsTool(roslynWorkspaceService))
            _registry.Register(New Tools.GetActiveDocumentTool(documentService))
            _registry.Register(New Tools.ReadDocumentTool(documentService))
            _registry.Register(New Tools.FindSymbolsTool(findSymbolsService))
            _registry.Register(New Tools.FindReferencesTool(findSymbolsService))
            _registry.Register(New Tools.GetRoslynDiagnosticsTool(roslynDiagnosticsService))
            _registry.Register(New Tools.ApplyDocumentEditTool(documentEditService))
            _registry.Register(New Tools.BuildSolutionTool(buildService))

            ' This one always last!!!!
            _registry.Register(New Tools.GetAvailableToolsTool(_registry))

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