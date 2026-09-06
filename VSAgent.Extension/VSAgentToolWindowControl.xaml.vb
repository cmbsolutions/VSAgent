
Imports Microsoft.VisualStudio.Shell
Imports VSAgent.Protocol.Messages

'''<summary>
''' Interaction logic for VSAgentToolWindowControl.xaml
'''</summary>
Partial Public Class VSAgentToolWindowControl
    Inherits System.Windows.Controls.UserControl

    Private ReadOnly _agentHostClient As AgentHostClient

    Private isThinking As Boolean = False
    Private isContent As Boolean = False
    Private isTool As Boolean = False

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
        Dim unused = AppendTextToOutputAsync($"Failed with error: {errorMessage}")
    End Sub

    Private Sub AgentHostClient_ToolCompleted(toolName As String)
        Dim unused = AppendTextToOutputAsync($"Completed successfully.")
    End Sub

    Private Sub AgentHostClient_ToolStarted(toolName As String, actionDescription As String)
        If Not isTool Then
            Dim unused1 = AppendTextToOutputAsync($"{Environment.NewLine}Tool > ")
            isTool = True
        End If

        isThinking = False
        isContent = False
        Dim unused = AppendTextToOutputAsync($"'{toolName}' started: {actionDescription}... ")
    End Sub

    Private Sub AgentHostClient_Content(text As String)
        If Not isContent Then
            Dim unused1 = AppendTextToOutputAsync($"{Environment.NewLine}Assistant > ")
            isContent = True
        End If

        isThinking = False
        isTool = False
        Dim unused = AppendTextToOutputAsync(text)
    End Sub

    Private Sub AgentHostClient_Thinking(text As String)
        If Not isThinking Then
            Dim unused1 = AppendTextToOutputAsync($"{Environment.NewLine}Thinking > ")
            isThinking = True
        End If

        isContent = False
        isTool = False
        Dim unused = AppendTextToOutputAsync(text)
    End Sub

    Public Async Function SetConnectedAsync() As Task
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()
        btnSend.IsEnabled = True
    End Function

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

    Private Sub btnStop_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btnStop.Click
        Dim unused = ThreadHelper.JoinableTaskFactory.RunAsync(AddressOf SendInterruptAsync)
    End Sub

    Private Async Function SendInterruptAsync() As Task
        Dim response As AgentHostResponse = Nothing
        Dim errorMessage As String = Nothing

        Await AppendTextToOutputAsync($"{Environment.NewLine}Sending interrupt...")
        Try
            response = Await _agentHostClient.SendInterruptAsync()

        Catch ex As Exception
            errorMessage = ex.Message
        End Try

        ' Now we're outside Catch/Finally, so Await is allowed.
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()

        If errorMessage IsNot Nothing Then
            Await AppendTextToOutputAsync("Error: " & errorMessage)
        Else
            Await AppendTextToOutputAsync(response.Content)
        End If
    End Function
End Class