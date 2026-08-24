Imports Newtonsoft.Json.Linq
Imports VSAgent.Protocol.DTO

Public Class AgentHelpers
    Public Shared Function BuildOpenAITools(descriptors As IReadOnlyList(Of ToolDescriptor)) As JArray
        Dim result As New JArray()

        For Each descriptor In descriptors

            If String.Equals(descriptor.Name, "getAvailableTools", StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If

            result.Add(
                New JObject From {
                    {
                        "type",
                        "function"
                    },
                    {
                        "function",
                        New JObject From {
                            {"name", descriptor.Name},
                            {"description", descriptor.Description},
                            {
                                "parameters",
                                JObject.FromObject(descriptor.Parameters)
                            }
                        }
                    }
                })
        Next

        Return result
    End Function
End Class
