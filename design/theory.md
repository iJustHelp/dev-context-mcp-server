## AI-assisted Coding

```text
LLM(Context) = Plan
LLM(Context + Plan) = Code

where 
Context =
  Requirements +
  Existing code +
  Project structure +
  Framework/library versions +
  Documentation +
  Coding standards +
  Error messages +
  Test results +
  Previous conversation
```

How we can enrich Context?

### Agent Loop

#### Flowchart

```mermaid
flowchart TD

    UserPrompt --> Agent[Agent receives prompt]

    Agent --> LoadContext[Agent loads  instructions and initial context]

    LoadContext --> SendToLLM([Agent sends to LLM:<br/>prompt + instructions + <b>MORE</b> context])

subgraph Loop["Agent Loop"]
    SendToLLM --> LLMThink([LLM reasoning])

    LLMThink --> NeedQuestion{Need more info<br/>from user?}

    NeedQuestion -- Yes --> AskUser[Agent asks user question]
    AskUser --> UserAnswer[User answers]
    UserAnswer --> SendToLLM

    NeedQuestion -- No --> NeedTools{Need tools or scripts?}

    NeedTools -- Yes --> RunTools[Agent runs tools:<br/>bash scripts, read files, MCP tools, build/tests]
    RunTools --> ToolOutput[Tools return new context:<br/>file content, docs, errors, test output]

    ToolOutput --> UpdateContext[Agent adds new context]
    UpdateContext --> SendToLLM
end
    NeedTools -- No --> FinalResult[Plan or Code]

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

    Agent->>LLM: Prompt + instructions + more context
    LLM-->>Agent: What to do next

    LLM->>User: Question
    User-->>LLM: Answer
    

    Agent->>Tools:  Run scripts, call MCP tools etc.
    Tools-->>Agent: New context

    Agent->>LLM: Prompt + instructions + more context
    LLM-->>Agent: Done

    Agent-->>User: Plan or Code
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

    subgraph Loop[" "]
        AGENT --> |context| LLM[LLM]
        AGENT --> |request| MCP[Dev Context MCP Server]
    end
    
    DB[(SQLite / FTS Index)]

    subgraph Internal[" "]
        MD[Company Docs]
        PKG[NuGet Packages <br> qa, prod, public]
    end

    MCP --> DB
    DB --> MD
    DB --> PKG

    LLM --> |actions|AGENT
    MCP --> |context| AGENT
    LLM --> RESULT[Plan or Code]
```