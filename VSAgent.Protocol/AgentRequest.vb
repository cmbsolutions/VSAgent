Imports Newtonsoft.Json.Linq

Namespace Messages
    Public Class AgentRequest
        Public Property Id As String
        Public Property Tool As String
        Public Property Version As Integer
        Public Property Parameters As JObject
    End Class
End Namespace