
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

        AddHandler _agentHostClient.Thinking, AddressOf AgentHostClient_Thinking
        AddHandler _agentHostClient.Content, AddressOf AgentHostClient_Content
        AddHandler _agentHostClient.ToolStarted, AddressOf AgentHostClient_ToolStarted
        AddHandler _agentHostClient.ToolCompleted, AddressOf AgentHostClient_ToolCompleted
        AddHandler _agentHostClient.ToolFailed, AddressOf AgentHostClient_ToolFailed
    End Sub

    Private Sub AgentHostClient_ToolFailed(toolName As String, errorMessage As String)
        Dim unused = AppendTextToOutputAsync($"{Environment.NewLine}Tool '{toolName}' failed with error: {errorMessage}")
    End Sub

    Private Sub AgentHostClient_ToolCompleted(toolName As String)
        Dim unused = AppendTextToOutputAsync($"{Environment.NewLine}Tool '{toolName}' completed successfully.")
    End Sub

    Private Sub AgentHostClient_ToolStarted(toolName As String, actionDescription As String)
        Dim unused = AppendTextToOutputAsync($"{Environment.NewLine}Tool '{toolName}' started: {actionDescription}")
    End Sub

    Private Sub AgentHostClient_Content(text As String)
        Dim unused = AppendTextToOutputAsync($"{Environment.NewLine}{text}")
    End Sub

    Private Sub AgentHostClient_Thinking(text As String)
        Dim unused = AppendTextToOutputAsync($"{Environment.NewLine}Thinking: {text}")
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
            Await AppendTextToOutputAsync("Error: " & errorMessage)
        Else
            Await AppendTextToOutputAsync(response.Content)
        End If

    End Function

    Private Async Function AppendTextToOutputAsync(text As String) As Task
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()
        txtOutput.Text &= text
    End Function
End Class