Imports Newtonsoft.Json
Imports VSAgent.Protocol.Tools

Namespace DTO
    Public Class ToolDescriptor
        <JsonProperty("name")>
        Public Property Name As String
        <JsonProperty("version")>
        Public Property Version As Integer
        <JsonProperty("description")>
        Public Property Description As String
        <JsonProperty("actiondescription")>
        Public Property ActionDescription As String
        <JsonProperty("parameters")>
        Public Property Parameters As ToolParameterSchema
    End Class
End Namespace
