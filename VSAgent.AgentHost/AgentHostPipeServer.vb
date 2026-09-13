Imports System.Threading
Imports VSAgent.Protocol.Events
Imports VSAgent.Protocol.Messages

Public Class AgentHostPipeServer
    Private ReadOnly _runner As AgentRunner
    Private ReadOnly _transport As Transport.TransportPipeServer(Of AgentHostRequest, AgentHostResponse)
    Private _currentRunCancellation As CancellationTokenSource

    Public Sub New(PipeName As String, runner As AgentRunner)
        _runner = runner

        _transport = New Transport.TransportPipeServer(Of AgentHostRequest, AgentHostResponse)(PipeName, AddressOf HandleRequestAsync, "AgentHostPipeServer")
        _transport.Start()

        AddHandler _runner.Thinking, AddressOf Runner_Thinking
        AddHandler _runner.Content, AddressOf Runner_Content
        AddHandler _runner.ToolStarted, AddressOf Runner_ToolStarted
        AddHandler _runner.ToolCompleted, AddressOf Runner_ToolCompleted
        AddHandler _runner.ToolFailed, AddressOf Runner_ToolFailed
        AddHandler _runner.Statistics, AddressOf Runner_Statistics
        AddHandler _runner.TaskCancelled, AddressOf Runner_TaskCancelled

    End Sub

    Private Sub Runner_TaskCancelled()
        Dim unused = _transport.SendEventAsync(
                New AgentHostEvent With {
                    .Type = "taskCancelled"
                })
    End Sub

    Public Async Function StopAsync() As Task
        If _transport Is Nothing Then Return
        Await _transport.StopAsync
    End Function

    Private Async Function HandleRequestAsync(request As AgentHostRequest) As Task(Of AgentHostResponse)
        Select Case request.Type
            Case "prompt"
                Try
                    _currentRunCancellation?.Dispose()
                    _currentRunCancellation = New CancellationTokenSource()

                    Dim result = Await _runner.RunAsync(request.Content, _currentRunCancellation.Token)

                    Return New AgentHostResponse With {
                        .RequestId = request.Id,
                        .Success = True,
                        .Content = result
                    }
                Catch ex As OperationCanceledException
                    Return New AgentHostResponse With {
                        .Success = True,
                        .Content = Nothing
                    }
                Finally
                    _currentRunCancellation.Dispose()
                    _currentRunCancellation = Nothing
                End Try
            Case "interrupt"
                'Await _runner.InterruptAsync()
                If _currentRunCancellation IsNot Nothing Then
                    Await _currentRunCancellation.CancelAsync()
                    Return New AgentHostResponse With {
                        .RequestId = request.Id,
                        .Success = True
                    }
                Else
                    Return New AgentHostResponse With {
                        .RequestId = request.Id,
                        .Success = False,
                        .ErrorMessage = "No active run to interrupt."
                    }
                End If
            Case Else
                Return New AgentHostResponse With {
                    .RequestId = request.Id,
                    .Success = False,
                    .ErrorMessage = $"Unknown request type: {request.Type}"
                }
        End Select
    End Function

    Private Sub Runner_Thinking(text As String)

        Dim unused = _transport.SendEventAsync(
                New AgentHostEvent With {
                    .Type = "thinking",
                    .Text = text
                })
    End Sub
    Private Sub Runner_Content(text As String)

        Dim unused = _transport.SendEventAsync(
                New AgentHostEvent With {
                    .Type = "content",
                    .Text = text
                })
    End Sub
    Private Sub Runner_ToolStarted(toolName As String, actionDescription As String)

        Dim unused = _transport.SendEventAsync(
                New AgentHostEvent With {
                    .Type = "toolStarted",
                    .ToolName = toolName,
                    .ActionDescription = actionDescription
                })
    End Sub

    Private Sub Runner_ToolCompleted(toolName As String)

        Dim unused = _transport.SendEventAsync(
                New AgentHostEvent With {
                    .Type = "toolCompleted",
                    .ToolName = toolName
                })
    End Sub

    Private Sub Runner_ToolFailed(toolName As String, actionDescription As String)

        Dim unused = _transport.SendEventAsync(
                New AgentHostEvent With {
                    .Type = "toolFailed",
                    .ToolName = toolName,
                    .ActionDescription = actionDescription
                })
    End Sub

    Private Sub Runner_Statistics(statistics As String)
        Dim unused = _transport.SendEventAsync(
                New AgentHostEvent With {
                    .Type = "statistics",
                    .Text = statistics
                })
    End Sub
End Class
