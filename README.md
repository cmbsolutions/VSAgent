[![wakatime](https://wakatime.com/badge/user/764142e6-ba3c-4be3-9aca-2093a1befa2d/project/87827ab1-d843-48a0-a6ff-b84036ad9fb2.svg)](https://wakatime.com/badge/user/764142e6-ba3c-4be3-9aca-2093a1befa2d/project/87827ab1-d843-48a0-a6ff-b84036ad9fb2)

# VSAgent

A Visual Studio IDE coding agent.

## Projects

| Project | Description |
|---------|-------------|
| **[VSAgent.Abstractions](VSAgent.Abstractions/)** | Shared interfaces for the agent system, including `IAgentServer`, `IDocumentService`, `ILog`, `IRoslynWorkspaceService`, `ISolutionService`, `ITool`, and `IToolRegistry`. |
| **[VSAgent.Extension](VSAgent.Extension/)** | Visual Studio extension (VSIX) that hosts the agent, implements document/roslyn/solution services, and provides the `About` command. |
| **[VSAgent.Protocol](VSAgent.Protocol/)** | Data models and contracts used for communication between the extension and server — `AgentRequest`, `AgentResponse`, `ToolDescriptor`, document/project/solution info types. |
| **[VSAgent.Server](VSAgent.Server/)** | Pipe-based server (`AgentPipeServer`) that receives requests from the extension and orchestrates agent execution. |
| **[VSAgent.TestClient](VSAgent.TestClient/)** | Console client for testing and interacting with the agent pipe server during development. |
| **[VSAgent.Tools](VSAgent.Tools/)** | Built-in tools available to the agent, including `GetActiveDocument`, `GetAvailableTools`, `GetProjects`, `GetRoslynProjects`, `GetSolutionInfo`, `Ping`, and `ReadDocument`. |

## Prerequisites

- **Visual Studio 2022** (17.14+) or **Visual Studio 2025** (18.0+) — Community, Professional, or Enterprise
- **.NET Framework 4.5+**
- [Visual Studio Extension Development workload](https://learn.microsoft.com/visualstudio/extensibility/extension-overview?view=vs-2022#installing-the-tools)

## Building

Open `VSAgent.slnx` in Visual Studio and build the solution, or run:

```bash
dotnet build VSAgent.slnx
```

Build each project individually if needed:

```bash
dotnet build VSAgent.Abstractions\VSAgent.Abstractions.vbproj
dotnet build VSAgent.Protocol\VSAgent.Protocol.vbproj
dotnet build VSAgent.Server\VSAgent.Server.vbproj
dotnet build VSAgent.Tools\VSAgent.Tools.vbproj
dotnet build VSAgent.Extension\VSAgent.Extension.vbproj
```

## Installing the Extension

After building, install the VSIX from:

```
VSAgent.Extension\bin\Debug\VSAgent.Extension.vsix
```

- In Visual Studio: **Tools → Install VSIX Extension…**, or double-click the `.vsix` file.
- The extension targets VS 2022 (17.14+) and VS 2025 (18.0+).

## How It Works

```
[External Agent / Client]
        │
        │  Named pipe: "VSAgent" (JSON over lines)
        ▼
[VSAgent.Extension] ──► Visual Studio services (Roslyn, Solution, Documents)
        │
        ▼
[VSAgent.Tools] ────► Tool implementations (execute on behalf of the agent)
```

1. **VSAgent.Extension** launches inside Visual Studio when you open a solution. It hosts **VSAgent.Server**, which listens on a named pipe called `"VSAgent"`.
2. Any external process (an LLM agent, script, or test client) connects to that pipe and sends JSON `AgentRequest` messages.
3. The server dispatches each request to the corresponding **ITool** implementation in **VSAgent.Tools**, which uses Visual Studio's Roslyn and DTE APIs to perform actions — read files, list projects, get solution info, etc.
4. Results are returned as JSON `AgentResponse` messages back through the same pipe connection.

## Using the Agent

### Via Named Pipe (External Client)

Any process can connect to `"VSAgent"` and send requests in this format:

```json
{
  "id": "unique-guid",
  "tool": "getAvailableTools",
  "parameters": {}
}
```

Responses arrive line-by-line on the same pipe:

```json
{
  "result": [...],
  "error": null,
  "statusCode": 0
}
```

### Available Tools

| Tool | Description |
|------|-------------|
| `getAvailableTools` | Lists all registered tools and their parameter schemas. |
| `getActiveDocument` | Returns info about the currently active editor document. |
| `getRoslynProjects` | Returns project information via Roslyn. |
| `getProjects` | Returns project information via DTE. |
| `getSolutionInfo` | Returns the current solution's metadata. |
| `readDocument` | Reads the contents of a file by path or document ID. |
| `ping` | Ping/health-check tool — returns `pong`. |

### Testing with VSAgent.TestClient

Run **VSAgent.TestClient** (after installing the extension and opening a solution in Visual Studio):

```bash
dotnet run --project VSAgent.TestClient\VSAgent.TestClient.vbproj
```

It connects to the `"VSAgent"` pipe, retrieves the list of available tools, and exercises each one — useful for verifying that the extension is loaded and tools are wired correctly.

## Architecture Notes

- **Protocol** uses JSON lines over a bi-directional `NamedPipeServerStream`. Each request/response pair is one line.
- **Tools** execute synchronously on the extension's thread but use `async/await` to avoid blocking Visual Studio's UI.
- **VSAgent.Server** accepts one client at a time per pipe instance and handles requests in-order until the client disconnects.

## License

See [LICENSE](LICENSE) for licensing information.
