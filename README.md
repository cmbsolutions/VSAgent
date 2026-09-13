[![wakatime](https://wakatime.com/badge/github/cmbsolutions/VSAgent.svg)](https://wakatime.com/badge/github/cmbsolutions/VSAgent)

# VSAgent

An AI-powered Visual Studio extension that connects Visual Studio's Roslyn compiler platform with large language models (LLMs) through a tool-based agent architecture. It enables an LLM to inspect, modify, build, and navigate .NET codebases programmatically via named-pipe IPC.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
  - [System Diagram](#system-diagram)
  - [Component Description](#component-description)
- [Projects](#projects)
  - [VSAgent.Abstractions](#vsagentabstractions)
  - [VSAgent.Protocol](#vsagentprotocol)
  - [VSAgent.Transport](#vsagenttransport)
  - [VSAgent.Server](#vsagentserver)
  - [VSAgent.Tools](#vsagenttools)
  - [VSAgent.AgentHost](#vsagentagenthost)
  - [VSAgent.Extension](#vsagentextension)
- [Available Tools](#available-tools)
- [Communication Protocol](#communication-protocol)
- [Build & Target Frameworks](#build--target-frameworks)
- [Dependencies](#dependencies)
- [Key Workflows](#key-workflows)
  - [Tool Execution Flow](#tool-execution-flow)
  - [LLM Agent Loop](#llm-agent-loop)
- [Getting Started](#getting-started)
- [File Statistics](#file-statistics)

---

## Overview

VSAgent bridges Visual Studio's Roslyn workspace with LLMs (via [Ollama](https://ollama.com/)) through a **tool-calling agent pattern**. The extension hosts a server that exposes Roslyn-based tools. A standalone AgentHost process connects to this server, sends tool calls on behalf of the LLM, and feeds results back into the model's conversation context.

The system is written in **VB.NET** and consists of **7 projects** organized into distinct layers:

| Layer | Projects |
|---|---|
| Abstractions | `VSAgent.Abstractions` |
| Protocol / DTOs | `VSAgent.Protocol` |
| Transport (IPC) | `VSAgent.Transport` |
| VS Extension | `VSAgent.Extension`, `VSAgent.Server` |
| Tools (Tool Implementations + Registry) | `VSAgent.Tools` |
| Agent Host (Standalone CLI) | `VSAgent.AgentHost` |

---

## Architecture

### System Diagram

```
┌─────────────────────────────────────-─┐
│          Visual Studio IDE            │
│                                       │
│  ┌──────────────────────────────┐     │
│  │   VSAgent.Extension (VSIX)   │     │
│  │                              │     │
│  │  • ToolWindow / Package      │     │
│  │  • RoslynWorkspaceService    │     │
│  │  • BuildService              │     │
│  │  • DocumentService           │     │
│  │  • SymbolService             │     │
│  │  • DiagnosticsService        │     │
│  │  • SolutionService           │     │
│  └──────────┬───────────────────┘     │
│             │                         │
│  ┌──────────▼───────────────────┐     │
│  │   VSAgent.Server             │     │
│  │   (AgentPipeServer)          │     │
│  │   Named Pipe: "VSAgent"      │     │
│  │   Hosts ToolRegistry         │     │
│  └──────────┬───────────────────┘     │
│             │                         │
├─────────────▼─────────────────────────┤
│         Named Pipe IPC                │
│         (TransportPipeClient/Server)  │
├─────────────▲─────────────────────────┤
│             │                         │
│  ┌──────────┴───────────────────┐     │
│  │   VSAgent.AgentHost          │     │
│  │   (Standalone .NET 10 CLI)   │     │
│  │                              │     │
│  │  • OllamaClient              │     │
│  │  • AgentRunner               │     │
│  │  • AgentHostPipeServer       │     │
│  └──────────┬───────────────────┘     │
│             │                         │
│  ┌──────────▼───────────────────┐     │
│  │         Ollama LLM           │     │
│  │         (qwen3.6:35b)        │     │
│  └──────────────────────────────┘     │
└────────────────────────────────────-──┘
```

### Component Description

1. **VSAgent.Abstractions** — Defines all interfaces (`ITool`, `IRoslynWorkspaceService`, `ISymbolService`, `IDocumentService`, `IDocumentEditService`, `IBuildService`, `ISolutionService`, `IRoslynDiagnosticsService`). These form the contract between the VS extension and tools.

2. **VSAgent.Protocol** — Contains all DTOs (Data Transfer Objects), messages, and schemas used for inter-process communication. Targets **.NET Standard 2.0** so it can be shared across projects.

3. **VSAgent.Transport** — Generic named-pipe transport layer (`TransportPipeClient<TRequest,TResponse>` and `TransportPipeServer<TRequest,TResponse>`) built on .NET's `NamedPipe*Stream`. Handles serialization/deserialization via Newtonsoft.Json, request/response correlation with `TaskCompletionSource`, and event broadcasting.

4. **VSAgent.Server** — The server-side component that listens on a named pipe (`"VSAgent"`). It receives `AgentRequest` messages, dispatches them to registered tools via `ToolRegistry`, and returns `AgentResponse` results.

5. **VSAgent.Tools** — Implements all 14 tool classes (each `ITool`) and the `ToolRegistry`. Targeted at **.NET Framework 4.7.2**.

6. **VSAgent.AgentHost** — The standalone agent application targeting **.NET 10.0**. It connects to the VS server via named pipes, communicates with Ollama LLMs, and runs the agent loop (prompt → LLM → tool calls → results → repeat).

7. **VSAgent.Extension** — The Visual Studio Extension package (VSIX) targeting **.NET Framework 4.7.2**. It provides the VS-specific implementations of all service interfaces, hosts the Tool Window UI, and manages the AgentHost lifecycle.

---

## Projects

### VSAgent.Abstractions

| Property | Value |
|---|---|
| Language | VB.NET |
| Target Framework | .NET Framework 4.7.2 |
| Document Count | 14 |
| Project References | 1 |
| Metadata References | 10 |

**Key Interfaces:**

| Interface | Purpose |
|---|---|
| `ITool` | Core contract for all tool implementations (Name, Description, ActionDescription, ParametersSchema, Version, ExecuteAsync) |
| `IRoslynWorkspaceService` | Get projects and project documents from the Roslyn workspace |
| `ISymbolService` | Find symbols and references by name or position |
| `IDocumentService` | Get active document info and read document content |
| `IDocumentEditService` | Apply edits, add documents, remove documents |
| `IBuildService` | Build solution or individual projects |
| `ISolutionService` | Get solution info and list all projects |
| `IRoslynDiagnosticsService` | Retrieve compiler diagnostics for the current solution |
| `IToolRegistry` | Registry of available tools (interface) |

### VSAgent.Protocol

| Property | Value |
|---|---|
| Language | VB.NET |
| Target Framework | .NET Standard 2.0 |
| Output Path | `bin\Debug
etstandard2.0\VSAgent.Protocol.dll` |
| Document Count | 32 (including source + generated) |
| Project References | 0 |
| Metadata References | 114 |

**Data Transfer Objects (DTOs):**

| DTO Class | Namespace | Purpose |
|---|---|---|
| `SolutionInfo` | `VSAgent.Protocol.DTO` | Solution name, file path, directory path |
| `ProjectInfo` | `VSAgent.Protocol.DTO` | SDK-style project metadata |
| `RoslynProjectInfo` | `VSAgent.Protocol.DTO` | Full Roslyn project details (id, name, assembly name, language, doc count, ref counts) |
| `RoslynDocumentInfo` | `VSAgent.Protocol.DTO` | Document ID, name, path, parent project, folder hierarchy |
| `RoslynDocument` | `VSAgent.Protocol.DTO` | Full document content with metadata |
| `ActiveDocumentInfo` | `VSAgent.Protocol.DTO` | Currently active editor document with caret/selection info |
| `RoslynSymbolInfo` | `VSAgent.Protocol.DTO` | Symbol: name, kind, fully qualified name, project/doc location, line/column |
| `RoslynSymbolReferenceInfo` | `VSAgent.Protocol.DTO` | A reference to a symbol from another location |
| `RoslynDiagnosticInfo` | `VSAgent.Protocol.DTO` | Compiler diagnostic: severity, warning/error code, message, location |
| `BuildResult` | `VSAgent.Protocol.DTO` | Build outcome with success/failure status |
| `DocumentEditResult` | `VSAgent.Protocol.DTO` | Result of a document edit operation |
| `AddDocumentResult` | `VSAgent.Protocol.DTO` | Result of adding a new document |
| `RemoveDocumentResult` | `VSAgent.Protocol.DTO` | Result of removing a document |
| `ToolDescriptor` | `VSAgent.Protocol.DTO` | Tool name, version, description, action description, parameter schema |
| `ToolParameterSchema` | `VSAgent.Protocol.Tools` | JSON Schema for tool parameters |
| `ToolPropertySchema` | `VSAgent.Protocol.Tools` | Property-level schema (type, description) |

**Message Types:**

| Message Class | Namespace | Purpose |
|---|---|---|
| `AgentRequest` | `VSAgent.Protocol.Messages` | Request to invoke a tool (id, tool name, parameters) |
| `AgentResponse` | `VSAgent.Protocol.Messages` | Response from tool execution (success/failure, result data) |
| `TransportMessage` | `VSAgent.Protocol` | Wrapped message with type ("request"/"response"/"event"), request ID, and JSON payload |

**Parameter DTOs:**

| Class | Used By |
|---|---|
| `FindSymbolParameters` | `findSymbol` |
| `FindReferenceParameters` | `findReferences` |
| `ReadDocumentParameters` | `readDocument` |
| `ApplyDocumentEditParameters` | `applyDocumentEdit` |
| `AddDocumentParameters` | `addDocument` |
| `RemoveDocumentParameters` | `removeDocument` |
| `BuildProjectParameters` | `buildProject` |
| `GetProjectDocumentsParameters` | `getProjectDocuments` |

### VSAgent.Transport

| Property | Value |
|---|---|
| Language | VB.NET |
| Target Framework | .NET Standard 2.0 |
| Output Path | `bin\Debug
etstandard2.0\VSAgent.Transport.dll` |
| Document Count | 4 (including generated) |
| Project References | 1 |
| Metadata References | 114 |

**Key Classes:**

| Class | Purpose |
|---|---|
| `TransportPipeClient<TRequest,TResponse>` | Named pipe client — connects to server, sends requests with correlation IDs, reads responses/events in a background loop |
| `TransportPipeServer<TRequest,TResponse>` | Named pipe server — listens for connections, processes incoming requests via a handler delegate, responds, supports events |

**Protocol Details:**
- Communication uses **JSON** serialized via Newtonsoft.Json
- Messages are line-delimited (one JSON object per line)
- Request/response correlation via GUID-based request IDs and `TaskCompletionSource<T>`
- Message types: `"request"`, `"response"`, `"event"`
- Thread safety: Write lock (`SemaphoreSlim`) prevents message entanglement

### VSAgent.Server

| Property | Value |
|---|---|
| Language | VB.NET |
| Target Framework | .NET Framework 4.7.2 |
| Document Count | 6 (including generated) |
| Project References | 4 |
| Metadata References | 22 |

**Key Class:**

| Class | Purpose |
|---|---|
| `AgentPipeServer` | Wraps the transport server and tool registry. Listens on named pipe `"VSAgent"`. When a request arrives, it looks up the tool by name in the registry, calls `ExecuteAsync(request)`, wraps the result as an `AgentResponse`, and sends it back. Handles unknown tools gracefully with a failure response. |

### VSAgent.Tools

| Property | Value |
|---|---|
| Language | VB.NET |
| Target Framework | .NET Framework 4.7.2 |
| Document Count | 22 (including generated) |
| Project References | 2 |
| Metadata References | 11 |

**Tool Registry:**

| Class | Purpose |
|---|---|
| `ToolRegistry` | Dictionary-based registry of tools keyed by name (case-insensitive). Registers tools via `Register(ITool)`, retrieves via `GetTool(name)`, and exposes all registered tools as `ToolDescriptor` list via `GetAvailableTools()`. |

**Available Tools (14 total):**

| Tool Name | Description | Parameters | Action Description |
|---|---|---|---|
| `ping` | Checks whether the VSAgent server is available. *(none)* | Playing ping-pong with the server. |
| `getAvailableTools` | Returns all tools exposed by the VSAgent server. *(none)* | Getting all available tools. |
| `getSolutionInfo` | Gets information about the current solution. *(none)* | Retrieving solution information. |
| `getProjects` | Gets a list of all projects in the current solution SDK Style. *(none)* | Retrieving all projects. |
| `getRoslynProjects` | Gets a list of all Roslyn projects in the current solution. *(none)* | Getting project through Roslyn. |
| `getProjectDocuments` | Returns all source documents in a Roslyn project. *projectid (string)* | Listing project documents. |
| `getDiagnostics` | Returns compiler diagnostics for the currently loaded Visual Studio solution. *(none)* | Getting diagnostics. |
| `findSymbol` | Search by symbol name and returns matching classes, methods, properties, fields, etc. *SymbolName (string)* | Finding a symbol. |
| `findReferences` | Finds all source references to a symbol identified by its document and source position. *documentId (string), line (int), column (int)* | Finding references. |
| `getActiveDocument` | Returns information about the currently active document in Visual Studio, including caret and selection information. *(none)* | Getting the active document. |
| `readDocument` | Reads the content of a document and returns it as a string. *documentId (string), filePath (string)* | Reading the document source-code. |
| `applyDocumentEdit` | Applies a source-code edit directly to a document. *documentId (string), filePath (string), oldText (string), newText (string)* | Applying source-code change. |
| `addDocument` | Creates a new source document in a Visual Studio project. *projectid (string), name (string), text (string), folders (array, optional)* | Adding a new document to the project. |
| `removeDocument` | Removes a source document in a Visual Studio project. *projectid (string), documentname (string)* | Removing a document from the project. |
| `buildSolution` | Builds the currently loaded Visual Studio solution and returns whether the build succeeded. *(none)* | Building the solution. |
| `buildProject` | Builds the given project and returns whether the build succeeded. *ProjectId (string)* | Building the project. |

### VSAgent.AgentHost

| Property | Value |
|---|---|
| Language | VB.NET |
| Target Framework | .NET 10.0 |
| Output Path | `bin\Debug
et10.0\VSAgent.AgentHost.dll` |
| Document Count | 12 (including generated) |
| Project References | 2 |
| Metadata References | 168 |

**Key Classes:**

| Class | Purpose |
|---|---|
| `OllamaClient` | HTTP client for Ollama's chat API. Sends streaming requests with messages, tools, and tool choices. Handles SSE-like response stream parsing (split into thinking and content buffers). Raises events for thinking, content, and statistics. Configured with model name and base URL. |
| `OllamaAssistantResponse` | Captures LLM response: thinking text, content text, tool calls, and session statistics. |
| `OllamaToolCall` | Represents a single tool call from the LLM (id, name, arguments). |
| `OllamaSessionStatistics` | Tracks tokens used during an Ollama session. |
| `AgentRunner` | Core agent loop: accepts user prompts, sends them to Ollama with registered tools, processes returned tool calls by sending them via the VS server, feeds results back to the LLM in a do-while loop until no more tool calls are returned. Supports interrupt/cancellation. |
| `VSAgentPipeClient` | Wrapper around `TransportPipeClient<AgentRequest, AgentResponse>` for interacting with the VSAgent server. Provides methods like `ConnectAsync`, `GetAvailableToolsAsync`, and `CallToolAsync`. |
| `AgentHostPipeServer` | Named pipe server on the AgentHost side (pipe name: `"VSAgent.AgentHost"`) to allow other processes to communicate with the agent host itself. |
| `AgentHelpers` | Helper utilities for the agent host. |
| `Program` (Module) | CLI entry point. Default model: **qwen3.6:35b** at `http://localhost:11434/`. Creates a system prompt, starts the agent loop with colored console output distinguishing thinking (dark gray), content (cyan), and tool events (yellow/red). Supports `/exit` or `/quit` commands. |

### VSAgent.Extension

| Property | Value |
|---|---|
| Language | VB.NET |
| Target Framework | .NET Framework 4.7.2 |
| Output Path | `bin\Debug
et472\VSAgent.Extension.dll` |
| Document Count | 19 (including generated) |
| Project References | 5 |
| Metadata References | 150 |

**Package & UI:**

| Class | Purpose |
|---|---|
| `AboutCommandPackage` | VS Package (VSIX manifest) entry point. Initializes the extension. |
| `VSAgentToolWindow` / `VSAgentToolWindowControl` / `VSAgentToolWindowCommand` | Tool Window dialog that appears in Visual Studio for interacting with VSAgent. Uses WPF-based control (`.xaml`). |
| `AboutCommand` | Command handler showing an info message box when invoked. |

**VS Service Implementations:**

| Class | Implements | Purpose |
|---|---|---|
| `VisualStudioRoslynWorkspaceService` | `IRoslynWorkspaceService` | Gets projects/documents from VS Roslyn workspace via `RoslynWorkspaceProvider`. |
| `VisualStudioSolutionService` | `ISolutionService` | Gets solution info and SDK-style project list from VS. |
| `VisualStudioDocumentService` | `IDocumentService` | Reads active document and content from VS text manager. |
| `VisualStudioDocumentEditService` | `IDocumentEditService` | Applies edits, adds/removes documents in VS editor via Roslyn APIs. |
| `VisualStudioBuildService` | `IBuildService` | Invokes MSBuild/VS build for solution or individual projects. |
| `VisualStudioDiagnosticsService` | `IRoslynDiagnosticsService` | Retrieves compiler diagnostics from VS. |
| `VisualStudioFindSymbolsService` | `ISymbolService` | Finds symbols by name using Roslyn search APIs. |

**Other Extension Classes:**

| Class | Purpose |
|---|---|
| `RoslynWorkspaceProvider` | Thread-safe provider of the active Visual Studio `Microsoft.CodeAnalysis.Workspace`. |
| `AgentHostController` | Manages the lifecycle of the `VSAgent.AgentHost.exe` process (start/stop/check running status). Locates it in the extension's `AgentHost/` subdirectory. |
| `AgentHostClient` | Client for communicating with a remote AgentHost instance. |

---

## Available Tools (Complete List)

Below is the comprehensive inventory of all 16 tools exposed by the VSAgent server:

### Utility & Discovery

| # | Name | Parameters | Description |
|---|---|---|---|
| 1 | `ping` | — | Health check — returns "pong" |
| 2 | `getAvailableTools` | — | Lists all registered tools with their schemas |

### Solution & Project Management

| # | Name | Parameters | Description |
|---|---|---|---|
| 3 | `getSolutionInfo` | — | Returns solution name, file path, and directory |
| 4 | `getProjects` | — | Lists all SDK-style projects in the solution |
| 5 | `getRoslynProjects` | — | Lists all Roslyn projects with full metadata (ID, assembly name, language, doc count, reference counts) |
| 6 | `getProjectDocuments` | `projectid: string` | Lists all source documents within a project (IDs, names, paths, folders) |

### Symbol & Diagnostics

| # | Name | Parameters | Description |
|---|---|---|---|
| 7 | `findSymbol` | `SymbolName: string` | Search for symbols by name across the solution |
| 8 | `findReferences` | `documentId: string, line: int, column: int` | Find all references to a symbol at a specific location |
| 9 | `getDiagnostics` | — | Returns all compiler diagnostics (errors + warnings) for the solution |

### Document Operations

| # | Name | Parameters | Description |
|---|---|---|---|
| 10 | `getActiveDocument` | — | Returns info about the currently active editor document (caret, selection) |
| 11 | `readDocument` | `documentId: string, filePath: string` | Reads and returns the full content of a source file |
| 12 | `applyDocumentEdit` | `documentId: string, filePath: string, oldText: string, newText: string` | Performs a text replacement in a source file (requires exact match for oldText) |
| 13 | `addDocument` | `projectid: string, name: string, text: string, folders: string[] (opt)` | Creates a new source file within a project |
| 14 | `removeDocument` | `projectid: string, documentname: string` | Deletes a source file that was created by the agent from a project (⚠️ irreversible) |

### Build Operations

| # | Name | Parameters | Description |
|---|---|---|---|
| 15 | `buildSolution` | — | Builds the entire solution and returns success/failure |
| 16 | `buildProject` | `ProjectId: string` | Builds a specific project by ID and returns success/failure |

---

## Communication Protocol

### Transport Mechanism

- **Named Pipes** on Windows via `System.IO.Pipes`
- Pipe names: `"VSAgent"` (server), `"VSAgent.AgentHost"` (agent host server)
- Line-delimited JSON over byte streams

### Message Format

```json
{
  "MessageType": "request",     // "request" | "response" | "event"
  "RequestId": "guid-here",     // Correlation ID for request/response matching
  "Payload": { ... }             // Serialized tool-specific payload
}
```

### Request Flow (Tool Invocation)

1. AgentHost sends an `AgentRequest` wrapped in a `TransportMessage`:
   ```json
   {
     "MessageType": "request",
     "RequestId": "550e8400-e29b-41d4-a716-446655440000",
     "Payload": {
       "Id": "550e8400-e29b-41d4-a716-446655440000",
       "Tool": "buildSolution",
       "Parameters": {}
     }
   }
   ```

2. Server looks up the tool in `ToolRegistry`, calls `ExecuteAsync(request)`

3. Server sends back:
   ```json
   {
     "MessageType": "response",
     "RequestId": "550e8400-e29b-41d4-a716-446655440000",
     "Payload": {
       "Success": true,
       "Version": 1,
       "Result": { "succeeded": true }
     }
   }
   ```

### Response Class Format

**Successful:** `AgentResponse.Ok(requestId, version, result)`
```json
{ "success": true, "version": 1, "result": { ... }, "errorMessage": null }
```

**Failed:** `AgentResponse.Failed(requestId, version, errorMessage)`
```json
{ "success": false, "version": 1, "result": null, "errorMessage": "Error details..." }
```

---

## Build & Target Frameworks

| Project | Target Framework | Configuration | Output File |
|---|---|---|---|
| VSAgent.Abstractions | .NET Framework 4.7.2 | Debug/Release | `bin\Debug\VSAgent.Abstractions.dll` |
| VSAgent.Protocol | .NET Standard 2.0 | Debug/Release | `bin\Debug
etstandard2.0\VSAgent.Protocol.dll` |
| VSAgent.Transport | .NET Standard 2.0 | Debug/Release | `bin\Debug
etstandard2.0\VSAgent.Transport.dll` |
| VSAgent.Server | .NET Framework 4.7.2 | Debug/Release | `bin\Debug\VSAgent.Server.dll` |
| VSAgent.Tools | .NET Framework 4.7.2 | Debug/Release | `bin\Debug\VSAgent.Tools.dll` |
| VSAgent.AgentHost | **.NET 10.0** (SDK-style) | Debug/Release | `bin\Debug
et10.0\VSAgent.AgentHost.dll` |
| VSAgent.Extension | .NET Framework 4.7.2 | Debug/Release | `bin\Debug
et472\VSAgent.Extension.dll` |

---

## Dependencies

### External Libraries

| Library | Purpose | Used By |
|---|---|---|
| **Newtonsoft.Json** | JSON serialization/deserialization | Protocol, Transport, Tools, AgentHost |
| **Microsoft.CodeAnalysis** | Roslyn API access for workspace, projects, documents, symbols, diagnostics | Extension (via VisualStudio*Service classes) |
| **Microsoft.VisualStudio.Shell** | VS Extensibility APIs (AsyncPackage, ToolWindow, etc.) | VSAgent.Extension |
| **Microsoft.Omnisharp...** | *(referenced in project metadata)* | Multiple projects |

### Internal Dependencies Graph

```
VSAgent.Abstractions          ← (base interfaces)
VSAgent.Protocol              ← VSAgent.Abstractions
VSAgent.Transport             ← VSAgent.Protocol, Newtonsoft.Json
VSAgent.Tools                 ← VSAgent.Abstractions, VSAgent.Protocol
VSAgent.Server                ← VSAgent.Tools, VSAgent.Transport, VSAgent.Protocol
VSAgent.Extension             ← VSAgent.Abstractions, VSAgent.Protocol, Microsoft.CodeAnalysis, Microsoft.VisualStudio.Shell
VSAgent.AgentHost  ──→       ← VSAgent.Protocol, VSAgent.Transport (via VSAgent.Server or direct pipe client)
```

---

## Key Workflows

### Tool Execution Flow

```
LLM decides to call tool "buildSolution"
    │
    ▼
AgentRunner.BuildOpenAITools() → formats tool schemas for Ollama
    │
    ▼
OllamaClient.SendAsync(messages, tools) → streaming chat API
    │
    ▼
Ollama returns tool_calls: [{ id, name: "buildSolution", arguments: {} }]
    │
    ▼
AgentRunner.ExecuteToolCallAsync(toolCall)
    │
    ├──→ VSAgentPipeClient.CallToolAsync("buildSolution", {})
    │       │
    │       ▼
    │   TransportPipeClient.Send(AgentRequest{ Tool: "buildSolution" })
    │       │
    │       ▼ (Named Pipe IPC)
    │
    ▼
AgentPipeServer.HandleAgentRequestAsync(request)
    │
    ├──→ _toolRegistry.GetTool("buildSolution") → BuildSolutionTool instance
    │
    ▼
BuildSolutionTool.ExecuteAsync(request)
    │
    ├──→ _buildService.BuildSolutionAsync()
    │       │
    │       ▼ (VS Build API)
    │
    ▼
Returns AgentResponse{ Success: true, Result: BuildResult }
    │
    ▼ (Named Pipe IPC)
    │
    ▼
VSAgentPipeClient receives response → completion.SetResult(response)
    │
    ▼
AgentRunner feeds result back into messages as tool role
    │
    ▼
Loop continues (calls Ollama again with updated conversation)
```

### LLM Agent Loop (AgentHost Main)

1. **Initialize**: Connect to VSAgent named pipe, fetch tool descriptors, create `OllamaClient`, create `AgentRunner`
2. **Read user prompt** from console
3. **Add prompt** as user message in conversation history
4. **Send to Ollama** with tools and system prompt
5. **Parse response**:
   - No tool calls → return content, end loop
   - Has tool calls → for each call, send to VSAgent server via pipe, collect result
6. **Add assistant message + tool results** to conversation history
7. **Loop back to step 4** until no more tool calls
8. **Exit**: `/exit` or `/quit`

---

## Getting Started

### Prerequisites

1. **Visual Studio** (2022/2026 recommended) with the VB.NET workload
2. **.NET 10 SDK** (for AgentHost compilation and runtime)
3. **Ollama** installed locally, running on `http://localhost:11434/`
4. A model pulled in Ollama (e.g., `ollama pull qwen3.6:35b`)

### Building the Solution

Open `VSAgent.slnx` in Visual Studio and build the solution, or use `dotnet build` from the root directory.

```bash
cd <folder>\VSAgent
dotnet build VSAgent.slnx
```

### Running

1. **Launch Visual Studio** with the VSAgent extension loaded (the VSIX package will start automatically)
2. The extension's `AgentHostController` will launch `VSAgent.AgentHost.exe` as a child process, this happens when the tollwindow is opened.
3. The AgentHost connects to the server on named pipe `"VSAgent"` and waits for input
4. **In the toolwindow** type prompts (e.g., "Build the solution", "Find references to Main in Program.vb")
5. The agent will use Ollama to decide which tools to call and act on your behalf

### Configuration

The default Ollama settings are hardcoded in `Program.Main()` for now, this will change in future iterations:

```vb
Private Const model = "qwen3.6:35b"
Private Const base_url = "http://localhost:11434/"
Private Const APIKey = "ollama"
```

---

## File Statistics

| Metric | Count |
|---|---|
| **Total Projects** | 7 |
| **Total Source Documents** (excluding generated/obj) | ~105 |
| **Total Lines of Code** (approx.) | ~8,000+ |
| **Languages** | VB.NET only |
| **Tools Implemented** | 16 |
| **Service Interfaces** | 9 |
| **DTO Classes** | 30+ |
| **Named Pipes Used** | 2 (`"VSAgent"`, `"VSAgent.AgentHost"`) |

---

## License

See [LICENSE](LICENSE) for licensing information.
