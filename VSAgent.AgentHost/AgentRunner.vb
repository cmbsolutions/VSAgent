Imports System.Threading
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports VSAgent.Ollama
Imports VSAgent.Protocol.DTO

Public Class AgentRunner

    Private ReadOnly _vsAgent As VSAgentPipeClient
    Private ReadOnly _ollama As OllamaClient

    Private ReadOnly _tools As JArray
    Private ReadOnly _messages As JArray

    Private ReadOnly _toolActionDescriptions As Dictionary(Of String, String)
    Private _cancellationTokenSource As CancellationTokenSource

    Public Event Thinking(text As String)
    Public Event Content(text As String)

    Public Event ToolStarted(toolName As String, actionDescription As String)
    Public Event ToolCompleted(toolName As String)
    Public Event ToolFailed(toolName As String, errorMessage As String)

    Public Event Statistics(statistics As String)

    Public Event TaskCancelled()

    Public Sub New(vsAgent As VSAgentPipeClient, ollama As OllamaClient, toolDescriptors As IReadOnlyList(Of ToolDescriptor))

        _vsAgent = vsAgent
        _ollama = ollama
        _cancellationTokenSource = New CancellationTokenSource

        AddHandler _ollama.ThinkingReceived, Sub(text) RaiseEvent Thinking(text)
        AddHandler _ollama.ContentReceived, Sub(text) RaiseEvent Content(text)
        AddHandler _ollama.StatisticsReceived, Sub(stats) RaiseEvent Statistics(stats)

        ' Fallback action descriptions, used when Model does not provide a description of what it is doing
        _toolActionDescriptions = toolDescriptors.ToDictionary(
            Function(t) t.Name,
            Function(t) t.ActionDescription,
            StringComparer.OrdinalIgnoreCase)

        _tools = BuildOpenAITools(toolDescriptors)

        _messages = New JArray From {
            New JObject From {
                {"role", "system"},
                {
                    "content",
                    My.Resources.system_prompt
                }
            }
        }

    End Sub

    Public Async Function RunAsync(userPrompt As String) As Task(Of String)

        _messages.Add(
            New JObject From {
                {"role", "user"},
                {"content", userPrompt}
            })

        Do
            If _cancellationTokenSource.IsCancellationRequested Then
                RaiseEvent TaskCancelled()
                Return Nothing
            End If

            Dim response = Await _ollama.SendAsync(_messages, _tools, _cancellationTokenSource.Token)
            Dim content = response.Content

            Dim assistantMessage As New JObject From {
                {"role", "assistant"},
                {"content", content}
            }

            If response.ToolCalls.Count = 0 Then
                _messages.Add(assistantMessage)
                Return content
            Else
                Dim calls As New JArray()

                For Each toolCall In response.ToolCalls

                    calls.Add(
                        New JObject From {
                            {"id", toolCall.Id},
                            {
                                "function",
                                New JObject From {
                                    {"name", toolCall.Name},
                                    {"arguments", toolCall.Arguments}
                                }
                            }
                        })

                Next

                assistantMessage("tool_calls") = calls

                _messages.Add(assistantMessage)
            End If

            For Each toolCall In response.ToolCalls
                Await ExecuteToolCallAsync(toolCall)
            Next
        Loop
    End Function

    Private Async Function ExecuteToolCallAsync(toolCall As OllamaToolCall) As Task

        Dim toolResult As String
        Dim description As String = Nothing

        Try
            If _toolActionDescriptions.TryGetValue(toolCall.Name, description) Then
                RaiseEvent ToolStarted(toolCall.Name, description)
            Else
                RaiseEvent ToolStarted(toolCall.Name, "")
            End If

            Dim response = Await _vsAgent.CallToolAsync(toolCall.Name, toolCall.Arguments)

            If response.Success Then

                Dim resultToken = If(response.Result Is Nothing, JValue.CreateNull(), JToken.FromObject(response.Result))

                toolResult = resultToken.ToString(Formatting.None)

                RaiseEvent ToolCompleted(toolCall.Name)
            Else

                toolResult =
                    New JObject From {
                        {"success", False},
                        {"error", response.ErrorMessage}
                    }.ToString(Formatting.None)

                RaiseEvent ToolFailed(toolCall.Name, response.ErrorMessage)
            End If

        Catch ex As Exception

            toolResult =
                New JObject From {
                    {"success", False},
                    {"error", ex.Message}
                }.ToString(Formatting.None)

            RaiseEvent ToolFailed(toolCall.Name, ex.Message)
        End Try

        ' Feed the result back to Qwen.
        _messages.Add(
            New JObject From {
                {"role", "tool"},
                {"tool_call_id", toolCall.Id},
                {"tool_name", toolCall.Name},
                {"content", toolResult}
            })

    End Function

    Private Shared Function BuildOpenAITools(descriptors As IReadOnlyList(Of ToolDescriptor)) As JArray

        Dim tools As New JArray()

        For Each descriptor In descriptors

            If String.Equals(descriptor.Name, "getAvailableTools", StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If

            tools.Add(
                New JObject From {
                    {"type", "function"},
                    {
                        "function",
                        New JObject From {
                            {"name", descriptor.Name},
                            {"description", descriptor.Description},
                            {
                                "parameters",
                                JObject.FromObject(
                                    descriptor.Parameters)
                            }
                        }
                    }
                })

        Next

        Return tools

    End Function

    Public Async Function InterruptAsync() As Task
        If _cancellationTokenSource IsNot Nothing Then
            _cancellationTokenSource.Cancel()
            Await Task.Delay(100)
        End If
    End Function
End Class