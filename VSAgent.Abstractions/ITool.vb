Imports VSAgent.Protocol.Tools
Public Interface ITool
    ReadOnly Property Name As String
    ReadOnly Property Description As String
    ReadOnly Property ParametersSchema As ToolParameterSchema

    Function ExecuteAsync(request As Protocol.Messages.AgentRequest) As Task(Of Protocol.Messages.AgentResponse)
End Interface
