Imports VSAgent.Protocol.Events
Imports VSAgent.Protocol.Messages

Public Class AgentHostPipeServer
    Private ReadOnly _runner As AgentRunner
    Private ReadOnly _transport As Transport.TransportPipeServer(Of AgentHostRequest, AgentHostResponse)

    Public Sub New(PipeName As String, runner As AgentRunner)
        _runner = runner

        _transport = New Transport.TransportPipeServer(Of AgentHostRequest, AgentHostResponse)(PipeName, AddressOf HandleRequestAsync)
        _transport.Start()

        AddHandler _runner.Thinking, AddressOf Runner_Thinking
        AddHandler _runner.Content, AddressOf Runner_Content
        AddHandler _runner.ToolStarted, AddressOf Runner_ToolStarted
        AddHandler _runner.ToolCompleted, AddressOf Runner_ToolCompleted
        AddHandler _runner.ToolFailed, AddressOf Runner_ToolFailed
    End Sub

    Public Async Function StopAsync() As Task
        If _transport Is Nothing Then
            Return
        End If

        Await _transport.StopAsync
    End Function

    Private Async Function HandleRequestAsync(request As AgentHostRequest) As Task(Of AgentHostResponse)

        Select Case request.Type
            Case "prompt"
                Dim result = Await _runner.RunAsync(request.Content)

                Return New AgentHostResponse With {
                    .RequestId = request.Id,
                    .Success = True,
                    .Content = result
                }

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
End Class
