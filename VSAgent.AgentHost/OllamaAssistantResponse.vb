Namespace Ollama
    Public Class OllamaAssistantResponse

        Public Property Thinking As String
        Public Property Content As String

        Public Property ToolCalls As New List(Of OllamaToolCall)

    End Class
End Namespace
