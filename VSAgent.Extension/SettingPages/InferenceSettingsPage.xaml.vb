Imports System.Windows
Imports VSAgent.Protocol.Settings



Public Class InferenceSettingsPage

        Private _loading As Boolean = True

        Public Sub New()
            InitializeComponent()

            AddHandler DataContextChanged,
                AddressOf InferenceSettingsPage_DataContextChanged
        End Sub

        Private Sub InferenceSettingsPage_DataContextChanged(
            sender As Object,
            e As DependencyPropertyChangedEventArgs)

            Dim config = TryCast(DataContext, VSAgentSettings)

            If config Is Nothing Then Return

            _loading = True

            Select Case config.ReasoningEffort.ToLowerInvariant()
                Case "medium"
                    rbMedium.IsChecked = True

                Case "xhigh"
                    rbXHigh.IsChecked = True

                Case Else
                    rbLow.IsChecked = True
            End Select

            _loading = False

        End Sub

        Private Sub Reasoning_Checked(sender As Object, e As RoutedEventArgs)

            If _loading Then Return

            Dim config = TryCast(DataContext, VSAgentSettings)

            If config Is Nothing Then Return

            If rbLow.IsChecked = True Then
                config.ReasoningEffort = "low"

            ElseIf rbMedium.IsChecked = True Then
                config.ReasoningEffort = "medium"

            ElseIf rbXHigh.IsChecked = True Then
                config.ReasoningEffort = "xhigh"
            End If

        End Sub

    End Class
