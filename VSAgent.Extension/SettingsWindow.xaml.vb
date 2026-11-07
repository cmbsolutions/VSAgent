Imports System.Windows
Imports VSAgent.Protocol.Settings

Partial Public Class SettingsWindow

    Public ReadOnly Property SavedSettings As VSAgentSettings

    Public Sub New(Optional availableModels As IEnumerable(Of String) = Nothing)

        InitializeComponent()

        SavedSettings = Settings.SettingsManager.Load()

        If availableModels IsNot Nothing Then
            For Each modelName In availableModels
                cmbModel.Items.Add(modelName)
            Next
        End If

        ' Preserve the configured model even if Ollama is offline.
        If Not String.IsNullOrWhiteSpace(SavedSettings.Model) AndAlso
           Not cmbModel.Items.Contains(SavedSettings.Model) Then

            cmbModel.Items.Add(SavedSettings.Model)
        End If

        ApplySettingsToControls(SavedSettings)

    End Sub

    Private Sub ApplySettingsToControls(settings As VSAgentSettings)

        cmbModel.SelectedItem = settings.Model

        chkShowThinking.IsChecked = settings.ShowThinking

        Select Case settings.ReasoningEffort?.ToLowerInvariant()
            Case "medium"
                rbMedium.IsChecked = True
            Case "xhigh"
                rbXHigh.IsChecked = True
            Case Else
                rbLow.IsChecked = True
        End Select

        sldNumCtx.Value = Math.Max(
            sldNumCtx.Minimum,
            Math.Min(sldNumCtx.Maximum, settings.NumCtx))

        sldNumPredict.Value = Math.Max(
            sldNumPredict.Minimum,
            Math.Min(sldNumPredict.Maximum, settings.NumPredict))

        UpdateSliderLabels()

    End Sub

    Private Sub UpdateSliderLabels()

        If txtNumCtx IsNot Nothing AndAlso
           sldNumCtx IsNot Nothing Then

            txtNumCtx.Text =
                CInt(sldNumCtx.Value).ToString("N0") & " tokens"
        End If

        If txtNumPredict IsNot Nothing AndAlso
           sldNumPredict IsNot Nothing Then

            txtNumPredict.Text =
                CInt(sldNumPredict.Value).ToString("N0") & " tokens"
        End If

    End Sub

    Private Sub Slider_ValueChanged(
        sender As Object,
        e As RoutedPropertyChangedEventArgs(Of Double))

        UpdateSliderLabels()

    End Sub

    Private Sub Reset_Click(sender As Object, e As RoutedEventArgs)

        Dim defaults As New VSAgentSettings()

        If Not cmbModel.Items.Contains(defaults.Model) Then
            cmbModel.Items.Add(defaults.Model)
        End If

        ApplySettingsToControls(defaults)

    End Sub

    Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)

        DialogResult = False

    End Sub

    Private Sub Save_Click(sender As Object, e As RoutedEventArgs)

        Dim selectedModel = TryCast(cmbModel.SelectedItem, String)

        If String.IsNullOrWhiteSpace(selectedModel) Then
            MessageBox.Show(
                Me,
                "Please select a model.",
                "VSAgent Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Warning)

            Return
        End If

        Dim reasoningEffort As String = "low"

        If rbMedium.IsChecked = True Then
            reasoningEffort = "medium"
        ElseIf rbXHigh.IsChecked = True Then
            reasoningEffort = "xhigh"
        End If

        Dim updatedSettings As New VSAgentSettings With {
            .Model = selectedModel,
            .ShowThinking = chkShowThinking.IsChecked = True,
            .reasoningEffort = reasoningEffort,
            .NumCtx = CInt(sldNumCtx.Value),
            .NumPredict = CInt(sldNumPredict.Value)
        }

        Try
            Settings.SettingsManager.Save(updatedSettings)

            SavedSettings.Model = updatedSettings.Model
            SavedSettings.ShowThinking = updatedSettings.ShowThinking
            SavedSettings.ReasoningEffort = updatedSettings.ReasoningEffort
            SavedSettings.NumCtx = updatedSettings.NumCtx
            SavedSettings.NumPredict = updatedSettings.NumPredict

            DialogResult = True

        Catch ex As Exception
            MessageBox.Show(
                Me,
                $"Unable to save settings: {ex.Message}",
                "VSAgent Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Error)
        End Try

    End Sub

End Class
