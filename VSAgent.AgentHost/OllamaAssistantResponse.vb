Namespace Ollama
    Public Class OllamaAssistantResponse

        Public Property Thinking As String
        Public Property Content As String

        Public Property ToolCalls As New List(Of OllamaToolCall)

        Public Property Statistics As String

    End Class
End Namespace
