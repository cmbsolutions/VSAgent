Imports VSAgent.Protocol.Messages

Public Class AgentHostClient
    Implements IDisposable

    Private ReadOnly _transport As Transport.TransportPipeClient(Of AgentHostRequest, AgentHostResponse)
    Public Sub New(PipeName As String)
        _transport = New Transport.TransportPipeClient(Of AgentHostRequest, AgentHostResponse)(PipeName)
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