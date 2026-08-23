Imports System
Imports System.IO.Pipelines
Imports System.Net.Http
Imports System.Text
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Module Program
    ' These could come from a config.json file
    Private Const model = "qwen3.6:35b"
    Private Const base_url = "http://localhost:11434/"
    Private Const APIKey = "ollama"

    Sub Main(args As String())
        MainAsync().GetAwaiter().GetResult()
    End Sub

    Private Async Function MainAsync() As Task
        Dim isThinking As Boolean = False
        Dim isContent As Boolean = False

        Using vsAgent As New VSAgentPipeClient()

            Console.WriteLine("Connecting to Visual Studio...")

            Await vsAgent.ConnectAsync()

            Dim descriptors = Await vsAgent.GetAvailableToolsAsync()

            Console.WriteLine($"VSAgent AgentHost connected.")

            Console.WriteLine($"{descriptors.Count} Visual Studio tools available.")

            Dim ollama As New OllamaClient(base_url, model)

            AddHandler ollama.ThinkingReceived,
                Sub(text)
                    If Not isThinking Then
                        Console.WriteLine()
                        Console.ForegroundColor = ConsoleColor.DarkGray
                        Console.WriteLine("Thinking > ")
                        Console.ForegroundColor = ConsoleColor.White
                        isThinking = True
                    End If

                    isContent = False
                    Console.ForegroundColor = ConsoleColor.DarkGray
                    Console.Write(text)
                    Console.ForegroundColor = ConsoleColor.White
                End Sub

            AddHandler ollama.ContentReceived,
                Sub(text)
                    If Not isContent Then
                        Console.WriteLine()
                        Console.ForegroundColor = ConsoleColor.Cyan
                        Console.WriteLine("Assistant > ")
                        Console.ForegroundColor = ConsoleColor.White
                        isContent = True
                    End If

                    isThinking = False
                    Console.ForegroundColor = ConsoleColor.Cyan
                    Console.Write(text)
                    Console.ForegroundColor = ConsoleColor.White
                End Sub

            Dim agent As New AgentRunner(vsAgent, ollama, descriptors)

            Console.WriteLine()
            Console.ForegroundColor = ConsoleColor.DarkGray
            Console.WriteLine("Type /exit or /quit to stop.")
            Console.ForegroundColor = ConsoleColor.White

            Do
                Console.Write("You > ")

                Dim prompt = Console.ReadLine()

                If String.IsNullOrWhiteSpace(prompt) Then
                    Continue Do
                End If

                If prompt.Equals("/exit", StringComparison.OrdinalIgnoreCase) OrElse
                    prompt.Equals("/quit", StringComparison.OrdinalIgnoreCase) Then
                    Exit Do
                End If

                Try
                    Await agent.RunAsync(prompt)

                Catch ex As Exception
                    Console.WriteLine()
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine($"Agent error: {ex}")
                    Console.ForegroundColor = ConsoleColor.White
                End Try

                Console.WriteLine()
            Loop
        End Using
    End Function
End Module
