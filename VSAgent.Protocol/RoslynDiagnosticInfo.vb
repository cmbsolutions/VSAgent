Namespace DTO
    Public Class RoslynDiagnosticInfo
        Public Property Id As String
        Public Property Severity As String
        Public Property Message As String

        Public Property ProjectName As String
        Public Property DocumentId As String
        Public Property FilePath As String

        Public Property Line As Integer
        Public Property Column As Integer
    End Class
End Namespace