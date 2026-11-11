Imports System.Windows
Imports System.Windows.Controls
Imports VSAgent.Protocol.Settings


Public Class ModelsSettingsPage

    Private _loading As Boolean = True

    Public Sub New()
        InitializeComponent()

        cmbProvider.ItemsSource =
            [Enum].GetValues(GetType(InferenceProviderType))

        AddHandler DataContextChanged,
            AddressOf ModelsSettingsPage_DataContextChanged
    End Sub

    Private Sub ModelsSettingsPage_DataContextChanged(
        sender As Object,
        e As DependencyPropertyChangedEventArgs)

        Dim config = TryCast(DataContext, VSAgentSettings)

        If config Is Nothing Then Return

        _loading = True

        cmbProvider.SelectedItem = config.InferenceProvider

        _loading = False

    End Sub

    Private Sub cmbProvider_SelectionChanged(
        sender As Object,
        e As SelectionChangedEventArgs)

        If _loading Then Return

        Dim config = TryCast(DataContext, VSAgentSettings)

        If config Is Nothing OrElse
           cmbProvider.SelectedItem Is Nothing Then Return

        config.InferenceProvider =
            DirectCast(cmbProvider.SelectedItem, InferenceProviderType)

        ' Models must be refreshed for the selected provider.
        ' We will connect this to the provider's model API.

    End Sub

End Class
