Imports System.IO
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Threading
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports VSAgent.Ollama

Public Class OllamaClient
    Implements IDisposable

    Private ReadOnly _http As HttpClient
    Private ReadOnly _model As String
    Private disposedValue As Boolean

    Private ReadOnly _thinkingBuffer As New StringBuilder()
    Private ReadOnly _contentBuffer As New StringBuilder()

    Private _lastThinkingFlush As DateTime = DateTime.UtcNow
    Private _lastContentFlush As DateTime = DateTime.UtcNow

    Public Event ThinkingReceived(text As String)
    Public Event ContentReceived(text As String)


    Public Sub New(baseUrl As String, model As String)
        _model = model

        _http = New HttpClient With {
            .BaseAddress = New Uri(baseUrl),
            .Timeout = Timeout.InfiniteTimeSpan
        }

        _http.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", "ollama")
    End Sub

    Public Async Function SendAsync(messages As JArray, tools As JArray, cancellationToken As CancellationToken) As Task(Of OllamaAssistantResponse)
        Dim body As New JObject From {
            {"model", _model},
            {"messages", messages},
            {"tools", tools},
            {"tool_choice", "auto"},
            {"think", True},
            {"stream", True}
        }

        Using request As New HttpRequestMessage(HttpMethod.Post, "api/chat")
            request.Content = New StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json")

            Using response = Await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)

                Dim errorText As String = Nothing

                If Not response.IsSuccessStatusCode Then
                    errorText = Await response.Content.ReadAsStringAsync(cancellationToken)

                    Throw New InvalidOperationException($"Ollama returned {CInt(response.StatusCode)}: {errorText}")
                End If

                Using stream = Await response.Content.ReadAsStreamAsync(cancellationToken)
                    Using reader As New StreamReader(stream)
                        Return Await ReadStreamAsync(reader, cancellationToken)
                    End Using
                End Using
            End Using
        End Using
    End Function

    Private Async Function ReadStreamAsync(reader As StreamReader, cancellationToken As CancellationToken) As Task(Of OllamaAssistantResponse)
        Dim result As New OllamaAssistantResponse()
        Dim thinkingBuilder As New StringBuilder
        Dim contentBuilder As New StringBuilder()

        While True

            cancellationToken.ThrowIfCancellationRequested()

            Dim line = Await reader.ReadLineAsync(cancellationToken)

            ' This will handle the httpstream ending unexpectedly
            If line Is Nothing Then
                Exit While
            End If

            If String.IsNullOrWhiteSpace(line) Then
                Continue While
            End If

            Dim chunk = JObject.Parse(line)

            Dim message = TryCast(chunk("message"), JObject)

            If message IsNot Nothing Then

                Dim thinking = message.Value(Of String)("thinking")

                If Not String.IsNullOrEmpty(thinking) Then
                    thinkingBuilder.Append(thinking)
                    _thinkingBuffer.Append(thinking)

                    If ShouldFlush(_thinkingBuffer, _lastThinkingFlush) Then
                        Dim text = _thinkingBuffer.ToString()

                        _thinkingBuffer.Clear()
                        _lastThinkingFlush = DateTime.UtcNow

                        RaiseEvent ThinkingReceived(text)
                    End If
                End If

                Dim content = message.Value(Of String)("content")

                If Not String.IsNullOrEmpty(content) Then
                    contentBuilder.Append(content)
                    _contentBuffer.Append(content)
                    If ShouldFlush(_contentBuffer, _lastContentFlush) Then
                        Dim text = _contentBuffer.ToString()

                        _contentBuffer.Clear()
                        _lastContentFlush = DateTime.UtcNow

                        RaiseEvent ContentReceived(text)
                    End If
                End If

                Dim toolCalls = TryCast(message("tool_calls"), JArray)

                If toolCalls IsNot Nothing Then
                    For Each toolCallToken In toolCalls
                        Dim toolCall = TryCast(toolCallToken, JObject)

                        If toolCall Is Nothing Then
                            Continue For
                        End If

                        Dim functionObject = TryCast(toolCall("function"), JObject)

                        If functionObject Is Nothing Then
                            Continue For
                        End If

                        Dim arguments = TryCast(functionObject("arguments"), JObject)

                        result.ToolCalls.Add(
                            New OllamaToolCall With {
                                .Id = toolCall.Value(Of String)("id"),
                                .Name = functionObject.Value(Of String)("name"),
                                .Arguments = If(arguments, New JObject())
                            })
                    Next
                End If
            End If

            ' When done is received
            If chunk.Value(Of Boolean?)("done").GetValueOrDefault(False) Then
                Exit While
            End If
        End While

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
End Class