Public Class ToolRegistry
    Private ReadOnly _tools As Dictionary(Of String, ITool)

    Public Sub New()
        _tools = New Dictionary(Of String, ITool)(StringComparer.OrdinalIgnoreCase)
    End Sub

    Public Sub Register(tool As ITool)
        _tools(tool.Name) = tool
    End Sub

    Public Function GetTool(name As String) As ITool
        Dim tool As ITool = Nothing

        _tools.TryGetValue(name, tool)
        Return tool
    End Function
End Class
