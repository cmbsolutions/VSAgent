Imports VSAgent.Protocol

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

    Public Function GetAvailableTools() As IReadOnlyList(Of DTO.ToolDescriptor)
        Return _tools.Values _
            .Select(Function(tool) New DTO.ToolDescriptor With {
                .Name = tool.Name,
                .Description = tool.Description,
                .Parameters = tool.ParametersSchema
            }) _
            .OrderBy(Function(tool) tool.Name) _
            .ToList()
    End Function
End Class
