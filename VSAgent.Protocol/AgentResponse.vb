Namespace Messages
    Public Class AgentResponse
        Public Property Id As String
        Public Property Success As Boolean
        Public Property Result As Object
        Public Property ErrorMessage As String

        Public Shared Function Ok(id As String, result As Object) As AgentResponse
            Return New AgentResponse With {
                .Id = id,
                .Success = True,
                .Result = result
            }
        End Function

        Public Shared Function Failed(id As String, message As String) As AgentResponse
            Return New AgentResponse With {
                .Id = id,
                .Success = False,
                .ErrorMessage = message
            }
        End Function
    End Class
End Namespace