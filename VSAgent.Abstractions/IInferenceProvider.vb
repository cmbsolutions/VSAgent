Imports System.IO
Imports System.Threading
Imports Newtonsoft.Json.Linq
Imports VSAgent.Protocol.InferenceProvider
Imports VSAgent.Protocol.Settings

Public Interface IInferenceProvider
    Function SendAsync(messages As JArray, tools As JArray, cancellationToken As CancellationToken) As Task(Of InferenceProviderAssistantResponse)
    Function ReadStreamAsync(reader As StreamReader, cancellationToken As CancellationToken) As Task(Of InferenceProviderAssistantResponse)
    Function GetModelsAsync(cancellationToken As CancellationToken) As Task(Of List(Of InferenceProviderModel))
    Function GetContextLengthAsync(cancellationToken As CancellationToken) As Task(Of Integer)
    Sub SetSettings(settings As VSAgentSettings)
End Interface
