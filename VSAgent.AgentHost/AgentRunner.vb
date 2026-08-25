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

    Public Event ToolStarted(toolName As String, actionDescription As String)
    Public Event ToolCompleted(toolName As String)
    Public Event ToolFailed(toolName As String, errorMessage As String)

    Public Sub New(vsAgent As VSAgentPipeClient, ollama As OllamaClient, toolDescriptors As IReadOnlyList(Of ToolDescriptor))

        _vsAgent = vsAgent
        _ollama = ollama

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
                    "
You are an AI software development assistant connected to a running Visual Studio instance.

Use the supplied Visual Studio tools whenever information or actions are required.

Do not guess about source code that you have not inspected.

You are allowed to modify source code and create documents using the available tools.

When asked to fix or refactor code:
1. Inspect the relevant solution, project and source code.
2. Use diagnostics, symbol search and reference search when useful.
3. Apply edits using the provided tools.
4. Build the affected project or solution.
5. If the build fails, inspect the errors and continue fixing them.
6. Continue until the requested task is complete or a tool returns an error that prevents further progress.

Do not ask the user to make code changes manually when a suitable tool exists.
"
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
        Dim cts As New CancellationTokenSource()

        Do
            Dim response = Await _ollama.SendAsync(_messages, _tools, cts.Token)
            Dim content = response.Content

            Dim assistantMessage As New JObject From {
                {"role", "assistant"},
                {"content", content}
            }

            'If Not String.IsNullOrWhiteSpace(content) Then
            '    Console.WriteLine()
            '    Console.ForegroundColor = ConsoleColor.Cyan
            '    Console.WriteLine($"Qwen > {content}")
            '    Console.ForegroundColor = ConsoleColor.White
            'End If

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
End Class