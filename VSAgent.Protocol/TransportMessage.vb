Imports Newtonsoft.Json.Linq

Namespace Messages
    Public Class TransportMessage
        Public Property MessageType As String ' For now only request, response or event
        Public Property RequestId As String
        Public Property Payload As JObject
    End Class
End Namespace
