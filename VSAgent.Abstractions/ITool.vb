Public Interface ITool
    ReadOnly Property Name As String

    Function ExecuteAsync(request As Protocol.Messages.AgentRequest) As Task(Of Protocol.Messages.AgentResponse)
End Interface
