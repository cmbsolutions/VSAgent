Public Interface IVisualStudioThreadingService
    Function SwitchToMainThreadAsync() As Task
    Sub ThrowIfNotOnMainThread()
    Sub ThrowIfNotOnUIThread()
End Interface