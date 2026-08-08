Imports VSAgent.Protocol.Tools
Public Interface ITool
    ReadOnly Property Name As String
    ReadOnly Property Description As String
    ReadOnly Property ParametersSchema As ToolParameterSchema
    ReadOnly Property Version As Integer

    Function ExecuteAsync(request As Protocol.Messages.AgentRequest) As Task(Of Protocol.Messages.AgentResponse)
End Interface
