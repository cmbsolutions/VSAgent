Namespace InferenceProvider
    Public Class InferenceProviderAssistantResponse

        Public Property Thinking As String
        Public Property Content As String

        Public Property ToolCalls As New List(Of InferenceProviderToolCall)

        Public Property Statistics As String

        Public Property DoneReason As String
        Public Property PromptEvalCount As Integer
        Public Property EvalCount As Integer
    End Class
End Namespace
