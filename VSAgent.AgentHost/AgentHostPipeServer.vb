Imports VSAgent.Messages

Public Class AgentHostPipeServer
    Private ReadOnly _runner As AgentRunner
    Private ReadOnly _transport As Transport.TransportPipeServer(Of AgentHostRequest, AgentHostResponse)

    Public Sub New(PipeName As String, runner As AgentRunner)
        _runner = runner

        _transport = New Transport.TransportPipeServer(Of AgentHostRequest, AgentHostResponse)(PipeName, AddressOf HandleRequestAsync)
        _transport.Start()
    End Sub

    Public Async Function StopAsync() As Task
        If _transport Is Nothing Then
            Return
        End If

        Await _transport.StopAsync
    End Function

    Private Async Function HandleRequestAsync(request As AgentHostRequest) As Task(Of AgentHostResponse)

        Select Case request.Type
            Case "prompt"
                Dim result = Await _runner.RunAsync(request.Content)

                Return New AgentHostResponse With {
                    .RequestId = request.Id,
                    .Success = True,
                    .Content = result
                }

            Case Else
                Return New AgentHostResponse With {
                    .RequestId = request.Id,
                    .Success = False,
                    .ErrorMessage = $"Unknown request type: {request.Type}"
                }

        End Select

    End Function
End Class
