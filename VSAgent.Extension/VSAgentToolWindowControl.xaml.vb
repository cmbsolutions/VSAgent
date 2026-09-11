
Imports System.Windows
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
        AddHandler _agentHostClient.Statistics, AddressOf AgentHostClient_Statistics
        AddHandler _agentHostClient.TaskCancelled, AddressOf AgentHostClient_TaskCancelled

    End Sub

    Private Sub AgentHostClient_TaskCancelled()
        isTool = False
        isThinking = False
        isContent = False

        Dim unused = AppendTextToOutputAsync($"{Environment.NewLine}Task is cancelled by user.{Environment.NewLine}", Media.Colors.Orange)
    End Sub

    Private Sub AgentHostClient_Statistics(statistics As String)
        Dim unused = UpdateStatisticsAsync(statistics)
    End Sub

    Private Sub AgentHostClient_ToolFailed(toolName As String, errorMessage As String)
        Dim unused = AppendTextToOutputAsync($"Failed with error: {errorMessage}", Media.Colors.Red)
        isTool = False
    End Sub

    Private Sub AgentHostClient_ToolCompleted(toolName As String)
        Dim unused = AppendTextToOutputAsync($"Completed successfully.", Media.Colors.Green)
        isTool = False
    End Sub

    Private Sub AgentHostClient_ToolStarted(toolName As String, actionDescription As String)
        If Not isTool Then
            Dim unused1 = AppendTextToOutputAsync($"{Environment.NewLine}Tool > ", Media.Colors.OrangeRed)
            isTool = True
        End If

        isThinking = False
        isContent = False
        Dim unused = AppendTextToOutputAsync($"'{toolName}' started: {actionDescription}... ", Media.Colors.OrangeRed)
    End Sub

    Private Sub AgentHostClient_Content(text As String)
        If Not isContent Then
            Dim unused1 = AppendTextToOutputAsync($"{Environment.NewLine}Assistant > ", Media.Colors.DodgerBlue)
            isContent = True
        End If

        isThinking = False
        isTool = False
        Dim unused = AppendTextToOutputAsync(text, Media.Colors.DodgerBlue)
    End Sub

    Private Sub AgentHostClient_Thinking(text As String)
        If Not chkThinking.IsChecked Then Exit Sub

        If Not isThinking Then
            Dim unused1 = AppendTextToOutputAsync($"{Environment.NewLine}Thinking > ", Media.Colors.Gray)
            isThinking = True
        End If

        isContent = False
        isTool = False
        Dim unused = AppendTextToOutputAsync(text, Media.Colors.Gray)
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

        isTool = False
        isThinking = False
        isContent = False

        Dim unused = AppendTextToOutputAsync($"{Environment.NewLine}User > {prompt}{Environment.NewLine}", Media.Colors.DarkViolet)

        Dim response As AgentHostResponse = Nothing
        Dim errorMessage As String = Nothing

        btnSend.IsEnabled = False
        txtPrompt.Clear()

        Try
            response = Await _agentHostClient.SendPromptAsync(prompt)

        Catch ex As Exception
            errorMessage = ex.Message
        End Try

        ' Now we're outside Catch/Finally, so Await is allowed.
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()

        btnSend.IsEnabled = True


        If errorMessage IsNot Nothing Then
            Await AppendTextToOutputAsync("Error: " & errorMessage, Media.Colors.IndianRed)
        Else
            Await AppendTextToOutputAsync(response.Content, Media.Colors.DodgerBlue)
        End If

    End Function

    Private Async Function AppendTextToOutputAsync(text As String, color As Media.Color) As Task

        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()

        Dim range As New Documents.TextRange(txtOutput.Document.ContentEnd, txtOutput.Document.ContentEnd) With {
            .Text = text
        }
        range.ApplyPropertyValue(Documents.TextElement.ForegroundProperty, New Media.SolidColorBrush(color))

        txtOutput.UpdateLayout()
        txtOutput.ScrollToEnd()

    End Function

    Private Async Function UpdateStatisticsAsync(text As String) As Task

        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()

        lblStats.Content = text
    End Function

    Private Sub btnStop_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btnStop.Click
        Dim unused = ThreadHelper.JoinableTaskFactory.RunAsync(AddressOf SendInterruptAsync)
    End Sub

    Private Async Function SendInterruptAsync() As Task
        Dim response As AgentHostResponse = Nothing
        Dim errorMessage As String = Nothing

        Await AppendTextToOutputAsync($"{Environment.NewLine}Sending interrupt...", Media.Colors.Purple)
        Try
            response = Await _agentHostClient.SendInterruptAsync()

        Catch ex As Exception
            errorMessage = ex.Message
        End Try

        ' Now we're outside Catch/Finally, so Await is allowed.
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()

        If errorMessage IsNot Nothing Then
            Await AppendTextToOutputAsync("Error: " & errorMessage, Media.Colors.IndianRed)
        Else
            Await AppendTextToOutputAsync(response.Content, Media.Colors.DodgerBlue)
        End If
    End Function
End Class