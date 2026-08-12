Imports Newtonsoft.Json.Linq

Namespace Messages
    Public Class AgentResponse
        Public Property Id As String
        Public Property Version As Integer
        Public Property Success As Boolean
        Public Property Result As Object
        Public Property ErrorMessage As String

        Public Shared Function Ok(id As String, version As Integer, result As Object) As AgentResponse
            Return New AgentResponse With {
                .Id = id,
                .Version = version,
                .Success = True,
                .Result = result
            }
        End Function

        Public Shared Function Failed(id As String, version As Integer, message As String) As AgentResponse
            Return New AgentResponse With {
                .Id = id,
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