Imports Newtonsoft.Json.Linq
Imports VSAgent.Protocol.Messages

Namespace Messages
    Public Class AgentHostResponse
        Public Property RequestId As String
        Public Property Version As Integer
        Public Property Success As Boolean
        Public Property Result As Object
        Public Property ErrorMessage As String
        Public Property Content As String

        Public Shared Function Ok(id As String, version As Integer, result As Object) As AgentHostResponse
            Return New AgentHostResponse With {
                .RequestId = id,
                .Version = version,
                .Success = True,
                .Result = result
            }
        End Function

        Public Shared Function Failed(id As String, version As Integer, message As String) As AgentHostResponse
            Return New AgentHostResponse With {
                .RequestId = id,
                .Version = version,
                .Success = False,
                .ErrorMessage = message
            }
        End Function

        Public Function GetResult(Of T)() As T

            If Result Is Nothing Then
                Return Nothing
            End If

            Dim token = TryCast(Result, JToken)

            If token IsNot Nothing Then
                Return token.ToObject(Of T)()
            End If

            Return JObject.FromObject(Result).ToObject(Of T)()
        End Function
    End Class
End Namespace