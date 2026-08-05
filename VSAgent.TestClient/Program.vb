Imports System.IO
Imports System.IO.Pipes
Imports System.Text
Imports System.Text.Json
Imports VSAgent.Protocol.DTO
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

                    Dim toolResponse = JsonSerializer.Deserialize(Of AgentResponse)(response)
                    Dim toolDescriptors As List(Of ToolDescriptor) = JsonSerializer.Deserialize(Of List(Of ToolDescriptor))(toolResponse.Result)

                    For Each toolDescriptor In toolDescriptors
                        Console.WriteLine($"Testing tool: {toolDescriptor.Name}, Version: {toolDescriptor.Version}...")

                        request = New AgentRequest With {
                            .Id = Guid.NewGuid().ToString(),
                            .Tool = toolDescriptor.Name,
                            .Parameters = New Dictionary(Of String, Object) From {}
                        }

                        json = JsonSerializer.Serialize(request)

                        Console.WriteLine($"Sending: {json}")

                        Await writer.WriteLineAsync(json)

                        response = Await reader.ReadLineAsync()

                        Console.WriteLine($"Received: {response}")
                        Console.WriteLine()
                    Next
                End Using
            End Using
        End Using
    End Function
End Module