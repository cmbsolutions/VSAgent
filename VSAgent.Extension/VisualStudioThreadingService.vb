Imports Microsoft.VisualStudio.Shell

Public Class VisualStudioThreadingService
    Implements IVisualStudioThreadingService

    Public Sub ThrowIfNotOnMainThread() Implements IVisualStudioThreadingService.ThrowIfNotOnMainThread
        ThreadHelper.ThrowIfNotOnUIThread()
    End Sub

    Public Async Function SwitchToMainThreadAsync() As Task Implements IVisualStudioThreadingService.SwitchToMainThreadAsync
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()
    End Function
End Class
