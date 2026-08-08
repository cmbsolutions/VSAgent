Namespace Tools
    Public Class ToolParameterSchema
        Public Property Type As String
        Public Property Properties As Dictionary(Of String, ToolPropertySchema)
        Public Property Required As List(Of String)
    End Class

    Public Class ToolPropertySchema
        Public Property Type As String
        Public Property Description As String
    End Class
End Namespace

