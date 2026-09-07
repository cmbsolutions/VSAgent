Namespace Events
    Public Class AgentHostEvent
        Public Property Type As String ' For now only thinking, content, toolStarted, toolCompleted, toolFailed or statistics
        Public Property Text As String
        Public Property ToolName As String
        Public Property ActionDescription As String
    End Class
End Namespace
