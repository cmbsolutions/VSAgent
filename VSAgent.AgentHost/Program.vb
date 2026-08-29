Module Program
    ' These could come from a config.json file
    Private Const model = "qwen3.6:35b"
    Private Const base_url = "http://localhost:11434/"
    Private Const APIKey = "ollama"

    Private isThinking As Boolean = False
    Private isContent As Boolean = False
    Private isTool As Boolean = False

    Sub Main(args As String())
        MainAsync().GetAwaiter().GetResult()
    End Sub

    Private Async Function MainAsync() As Task


        Using vsAgent As New VSAgentPipeClient()

            Console.WriteLine("Connecting to Visual Studio...")

            Await vsAgent.ConnectAsync()

            Dim descriptors = Await vsAgent.GetAvailableToolsAsync()

            Console.WriteLine($"VSAgent AgentHost connected.")

            Console.WriteLine($"{descriptors.Count} Visual Studio tools available.")

            Dim ollama As New OllamaClient(base_url, model)

            AddHandler ollama.ThinkingReceived, AddressOf OllamaThinkingReceivedEventHandler
            AddHandler ollama.ContentReceived, AddressOf OllamaContentReceivedEventHandler

            Dim agent As New AgentRunner(vsAgent, ollama, descriptors)

            AddHandler agent.ToolStarted, AddressOf AgentToolStartedEventHandler
            AddHandler agent.ToolCompleted, AddressOf AgentToolCompletedEventHandler
            AddHandler agent.ToolFailed, AddressOf AgentToolFailedEventHandler

            Dim hostServer As New AgentHostPipeServer("VSAGent.AgentHost", agent)

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

    Private Sub OllamaThinkingReceivedEventHandler(text As String)
        If Not isThinking Then
            Console.WriteLine()
            Console.ForegroundColor = ConsoleColor.DarkGray
            Console.Write("Thinking > ")
            Console.ForegroundColor = ConsoleColor.White
            isThinking = True
        End If

        isContent = False
        isTool = False
        Console.ForegroundColor = ConsoleColor.DarkGray
        Console.Write(text)
        Console.ForegroundColor = ConsoleColor.White
    End Sub

    Private Sub OllamaContentReceivedEventHandler(text As String)
        If Not isContent Then
            Console.WriteLine()
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write("Assistant > ")
            Console.ForegroundColor = ConsoleColor.White
            isContent = True
        End If

        isThinking = False
        isTool = False
        Console.ForegroundColor = ConsoleColor.Cyan
        Console.Write(text)
        Console.ForegroundColor = ConsoleColor.White
    End Sub

    Private Sub AgentToolStartedEventHandler(toolName As String, toolAction As String)
        If Not isTool Then
            Console.WriteLine()
            Console.ForegroundColor = ConsoleColor.Yellow
            Console.Write($"Tool > {toolName} started... ")
            Console.ForegroundColor = ConsoleColor.White
            isTool = True
        End If

        isThinking = False
        isContent = False
    End Sub

    Private Sub AgentToolCompletedEventHandler(toolName As String)
        Console.ForegroundColor = ConsoleColor.Yellow
        Console.Write(" and completed!")
        Console.ForegroundColor = ConsoleColor.White
        isTool = False
    End Sub

    Private Sub AgentToolFailedEventHandler(toolName As String, message As String)
        Console.ForegroundColor = ConsoleColor.Red
        Console.Write($" and failed! {message}")
        Console.ForegroundColor = ConsoleColor.White
        isTool = False
    End Sub
End Module
