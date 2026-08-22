Imports System
Imports System.IO.Pipelines
Imports System.Net.Http
Imports System.Text
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Module Program
    ' These could come from a config.json file
    Private Const model = "qwen3.8"
    Private Const base_url = "http://localhost:11434/v1/"
    Private Const APIKey = "ollama"

    Sub Main(args As String())
        MainAsync().GetAwaiter().GetResult()
    End Sub

    Private Async Function MainAsync() As Task
        Using vsClient As New VSAgentPipeClient()

            Await vsClient.ConnectAsync()

            Dim descriptors = Await vsClient.GetAvailableToolsAsync()

            Dim tools = AgentHelpers.BuildOpenAITools(descriptors)

            Dim messages As New JArray From {
                New JObject From {
                    {"role", "system"},
                    {
                        "content",
                        "You are connected to Visual Studio through tools. Use the available tools when needed."
                    }
                },
                New JObject From {
                    {"role", "user"},
                    {
                        "content",
                        "Check whether the VSAgent server is available. Use the ping tool."
                    }
                }
            }

            Dim ollama As New OllamaClient(base_url, model)

            Dim response = Await ollama.SendAsync(messages, tools)

            Console.WriteLine(response.ToString(Formatting.Indented))

        End Using
    End Function

    Private Async Function MainToolloopAsync() As Task
        Using vsClient As New VSAgentPipeClient()
            Using ollama As New OllamaClient(base_url, model)
                Await vsClient.ConnectAsync()

                Dim descriptors = Await vsClient.GetAvailableToolsAsync()

                Dim tools = AgentHelpers.BuildOpenAITools(descriptors)

                ' Toon de tools in de console
                Console.WriteLine("Available tools >")
                Console.WriteLine(JsonConvert.SerializeObject(tools, Formatting.Indented))

                ' Maak de berichtenlijst aan
                Dim messages As New JArray From {
                    New JObject From {
                        {"role", "system"},
                        {
                            "content",
                            "You are connected to a running Visual Studio instance through tools." & vbCrLf &
                                       "You are allowed to use the provided write/build tools." & vbCrLf &
                                       "If a tool exists for an operation, use it instead of claiming that Visual Studio, threading, saving, or environment restrictions prevent the operation." & vbCrLf &
                                       "The VSAgent tools handle Visual Studio SDK, Roslyn, threading, and document updates internally." & vbCrLf & vbCrLf &
                                       "When asked to fix code:" & vbCrLf &
                                       "1. Inspect the relevant code." & vbCrLf &
                                       "2. Use addDocument or applyDocumentEdit to make the changes." & vbCrLf &
                                       "3. Try to build the solution." & vbCrLf &
                                       "4. Repeat these steps until the build is successfully completed." & vbCrLf &
                                       "5. When a tool returns the same error multiple times, stop and report the error back to the user!" & vbCrLf &
                                       "6. Very important, before each tool call, briefly state what you are trying to learn or accomplish. Keep this explanation concise. This must never be forgotten!"
                        }
                    }}

                ' Start de hoofd-loop
                While True
                    Console.ForegroundColor = ConsoleColor.DarkGray
                    Console.WriteLine("Type /exit or /quit to stop.")
                    Console.ForegroundColor = ConsoleColor.Yellow
                    Console.Write("You > ")
                    Console.ForegroundColor = ConsoleColor.White

                    Dim userInput As String = Console.ReadLine().Trim()

                    If userInput.Equals("/exit", StringComparison.CurrentCultureIgnoreCase) OrElse userInput.Equals("/quit", StringComparison.CurrentCultureIgnoreCase) Then
                        Exit While
                    End If

                    If String.IsNullOrEmpty(userInput) Then
                        Continue While
                    End If

                    ' Voeg gebruikersbericht toe
                    Dim userMessage As New JObject From {
                        {"role", "user"},
                        {"content", userInput}
                    }

                    messages.Add(userMessage)

                    ' Loop voor de AI-reacties en toolafhandeling
                    While True
                        Dim response = Await ollama.SendAsync(messages, tools)

                        Dim choice As JToken = response("choices")(0)
                        Dim messageNode As JToken = choice("message")

                        ' Voeg het bericht van de AI toe aan de geschiedenis
                        Dim aiMessageDict As Dictionary(Of String, Object) = messageNode.ToObject(Of Dictionary(Of String, Object))()
                        messages.Add(aiMessageDict)

                        ' Toon tekstuele content als die er is
                        Dim aiContent As String = If(messageNode("content") IsNot Nothing, messageNode("content").ToString(), "")
                        If Not String.IsNullOrEmpty(aiContent) Then
                            Console.WriteLine()
                            Console.WriteLine("Qwen > " & aiContent)
                        End If

                        ' Controleer op tool calls
                        Dim toolCalls As JArray = CType(messageNode("tool_calls"), JArray)
                        If toolCalls Is Nothing OrElse toolCalls.Count = 0 Then
                            Console.WriteLine()
                            Exit While
                        End If

                        ' Voer elke tool call uit
                        For Each toolCall As JToken In toolCalls
                            Dim toolName As String = toolCall("function")("name").ToString()
                            Dim argumentsRaw As String = If(toolCall("function")("arguments") IsNot Nothing, toolCall("function")("arguments").ToString(), "{}")

                            Dim arguments As Dictionary(Of String, Object) = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(argumentsRaw)

                            Console.WriteLine("Tool > " & toolName)
                            Console.WriteLine("Args > " & JsonConvert.SerializeObject(arguments, Formatting.None))

                            Dim toolResult As String = ""

                            Try
                                ' Roep de tool aan via de pipe client
                                Dim result As Object = Await vsClient.CallToolAsync(toolName, Nothing) 'arguments)
                                Console.WriteLine("Tool result received")
                                toolResult = JsonConvert.SerializeObject(result, Formatting.None)

                            Catch ex As Exception
                                Dim errorDict As New Dictionary(Of String, String)()
                                errorDict("error") = ex.Message
                                toolResult = JsonConvert.SerializeObject(errorDict)
                            End Try

                            ' Stuur het resultaat van de tool terug naar de chatgeschiedenis
                            Dim toolResponse As New Dictionary(Of String, Object)()
                            toolResponse("role") = "tool"
                            toolResponse("tool_call_id") = toolCall("id").ToString()
                            toolResponse("content") = toolResult
                            messages.Add(toolResponse)
                        Next
                    End While
                End While
            End Using
        End Using
    End Function
End Module
