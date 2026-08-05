Public Interface IVisualStudioThreadingService
    Function SwitchToMainThreadAsync() As Task
    Sub ThrowIfNotOnMainThread()
End Interface