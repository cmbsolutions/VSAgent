Imports System.IO
Imports Newtonsoft.Json
Imports VSAgent.Protocol.Settings

Namespace Settings
    Public NotInheritable Class SettingsManager

        Private Sub New()
        End Sub

        Private Shared ReadOnly SettingsPath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VSAgent", "settings.json")

        Public Shared Function Load() As VSAgentSettings

            If Not File.Exists(SettingsPath) Then
                Return New VSAgentSettings()
            End If

            Try
                Return JsonConvert.DeserializeObject(Of VSAgentSettings)(File.ReadAllText(SettingsPath))
            Catch ex As Exception
                Debug.WriteLine($"Failed to load VSAgent settings: {ex.Message}")

                Return New VSAgentSettings()
            End Try
        End Function

        Public Shared Sub Save(settings As VSAgentSettings)
            Dim directoryPath = Path.GetDirectoryName(SettingsPath)
            Directory.CreateDirectory(directoryPath)
            File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented))
        End Sub

    End Class
End Namespace
