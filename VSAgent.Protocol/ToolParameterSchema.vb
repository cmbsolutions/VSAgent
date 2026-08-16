Imports Newtonsoft.Json

Namespace Tools
    Public Class ToolParameterSchema
        <JsonProperty("type")>
        Public Property Type As String
        <JsonProperty("properties")>
        Public Property Properties As Dictionary(Of String, ToolPropertySchema)
        <JsonProperty("required")>
        Public Property Required As List(Of String)
    End Class

    Public Class ToolPropertySchema
        <JsonProperty("type")>
        Public Property Type As String
        <JsonProperty("description", NullValueHandling:=NullValueHandling.Ignore)>
        Public Property Description As String
        <JsonProperty("items", NullValueHandling:=NullValueHandling.Ignore)>
        Public Property Items As ToolPropertySchema
    End Class
End Namespace

