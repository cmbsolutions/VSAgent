
Imports Microsoft.VisualStudio.Shell
Imports VSAgent.Protocol.Messages

'''<summary>
''' Interaction logic for VSAgentToolWindowControl.xaml
'''</summary>
Partial Public Class VSAgentToolWindowControl
    Inherits System.Windows.Controls.UserControl

    Private ReadOnly _agentHostClient As AgentHostClient

    Public Sub New(agentHostClient As AgentHostClient)

        InitializeComponent()

        _agentHostClient = agentHostClient
    End Sub

    Private Sub btnSend_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btnSend.Click
        Dim unused = ThreadHelper.JoinableTaskFactory.RunAsync(AddressOf SendPromptAsync)
    End Sub

    Private Async Function SendPromptAsync() As Task

        Dim prompt = txtPrompt.Text.Trim()

        If String.IsNullOrWhiteSpace(prompt) Then
            Return
        End If

        Dim response As AgentHostResponse = Nothing
        Dim errorMessage As String = Nothing

        btnSend.IsEnabled = False

        Try
            response = Await _agentHostClient.SendPromptAsync(prompt)

        Catch ex As Exception
            errorMessage = ex.Message
        End Try

        ' Now we're outside Catch/Finally, so Await is allowed.
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()

        btnSend.IsEnabled = True

        If errorMessage IsNot Nothing Then
            txtOutput.Text = "Error: " & errorMessage
        Else
            txtOutput.Text = response.Content
        End If

    End Function
End Class