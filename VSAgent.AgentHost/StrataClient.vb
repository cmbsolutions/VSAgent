Imports System.IO
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Threading
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports VSAgent.Ollama
Imports VSAgent.Protocol.InferenceProvider
Imports VSAgent.Protocol.Settings

Public Class StrataClient
    Implements IInferenceProvider
    Implements IDisposable

    Private ReadOnly _http As HttpClient
    Private disposedValue As Boolean
    Private _context_length_retrieved As Boolean = False

    Private ReadOnly _thinkingBuffer As New StringBuilder()
    Private ReadOnly _contentBuffer As New StringBuilder()

    Private _lastThinkingFlush As DateTime = DateTime.UtcNow
    Private _lastContentFlush As DateTime = DateTime.UtcNow

    Public Event ThinkingReceived(text As String) Implements IInferenceProvider.ThinkingReceived
    Public Event ContentReceived(text As String) Implements IInferenceProvider.ContentReceived
    Public Event StatisticsReceived(statistics As String) Implements IInferenceProvider.StatisticsReceived

    Private _sessionStats As New OllamaSessionStatistics

    Private _settings As VSAgentSettings

    Public Sub New(settings As VSAgentSettings)
        _settings = settings

        _http = New HttpClient With {
            .BaseAddress = New Uri(_settings.InferenceProviderBaseURL),
            .Timeout = Timeout.InfiniteTimeSpan
        }

        '_http.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("apiKey", Environment.GetEnvironmentVariable("STRATA_API_KEY"))
    End Sub

    Public Async Function SendAsync(messages As JArray, tools As JArray, cancellationToken As CancellationToken) As Task(Of InferenceProviderAssistantResponse) Implements IInferenceProvider.SendAsync
        Dim body As New JObject From {
            {"model", _settings.Model},
            {"messages", messages},
            {"tools", tools},
            {"stream", True},
            {"stream_options", New JObject From {
                {"include_usage", True}
            }},
            {"max_tokens", If(_settings.NumPredict > 0, _settings.NumPredict, 8192)}
        }

        Using request As New HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            request.Content = New StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json")

            Using response = Await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)

                Dim errorText As String = Nothing

                If Not response.IsSuccessStatusCode Then
                    errorText = Await response.Content.ReadAsStringAsync(cancellationToken)

                    Throw New InvalidOperationException($"Strata returned {CInt(response.StatusCode)}: {errorText}")
                End If

                Using stream = Await response.Content.ReadAsStreamAsync(cancellationToken)
                    Using reader As New StreamReader(stream)
                        Return Await ReadStreamAsync(reader, cancellationToken)
                    End Using
                End Using
            End Using
        End Using
    End Function

    Private Async Function ReadStreamAsync(reader As StreamReader, cancellationToken As CancellationToken) As Task(Of InferenceProviderAssistantResponse) Implements IInferenceProvider.ReadStreamAsync
        Dim result As New InferenceProviderAssistantResponse()

        Dim thinkingBuilder As New StringBuilder()
        Dim contentBuilder As New StringBuilder()

        ' OpenAI tool calls may arrive across multiple chunks.
        Dim pendingToolCalls As New SortedDictionary(Of Integer, StrataPendingToolCall)()

        While True

            cancellationToken.ThrowIfCancellationRequested()

            Dim line As String = Await reader.ReadLineAsync(cancellationToken)

            If line Is Nothing Then Exit While

            If String.IsNullOrWhiteSpace(line) Then Continue While

            ' Ignore SSE comments, event names and other metadata.
            If Not line.StartsWith("data:", StringComparison.Ordinal) Then
                Continue While
            End If

            Dim payload As String = line.Substring(5).TrimStart()

            If payload = "[DONE]" Then Exit While

            If String.IsNullOrWhiteSpace(payload) Then Continue While

            Dim chunk As JObject = JObject.Parse(payload)

            ' Statistics can arrive without choices.
            Dim usage As JObject = TryCast(chunk("usage"), JObject)

            If usage IsNot Nothing Then
                result.PromptEvalCount = usage.Value(Of Integer?)("prompt_tokens").GetValueOrDefault(0)

                result.EvalCount = usage.Value(Of Integer?)("completion_tokens").GetValueOrDefault(0)
            End If

            Dim choice As JObject = TryCast(chunk("choices")?.FirstOrDefault(), JObject)

            If choice Is Nothing Then Continue While

            Dim delta As JObject = TryCast(choice("delta"), JObject)

            If delta IsNot Nothing Then

                ' ------------------------------------------
                ' Reasoning
                ' ------------------------------------------

                Dim thinking As String = delta.Value(Of String)("reasoning_content")

                If Not String.IsNullOrEmpty(thinking) Then

                    thinkingBuilder.Append(thinking)
                    _thinkingBuffer.Append(thinking)

                    If ShouldFlush(_thinkingBuffer, _lastThinkingFlush) Then

                        Dim text As String = _thinkingBuffer.ToString()

                        _thinkingBuffer.Clear()
                        _lastThinkingFlush = DateTime.UtcNow

                        RaiseEvent ThinkingReceived(text)

                    End If

                End If

                ' ------------------------------------------
                ' Assistant content
                ' ------------------------------------------

                Dim content As String = delta.Value(Of String)("content")

                If Not String.IsNullOrEmpty(content) Then

                    contentBuilder.Append(content)
                    _contentBuffer.Append(content)

                    If ShouldFlush(_contentBuffer, _lastContentFlush) Then

                        Dim text As String = _contentBuffer.ToString()

                        _contentBuffer.Clear()
                        _lastContentFlush = DateTime.UtcNow

                        RaiseEvent ContentReceived(text)

                    End If

                End If

                ' ------------------------------------------
                ' Tool calls
                ' ------------------------------------------

                Dim toolCalls As JArray = TryCast(delta("tool_calls"), JArray)

                If toolCalls IsNot Nothing Then

                    For Each toolCallToken As JToken In toolCalls

                        Dim toolCall As JObject =
                        TryCast(toolCallToken, JObject)

                        If toolCall Is Nothing Then Continue For

                        Dim index As Integer =
                        toolCall.Value(Of Integer?)("index").GetValueOrDefault(0)

                        Dim pending As StrataPendingToolCall = Nothing

                        If Not pendingToolCalls.TryGetValue(index, pending) Then

                            pending = New StrataPendingToolCall()
                            pendingToolCalls.Add(index, pending)

                        End If

                        Dim callId As String = toolCall.Value(Of String)("id")

                        If Not String.IsNullOrEmpty(callId) Then
                            pending.Id = callId
                        End If

                        Dim functionObject As JObject = TryCast(toolCall("function"), JObject)

                        If functionObject IsNot Nothing Then

                            Dim functionName As String = functionObject.Value(Of String)("name")

                            If Not String.IsNullOrEmpty(functionName) Then
                                pending.Name.Append(functionName)
                            End If

                            Dim argumentsPart As String = functionObject.Value(Of String)("arguments")

                            If Not String.IsNullOrEmpty(argumentsPart) Then
                                pending.Arguments.Append(argumentsPart)
                            End If

                        End If

                    Next

                End If

            End If

            ' ------------------------------------------
            ' Finish reason
            ' ------------------------------------------

            Dim finishReason As String = choice.Value(Of String)("finish_reason")

            If Not String.IsNullOrEmpty(finishReason) Then
                result.DoneReason = finishReason
            End If

            ' ------------------------------------------
            ' Token usage
            ' ------------------------------------------

            Dim usages As JObject = TryCast(chunk("usage"), JObject)

            If usages IsNot Nothing Then

                result.PromptEvalCount = usages.Value(Of Integer?)("prompt_tokens").GetValueOrDefault(0)

                result.EvalCount = usages.Value(Of Integer?)("completion_tokens").GetValueOrDefault(0)

            End If

        End While

        ' ------------------------------------------
        ' Finalize tool calls
        ' ------------------------------------------

        For Each pending As StrataPendingToolCall In pendingToolCalls.Values

            Dim argumentsJson As String = pending.Arguments.ToString()

            If String.IsNullOrWhiteSpace(argumentsJson) Then
                argumentsJson = "{}"
            End If

            Dim arguments As JObject = JObject.Parse(argumentsJson)

            result.ToolCalls.Add(
            New InferenceProviderToolCall With {
                .Id = pending.Id,
                .Name = pending.Name.ToString(),
                .Arguments = arguments
            })

        Next

        ' ------------------------------------------
        ' Flush remaining event buffers
        ' ------------------------------------------

        If _thinkingBuffer.Length > 0 Then

            RaiseEvent ThinkingReceived(_thinkingBuffer.ToString())
            _thinkingBuffer.Clear()

        End If

        If _contentBuffer.Length > 0 Then

            RaiseEvent ContentReceived(_contentBuffer.ToString())
            _contentBuffer.Clear()

        End If

        result.Thinking = thinkingBuilder.ToString()
        result.Content = contentBuilder.ToString()

        result.Statistics = _sessionStats.ToString()

        RaiseEvent StatisticsReceived(result.Statistics)

        Debug.WriteLine($"Strata finished: Reason={result.DoneReason}, " &
            $"Prompt={result.PromptEvalCount}, " &
            $"Generated={result.EvalCount}, " &
            $"Tools={result.ToolCalls.Count}, " &
            $"ContentLength={result.Content.Length}"
        )

        Return result
    End Function

    Private Shared Function ShouldFlush(buffer As StringBuilder, lastFlush As DateTime) As Boolean
        If buffer.Length = 0 Then
            Return False
        End If

        If DateTime.UtcNow - lastFlush >= TimeSpan.FromMilliseconds(75) Then
            Return True
        End If

        Dim lastChar = buffer(buffer.Length - 1)

        Return lastChar = ControlChars.Lf OrElse lastChar = "."c OrElse lastChar = ":"c OrElse lastChar = ";"c
    End Function

    Public Async Function GetModelsAsync(cancellationToken As CancellationToken) As Task(Of List(Of InferenceProviderModel)) Implements IInferenceProvider.GetModelsAsync
        Using request As New HttpRequestMessage(HttpMethod.Get, "/v1/models")
            Using response = Await _http.SendAsync(request, cancellationToken)
                response.EnsureSuccessStatusCode()

                Dim json = Await response.Content.ReadAsStringAsync()

                Dim root = JObject.Parse(json)

                Return root("models").Select(
                    Function(m) New InferenceProviderModel With {
                        .Name = m.Value(Of String)("name"),
                        .Size = m.Value(Of Long)("size")
                    }).ToList()
            End Using
        End Using
    End Function

    Public Sub SetSettings(settings As VSAgentSettings) Implements IInferenceProvider.SetSettings
        _settings = settings
    End Sub

    Public Async Function GetContextLengthAsync(cancellationToken As CancellationToken) As Task(Of Integer) Implements IInferenceProvider.GetContextLengthAsync
        Using request As New HttpRequestMessage(HttpMethod.Get, "/api/ps")
            Using response = Await _http.SendAsync(request, cancellationToken)
                response.EnsureSuccessStatusCode()

                Dim json = Await response.Content.ReadAsStringAsync()

                Dim root = JObject.Parse(json)

                Return root("models").Select(Function(m) m.Value(Of Integer)("context_length")).FirstOrDefault()
            End Using
        End Using
    End Function


    Private Class StrataPendingToolCall
        Public Property Id As String = ""
        Public ReadOnly Property Name As New StringBuilder()
        Public ReadOnly Property Arguments As New StringBuilder()
    End Class

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                _http.Dispose()
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override finalizer
            ' TODO: set large fields to null
            disposedValue = True
        End If
    End Sub

    ' ' TODO: override finalizer only if 'Dispose(disposing As Boolean)' has code to free unmanaged resources
    ' Protected Overrides Sub Finalize()
    '     ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
    '     Dispose(disposing:=False)
    '     MyBase.Finalize()
    ' End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub

    Public Function SerializeToolArguments(arguments As JObject) As JToken Implements IInferenceProvider.SerializeToolArguments
        Return New JValue(arguments.ToString(Formatting.None))
    End Function
End Class

