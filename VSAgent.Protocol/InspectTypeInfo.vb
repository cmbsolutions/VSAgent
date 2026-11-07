Namespace DTO
    Public Class InspectTypeInfo
        Public Property TypeName As String
        Public Property Assembly As String
        Public Property BaseType As String
        Public Property Members As New List(Of InspectTypeMemberInfo)

        Public Overrides Function ToString() As String
            Return $"{TypeName}, {BaseType}, {Assembly}, {Members.Count}"
        End Function
    End Class

    Public Class InspectTypeMemberInfo
        Public Property Kind As String
        Public Property Name As String
        Public Property Signature As String
        Public Property DeclaredIn As String
        Public Property IsOverridable As Boolean
    End Class
End Namespace