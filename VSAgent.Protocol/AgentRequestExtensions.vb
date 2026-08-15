Namespace Messages
    Public Module AgentRequestExtensions

        <Runtime.CompilerServices.Extension>
        Public Function GetParameters(Of T As New)(request As AgentRequest) As T

            If request.Parameters Is Nothing Then
                Return New T()
            End If

            Return request.Parameters.ToObject(Of T)()
        End Function
    End Module
End Namespace
