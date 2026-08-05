Imports System.IO
Imports System.IO.Pipes
Imports System.Text
Imports System.Text.Json
Imports VSAgent.Protocol.Messages

Module Program

    Sub Main(args As String())
        MainAsync().GetAwaiter().GetResult()
    End Sub

    Public Async Function MainAsync() As Task

        Using pipe = New NamedPipeClientStream(
            ".",
            "VSAgent",
            PipeDirection.InOut,
            PipeOptions.Asynchronous)

            Console.WriteLine("Connecting to Visual Studio...")

            Await pipe.ConnectAsync(5000)

            Using reader = New StreamReader(pipe, New UTF8Encoding(False), detectEncodingFromByteOrderMarks:=False, bufferSize:=4096, leaveOpen:=True)
                Using writer = New StreamWriter(pipe, New UTF8Encoding(False), bufferSize:=4096, leaveOpen:=True)
                    writer.AutoFlush = True

                    Console.WriteLine("Get available tools...")

                    Dim request As New AgentRequest With {
                        .Id = Guid.NewGuid().ToString(),
                        .Tool = "getAvailableTools",
                        .Parameters = New Dictionary(Of String, Object) From {}
                    }

                    Dim json = JsonSerializer.Serialize(request)

                    Console.WriteLine($"Sending: {json}")

                    Await writer.WriteLineAsync(json)

                    Dim response = Await reader.ReadLineAsync()

                    Console.WriteLine($"Received: {response}")


                    Console.WriteLine("Testing ping tool, expecting pong in return...")

                    request = New AgentRequest With {
                        .Id = Guid.NewGuid().ToString(),
                        .Tool = "ping",
                        .Parameters = New Dictionary(Of String, Object) From {}
                    }

                    json = JsonSerializer.Serialize(request)

                    Console.WriteLine($"Sending: {json}")

                    Await writer.WriteLineAsync(json)

                    response = Await reader.ReadLineAsync()

                    Console.WriteLine($"Received: {response}")

                    Console.WriteLine("Testing getSolutionInfo tool, expecting solution info in return...")

                    request = New AgentRequest With {
                        .Id = Guid.NewGuid().ToString(),
                        .Tool = "getSolutionInfo",
                        .Parameters = New Dictionary(Of String, Object) From {}
                    }

                    json = JsonSerializer.Serialize(request)

                    Console.WriteLine($"Sending: {json}")

                    Await writer.WriteLineAsync(json)

                    response = Await reader.ReadLineAsync()

                    Console.WriteLine($"Received: {response}")

                    Console.WriteLine("Testing getProjects tool, expecting project info in return...")

                    request = New AgentRequest With {
                        .Id = Guid.NewGuid().ToString(),
                        .Tool = "getProjects",
                        .Parameters = New Dictionary(Of String, Object) From {}
                    }

                    json = JsonSerializer.Serialize(request)

                    Console.WriteLine($"Sending: {json}")

                    Await writer.WriteLineAsync(json)

                    response = Await reader.ReadLineAsync()

                    Console.WriteLine($"Received: {response}")

                    Console.WriteLine("Testing getRoslynProjects tool, expecting roslyn project info in return...")

                    request = New AgentRequest With {
                        .Id = Guid.NewGuid().ToString(),
                        .Tool = "getRoslynProjects",
                        .Parameters = New Dictionary(Of String, Object) From {}
                    }

                    json = JsonSerializer.Serialize(request)

                    Console.WriteLine($"Sending: {json}")

                    Await writer.WriteLineAsync(json)

                    response = Await reader.ReadLineAsync()

                    Console.WriteLine($"Received: {response}")
                End Using
            End Using
        End Using
    End Function
End Module