Imports System.Text
Imports Newtonsoft.Json.Linq

Namespace Ollama

    Public Class OllamaSessionStatistics
        Private _statistics As List(Of OllamaSessionStatistic)

        Sub New()
            _statistics = New List(Of OllamaSessionStatistic)
        End Sub

        Public Sub AddStatistic(chunk As JObject)
            _statistics.Add(New OllamaSessionStatistic With {
                    .Id = _statistics.Count,
                    .TotalDuration = chunk.Value(Of Int64?)("total_duration").GetValueOrDefault(0),
                    .LoadDuration = chunk.Value(Of Int64?)("load_duration").GetValueOrDefault(0),
                    .PromptEvalCount = chunk.Value(Of Int64?)("prompt_eval_count").GetValueOrDefault(0),
                    .PromptEvalDuration = chunk.Value(Of Int64?)("prompt_eval_duration").GetValueOrDefault(0),
                    .EvalCount = chunk.Value(Of Int64?)("eval_count").GetValueOrDefault(0),
                    .EvalDuration = chunk.Value(Of Int64?)("eval_duration").GetValueOrDefault(0)
                    })
        End Sub

        Public Overrides Function ToString() As String
            Dim builder As New StringBuilder

            Dim TotalPromptEvalCount = _statistics.Sum(Function(s) s.PromptEvalCount)
            Dim TotalPromptEvalDuration = _statistics.Sum(Function(s) s.PromptEvalDuration)
            Dim tpedtimespan As TimeSpan = TimeSpan.FromMicroseconds(TotalPromptEvalDuration \ 1000)
            Dim tps = TotalPromptEvalCount / tpedtimespan.TotalSeconds

            Dim TotalOutputEvalCount = _statistics.Sum(Function(s) s.EvalCount)
            Dim TotalOutputEvalDuration = _statistics.Sum(Function(s) s.EvalDuration)
            Dim toedtimespan As TimeSpan = TimeSpan.FromMicroseconds(TotalOutputEvalDuration \ 1000)
            Dim ots = TotalOutputEvalCount / toedtimespan.TotalSeconds

            Return $"Prompt: {TotalPromptEvalCount}T, {tps:F3}T/s. Output: {TotalOutputEvalCount}T, {ots:F3}T/s."
        End Function
    End Class

    Public Class OllamaSessionStatistic
        Property Id As Integer
        Property TotalDuration As Int64
        Property LoadDuration As Int64
        Property PromptEvalCount As Int64
        Property PromptEvalDuration As Int64
        Property EvalCount As Int64
        Property EvalDuration As Int64
    End Class
End Namespace