Imports System.ComponentModel
Imports System.Runtime.CompilerServices

Namespace Settings
    Public Class VSAgentSettings
        Implements INotifyPropertyChanged

        Public Property Model As String = ""
        Public Property ShowThinking As Boolean = True
        Public Property ReasoningEffort As String = "low"

        Private _numCtx As Integer = 32768
        Public Property NumCtx As Integer
            Get
                Return _numCtx
            End Get
            Set(value As Integer)
                If _numCtx <> value Then
                    _numCtx = value
                    NotifyPropertyChanged()
                End If
            End Set
        End Property

        Private _NumPredict As Integer = 8192
        Public Property NumPredict As Integer
            Get
                Return _NumPredict
            End Get
            Set(value As Integer)
                If _NumPredict <> value Then
                    _NumPredict = value
                    NotifyPropertyChanged()
                End If
            End Set
        End Property

        Public Property InferenceProviderBaseURL As String = "http://localhost:11434"

        Private _InferenceProviderModels As New List(Of String)
        Public Property InferenceProviderModels As List(Of String)
            Get
                Return _InferenceProviderModels
            End Get
            Set(value As List(Of String))
                _InferenceProviderModels = value
                NotifyPropertyChanged()
            End Set
        End Property

        Private _InferenceProvider As InferenceProviderType = InferenceProviderType.Ollama
        Public Property InferenceProvider As InferenceProviderType
            Get
                Return _InferenceProvider
            End Get
            Set(value As InferenceProviderType)
                If _InferenceProvider <> value Then
                    _InferenceProvider = value
                    NotifyPropertyChanged()
                End If
            End Set
        End Property

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Private Sub NotifyPropertyChanged(<CallerMemberName()> Optional ByVal propertyName As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub


    End Class

    Public Enum InferenceProviderType
        Ollama
        Strata
        OpenAI
    End Enum
End Namespace

