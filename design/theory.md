
```mermaid
sequenceDiagram
    participant User
    participant Agent
    participant LLM
    participant Tools

    User->>Agent: Prompt

    Agent->>LLM: Prompt + instructions + initial context
    LLM-->>Agent: What to do next

    Agent->>Tools: Run scripts, call MCP tools etc.
    Tools-->>Agent: New context

    Agent->>LLM: Prompt + instructions + richer context
    LLM-->>Agent: What to do next

    Agent->>Tools: more actions
    Tools-->>Agent: More context

    Agent->>LLM: Prompt + instructions + all useful context
    LLM-->>Agent: Done

    Agent-->>User: Plan or code
```
---

```mermaid
flowchart TD
    U[User Prompt] --> AGENT[AI Agent]

    AGENT --> LLM[LLM]

    AGENT --> MCP[Dev Context MCP Server]

    subgraph Internal Knowledge
        DB[(SQLite / FTS Index)]
        XML[XML Documentation]
        MD[Markdown Docs]
        PKG[Internal NuGet Packages]
        EX[Code Examples]
    end

    MCP --> DB
    MCP --> XML
    MCP --> MD
    MCP --> PKG
    MCP --> EX

    LLM --> AGENT
    MCP --> AGENT

    AGENT --> CODE[Code Changes]
    CODE --> BUILD[Build / Tests]
    BUILD --> AGENT
    AGENT --> RESULT[Result]
```