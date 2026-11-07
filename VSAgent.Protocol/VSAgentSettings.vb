Namespace Settings
    Public Class VSAgentSettings
        Public Property Model As String = ""
        Public Property ShowThinking As Boolean = True
        Public Property ReasoningEffort As String = "low"
        Public Property NumCtx As Integer = 32768
        Public Property NumPredict As Integer = 8192
        Public Property OllamaBaseURL As String = "http://localhost:11434"
        Public Property OllamaModels As New List(Of String)
    End Class
End Namespace

