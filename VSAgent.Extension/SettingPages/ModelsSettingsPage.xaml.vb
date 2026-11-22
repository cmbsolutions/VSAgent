Imports System.Windows
Imports System.Windows.Controls
Imports VSAgent.Protocol.Settings


Public Class ModelsSettingsPage

    Private _loading As Boolean = True
    Private _availableModels As IEnumerable(Of String)


    Public Sub New(Optional availableModels As IEnumerable(Of String) = Nothing)
        InitializeComponent()

        cmbProvider.ItemsSource = [Enum].GetValues(GetType(InferenceProviderType))

        If availableModels IsNot Nothing Then
            _availableModels = availableModels
            cmbModel.ItemsSource = _availableModels
        End If

        AddHandler DataContextChanged, AddressOf ModelsSettingsPage_DataContextChanged
    End Sub

    Private Sub ModelsSettingsPage_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)

        Dim config = TryCast(DataContext, VSAgentSettings)

        If config Is Nothing Then Return

        _loading = True

        cmbProvider.SelectedItem = config.InferenceProvider

        cmbModel.ItemsSource = _availableModels

        _loading = False

    End Sub

    Private Sub cmbProvider_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)

        If _loading Then Return

        Dim config = TryCast(DataContext, VSAgentSettings)

        If config Is Nothing OrElse cmbProvider.SelectedItem Is Nothing Then Return

        config.InferenceProvider = DirectCast(cmbProvider.SelectedItem, InferenceProviderType)

        ' Models must be refreshed for the selected provider.
        ' We will connect this to the provider's model API.

    End Sub

End Class
