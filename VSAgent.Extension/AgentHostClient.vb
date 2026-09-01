Imports Newtonsoft.Json.Linq
Imports VSAgent.Protocol.Events
Imports VSAgent.Protocol.Messages

Public Class AgentHostClient
    Implements IDisposable

    Private ReadOnly _transport As Transport.TransportPipeClient(Of AgentHostRequest, AgentHostResponse)

    Public Event Thinking(text As String)
    Public Event Content(text As String)

    Public Event ToolStarted(toolName As String, actionDescription As String)
    Public Event ToolCompleted(toolName As String)
    Public Event ToolFailed(toolName As String, errorMessage As String)


    Public Sub New(PipeName As String)
        _transport = New Transport.TransportPipeClient(Of AgentHostRequest, AgentHostResponse)(PipeName)

        AddHandler _transport.EventReceived, AddressOf Transport_OnEventReceived
    End Sub

    Private Sub Transport_OnEventReceived(payload As JObject)
        Dim hostEvent = payload.ToObject(Of AgentHostEvent)()

        If hostEvent Is Nothing Then
            Return
        End If

        Select Case hostEvent.Type
            Case "thinking"
                RaiseEvent Thinking(hostEvent.Text)
            Case "content"
                RaiseEvent Content(hostEvent.Text)
            Case "toolStarted"
                RaiseEvent ToolStarted(hostEvent.ToolName, hostEvent.ActionDescription)
            Case "toolCompleted"
                RaiseEvent ToolCompleted(hostEvent.ToolName)
            Case "toolFailed"
                RaiseEvent ToolFailed(hostEvent.ToolName, hostEvent.Text)
            Case Else
                Throw New InvalidOperationException($"Unknown eventtype received: {hostEvent.Type}")
        End Select
    End Sub

    Public Function ConnectAsync() As Task
        Return _transport.ConnectAsync()
    End Function

    Public Function SendPromptAsync(prompt As String) As Task(Of AgentHostResponse)

        Dim request As New AgentHostRequest With {
            .Id = Guid.NewGuid().ToString(),
            .Type = "prompt",
            .Content = prompt
        }
        Return _transport.SendAsync(request)
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            _transport?.Dispose()
        Catch
        End Try
    End Sub
End Class