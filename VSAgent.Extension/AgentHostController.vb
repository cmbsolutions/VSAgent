Imports System.IO

Public Class AgentHostController
    Implements IDisposable

    Private _process As Process

    Public ReadOnly Property IsRunning As Boolean
        Get
            Return _process IsNot Nothing AndAlso Not _process.HasExited
        End Get
    End Property

    Public Sub EnsureStarted()

        If IsRunning Then
            Return
        End If

        Dim extensionDirectory = Path.GetDirectoryName(GetType(AgentHostController).Assembly.Location)

        Dim agentHostPath = Path.Combine(extensionDirectory, "AgentHost", "VSAgent.AgentHost.exe")

        If Not File.Exists(agentHostPath) Then
            Throw New FileNotFoundException("VSAgent AgentHost could not be found.", agentHostPath)
        End If

        Dim startInfo As New ProcessStartInfo With {
            .FileName = agentHostPath,
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .WorkingDirectory = Path.GetDirectoryName(agentHostPath)
        }

        _process = Process.Start(startInfo)
    End Sub

    Public Sub StopHost()

        If Not IsRunning Then
            Return
        End If

        Try
            _process.Kill()
            _process.WaitForExit(2000)
        Catch
        End Try

        _process.Dispose()
        _process = Nothing
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        StopHost()
    End Sub
End Class