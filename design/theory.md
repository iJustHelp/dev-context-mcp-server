
### Agent Loop

#### Flowchart

```mermaid
flowchart TD
    Start([Start]) --> UserPrompt[User provides prompt]

    UserPrompt --> Agent[Agent receives prompt]

    Agent --> LoadContext[Load  instructions and initial context]

    LoadContext --> SendToLLM[Send to LLM:<br/>prompt + instructions + more context]

subgraph Loop["Agent Loop"]
    SendToLLM --> LLMThink[LLM reasons:<br/>what to do next]

    LLMThink --> NeedQuestion{Need more info<br/>from user?}

    NeedQuestion -- Yes --> AskUser[Agent asks user question]
    AskUser --> UserAnswer[User answers]
    UserAnswer --> SendToLLM

    NeedQuestion -- No --> NeedTools{Need tools or scripts?}

    NeedTools -- Yes --> RunTools[Agent runs tools:<br/>bash scripts, read files, MCP tools, build/tests]
    RunTools --> ToolOutput[Tools return new context:<br/>file content, docs, errors, test output]

    ToolOutput --> UpdateContext[Agent adds new context]
    UpdateContext --> SendToLLM

    NeedTools -- No --> DoneCheck{Task complete?}

    DoneCheck -- No --> LLMThink

end

    DoneCheck -- Yes --> FinalResult[Agent returns result:<br/>plan, code, explanation, or diff]

    FinalResult --> End([End])
```


#### Sequence

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

    LLM->>User: Question
    User-->>LLM: Answer
    

    Agent->>Tools: Do what LLM said
    Tools-->>Agent: More context

    Agent->>LLM: Prompt + instructions + all useful context
    LLM-->>Agent: Done

    Agent-->>User: Plan or Plan -> code
```
>Good LLM knows to ask, good Agent knows to pass.

Agent and LLM are working together like 
- GitHub Copilot + Sonnet 4.6
- Codex + GPT 5.5
- Claude + Opus 4.8  




---

### Dev Context MCP Server Solution

```mermaid
flowchart TD
    U[User Prompt] --> AGENT[Agent]

    AGENT --> LLM[LLM]

    AGENT --> MCP[Dev Context MCP Server]

    DB[(SQLite / FTS Index)]

    subgraph Internal
        MD[Company Docs]
        PKG[Internal NuGet Packages]
    end

    MCP --> DB
    DB --> MD
    DB --> PKG

    LLM --> AGENT
    MCP --> AGENT
    LLM --> RESULT[Plan or Code]
```