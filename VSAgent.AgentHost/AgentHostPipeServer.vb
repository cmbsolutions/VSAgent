Imports System.Threading
Imports Newtonsoft.Json
Imports VSAgent.Protocol.DTO
Imports VSAgent.Protocol.Events
Imports VSAgent.Protocol.Messages
Imports VSAgent.Protocol.Settings

Public Class AgentHostPipeServer
    Private _runner As AgentRunner
    Private _InferenceProvider As IInferenceProvider
    Private ReadOnly _vsAgent As VSAgentPipeClient
    Private ReadOnly _descriptors As IReadOnlyList(Of ToolDescriptor)
    Private ReadOnly _transport As Transport.TransportPipeServer(Of AgentHostRequest, AgentHostResponse)
    Private _currentRunCancellation As CancellationTokenSource

    Public ReadOnly Property Completion As Task
        Get
            Return _transport.Completion
        End Get
    End Property

    Public Sub New(PipeName As String, vsAgent As VSAgentPipeClient, descriptors As IReadOnlyList(Of ToolDescriptor))
        _vsAgent = vsAgent
        _descriptors = descriptors
        _transport = New Transport.TransportPipeServer(Of AgentHostRequest, AgentHostResponse)(PipeName, AddressOf HandleRequestAsync, "AgentHostPipeServer")
        _transport.Start()
    End Sub

    Private Sub AttachRunnerEvents(runner As AgentRunner)
        AddHandler runner.Thinking, AddressOf Runner_Thinking
        AddHandler runner.Content, AddressOf Runner_Content
        AddHandler runner.ToolStarted, AddressOf Runner_ToolStarted
        AddHandler runner.ToolCompleted, AddressOf Runner_ToolCompleted
        AddHandler runner.ToolFailed, AddressOf Runner_ToolFailed
        AddHandler runner.Statistics, AddressOf Runner_Statistics
        AddHandler runner.TaskCancelled, AddressOf Runner_TaskCancelled
    End Sub

    Private Sub DetachRunnerEvents(runner As AgentRunner)
        RemoveHandler runner.Thinking, AddressOf Runner_Thinking
        RemoveHandler runner.Content, AddressOf Runner_Content
        RemoveHandler runner.ToolStarted, AddressOf Runner_ToolStarted
        RemoveHandler runner.ToolCompleted, AddressOf Runner_ToolCompleted
        RemoveHandler runner.ToolFailed, AddressOf Runner_ToolFailed
        RemoveHandler runner.Statistics, AddressOf Runner_Statistics
        RemoveHandler runner.TaskCancelled, AddressOf Runner_TaskCancelled
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
            Case "getmodels"
                Try
                    _currentRunCancellation?.Dispose()
                    _currentRunCancellation = New CancellationTokenSource()

                    Dim result = Await _InferenceProvider.GetModelsAsync(_currentRunCancellation.Token)

                    Return New AgentHostResponse With {
                        .RequestId = request.Id,
                        .Success = True,
                        .Content = JsonConvert.SerializeObject(result)
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
            Case "setsettings"
                Try
                    Dim settings = JsonConvert.DeserializeObject(Of VSAgentSettings)(request.Content)

                    ConfigureAgent(settings)

                    Return New AgentHostResponse With {
                        .RequestId = request.Id,
                        .Success = True
                    }
                Catch ex As Exception
                    Return New AgentHostResponse With {
                        .Success = False,
                        .Content = "Settings could not be set."
                    }
                End Try
            Case Else
                Return New AgentHostResponse With {
                    .RequestId = request.Id,
                    .Success = False,
                    .ErrorMessage = $"Unknown request type: {request.Type}"
                }
        End Select
    End Function

    Private Sub ConfigureAgent(settings As VSAgentSettings)

        If _currentRunCancellation IsNot Nothing Then
            Throw New InvalidOperationException(
            "Cannot change inference settings while the agent is running.")
        End If

        Dim newProvider As IInferenceProvider

        Select Case settings.InferenceProvider

            Case InferenceProviderType.Ollama
                newProvider = New OllamaClient(settings)

            Case InferenceProviderType.Strata
                newProvider = New StrataClient(settings)

            Case Else
                Throw New NotSupportedException(
                $"Unsupported inference provider: {settings.InferenceProvider}")

        End Select

        Dim newRunner As New AgentRunner(_vsAgent, newProvider, _descriptors)

        If _runner IsNot Nothing Then
            DetachRunnerEvents(_runner)
        End If

        _InferenceProvider = newProvider
        _runner = newRunner

        AttachRunnerEvents(_runner)

    End Sub

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
