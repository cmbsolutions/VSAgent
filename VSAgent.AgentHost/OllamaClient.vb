Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class OllamaClient
    Implements IDisposable

    Private ReadOnly _http As HttpClient
    Private ReadOnly _model As String
    Private disposedValue As Boolean

    Public Sub New(baseUrl As String, model As String)
        _model = model

        _http = New HttpClient() With {
            .BaseAddress = New Uri(baseUrl)
        }

        _http.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", "ollama")
    End Sub

    Public Async Function SendAsync(messages As JArray, tools As JArray) As Task(Of JObject)
        Dim body As New JObject From {
            {"model", _model},
            {"messages", messages},
            {"tools", tools},
            {"tool_choice", "auto"}
        }

        Using content As New StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json")
            Using response = Await _http.PostAsync("chat/completions", content)

                Dim json = Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    Throw New InvalidOperationException($"Ollama returned {(CInt(response.StatusCode))}: {json}")
                End If

                Return JObject.Parse(json)
            End Using
        End Using
    End Function

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects)
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