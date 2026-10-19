Namespace DTO
    Public Class InspectTypeInfo
        Public Property TypeName As String
        Public Property Kind As String
        Public Property Assembly As String
        Public Property Members As New List(Of InspectTypeMemberInfo)
    End Class

    Public Class InspectTypeMemberInfo
        Public Property Kind As String
        Public Property Name As String
        Public Property Signature As String
    End Class
End Namespace