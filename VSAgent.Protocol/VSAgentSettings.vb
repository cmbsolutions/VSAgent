Imports System.ComponentModel

Namespace Settings
    Public Class VSAgentSettings
        Implements INotifyPropertyChanged

        Public Property Model As String = ""
        Public Property ShowThinking As Boolean = True
        Public Property ReasoningEffort As String = "low"
        Public Property NumCtx As Integer = 32768
        Public Property NumPredict As Integer = 8192
        Public Property InferenceProviderBaseURL As String = "http://localhost:11434"
        Public Property InferenceProviderModels As New List(Of String)
        Public Property InferenceProvider As InferenceProviderType = InferenceProviderType.Ollama

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    End Class

    Public Enum InferenceProviderType
        Ollama
        Strata
        OpenAI
    End Enum
End Namespace

