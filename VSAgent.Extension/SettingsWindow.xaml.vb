Imports System.Windows
Imports System.Windows.Controls
Imports VSAgent.Protocol.Settings

Partial Public Class SettingsWindow

    Private _savedSettings As VSAgentSettings
    Public ReadOnly Property SavedSettings As VSAgentSettings
        Get
            Return _savedSettings
        End Get
    End Property

    Private _workingSettings As VSAgentSettings

    Private ReadOnly _settingsPages As New Dictionary(Of String, Page) From {
        {"Models", New ModelsSettingsPage()},
        {"Inference", New InferenceSettingsPage()},
        {"Agent", New AgentSettingsPage()},
        {"Appearance", New AppearanceSettingsPage()},
        {"Connection", New ConnectionSettingsPage()}
    }

    Private Sub InitializeSettingsPages(settings As VSAgentSettings)

        _workingSettings = settings

        For Each page In _settingsPages.Values
            page.DataContext = _workingSettings
        Next

        lstSettingsPages.SelectedIndex = 0

    End Sub

    Private Sub lstSettingsPages_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)

        Dim selected = TryCast(lstSettingsPages.SelectedItem, ListBoxItem)

        If selected Is Nothing Then Return

        Dim pageName As String = CStr(selected.Tag)

        settingsFrame.Navigate(_settingsPages(pageName))

    End Sub


    Public Sub New(Optional availableModels As IEnumerable(Of String) = Nothing)

        InitializeComponent()

        _savedSettings = Settings.SettingsManager.Load()

        InitializeSettingsPages(_savedSettings)

    End Sub

    Private Sub Reset_Click(sender As Object, e As RoutedEventArgs)

        Dim defaults As New VSAgentSettings()

    End Sub

    Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)

        DialogResult = False

    End Sub

    Private Sub Save_Click(sender As Object, e As RoutedEventArgs)

        Try
            Settings.SettingsManager.Save(_workingSettings)

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
