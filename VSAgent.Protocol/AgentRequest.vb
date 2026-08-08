Imports VSAgent.Protocol.Tools

Namespace Messages
    Public Class AgentRequest
        Public Property Id As String
        Public Property Tool As String
        Public Property Version As Integer
        Public Property Parameters As Dictionary(Of String, Object)
    End Class
End Namespace