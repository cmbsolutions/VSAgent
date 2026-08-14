Imports System.IO
Imports System.IO.Pipes
Imports System.Text
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports VSAgent.Protocol.DTO
Imports VSAgent.Protocol.Messages
Module Program
    ''' <summary>
    ''' DEPRECATED
    ''' This client is deprecated, use a local LLM and the python script
    ''' </summary>
    ''' <param name="args"></param>

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
                        .Parameters = New JObject()
                    }

                    Dim json = JsonConvert.SerializeObject(request)

                    Console.WriteLine($"Sending: {json}")

                    Await writer.WriteLineAsync(json)

                    Dim response = Await reader.ReadLineAsync()

                    Console.WriteLine($"Received: {response}")

                    Console.WriteLine("Get active document...")

                    request = New AgentRequest With {
                        .Id = Guid.NewGuid().ToString(),
                        .Tool = "getActiveDocument",
                        .Parameters = New JObject()
                    }

                    json = JsonConvert.SerializeObject(request)

                    Console.WriteLine($"Sending: {json}")

                    Await writer.WriteLineAsync(json)

                    response = Await reader.ReadLineAsync()

                    Console.WriteLine($"Received: {response}")

                    Dim toolResponse = JsonConvert.DeserializeObject(Of AgentResponse)(response)
                    Dim activeDocument As ActiveDocumentInfo = toolResponse.GetResult(Of ActiveDocumentInfo)

                    Console.WriteLine("Get symbol...")

                    Dim params As New JObject From {
                        {"SymbolName", activeDocument.SelectionText}
                    }

                    request = New AgentRequest With {
                        .Id = Guid.NewGuid().ToString(),
                        .Tool = "findSymbol",
                        .Parameters = params
                    }

                    json = JsonConvert.SerializeObject(request)

                    Console.WriteLine($"Sending: {json}")

                    Await writer.WriteLineAsync(json)

                    response = Await reader.ReadLineAsync()

                    Console.WriteLine($"Received: {response}")

                    toolResponse = JsonConvert.DeserializeObject(Of AgentResponse)(response)

                    Dim foundSymbols = toolResponse.GetResult(Of IReadOnlyList(Of RoslynSymbolInfo))()

                    params = New JObject From {
                        {"DocumentId", foundSymbols.First.DocumentId},
                        {"Line", foundSymbols.First.Line},
                        {"Column", foundSymbols.First.Column}
                    }

                    request = New AgentRequest With {
                        .Id = Guid.NewGuid().ToString(),
                        .Tool = "findReferences",
                        .Parameters = params
                    }

                    json = JsonConvert.SerializeObject(request)

                    Console.WriteLine($"Sending: {json}")

                    Await writer.WriteLineAsync(json)
                    response = Await reader.ReadLineAsync()

                    Console.WriteLine($"Received: {response}")

                    'Dim result As JArray = DirectCast(toolResponse.Result, JArray)

                    'Dim toolDescriptors As List(Of ToolDescriptor) = result.ToObject(Of List(Of ToolDescriptor))

                    'For Each toolDescriptor In toolDescriptors
                    '    Console.WriteLine($"Testing tool: {toolDescriptor.Name}, Version: {toolDescriptor.Version}...")

                    '    Dim params As New JObject

                    '    For Each param In toolDescriptor.Parameters.Properties
                    '        Select Case param.Key
                    '            Case "filePath"
                    '                params.Add(param.Key, "E:\My Documents\localRepos\sentiatools\sentiman.net\SentiMan.NET\mainGUI.vb")
                    '            Case "documentId"
                    '                params.Add(param.Key, "58d5461c-5596-4b23-8151-319cc22f1751")
                    '            Case "line"
                    '                params.Add(param.Key, 5)
                    '            Case "column"
                    '                params.Add(param.Key, 14)
                    '            Case "SymbolName"
                    '                params.Add(param.Key, "XAccount")
                    '        End Select
                    '    Next

                    '    request = New AgentRequest With {
                    '        .Id = Guid.NewGuid().ToString(),
                    '        .Tool = toolDescriptor.Name,
                    '        .Parameters = params
                    '    }

                    '    json = JsonConvert.SerializeObject(request)

                    '    Console.WriteLine($"Sending: {json}")

                    '    Await writer.WriteLineAsync(json)

                    '    response = Await reader.ReadLineAsync()

                    '    Console.WriteLine($"Received: {response}")
                    '    Console.WriteLine()
                    'Next
                End Using
            End Using
        End Using
    End Function
End Module