Hi Josh,

Thank you for your response.

This app is just my experiment with agentic development. All code is on your box and only needs LAN access. It uses the simplest solution: Blazor uses server rendering with web sockets for UI component rendering. The app is server-only; page communication is handled only on the backend.
You can see it here: https://github.com/iJustHelp/have-fun/blob/main/design/architecture.md.
The only disadvantage I see is that the app has to keep the connection open.

Let me share the thoughts I've had over the past few months. Maybe writing is better than talking.  
Our development world has changed forever, and we have to adapt or retire.  
I guess the biggest issue is that developers have generated tons of code, and code is more structured than even English. LLMs handle it easily because it follows standard patterns.

Basically, agentic development is just  
Prompts + Agentic Harness + Context + LLM = plan --> code.

I have used Codex + GPT 5.5 and Claude + Opus 4.8 with identical results. Copilot is not as strong yet, but Microsoft will fix it soon. Anyway, they provide enough for development.

About the Context MCP server: how does the agent know about Blazor features?
It may be partly in the LLM, but some features are recent. The agent can look on the internet or in Microsoft's documentation, but it needs more tokens to find and understand the docs. The agent can use an context MCP server registered in a client like VS Code or the CLI. MCP server exposes tools with descriptions aka endpoints, and the agent knows what to call. It can also use skills to learn how and when to use the MCP server.

I have used the MCP server at https://context7.com/ for free for the game project. It has recent info, but it is public. How can I code using company internal docs, for example, by using a Smiley NuGet package like STI.Utilities or a NuGet package with OpenAPI clients? This is what dev-context-mcp-server tries to resolve.
Each NuGet package is deployed with readme.md and XML comments in the nupkg file. This info is used as context for the agent when the prompt mentions internal NuGet packages.
I would love to present it to you, Javier, and Brandon as an option. You can send me a 30-minute Teams meeting invitation for any time that is comfortable for you.

What changed for us? I think only developers who are close to the business are safe, like you, Javier, and James. Others just get requirements, work with an agent, review code, and do integration testing. This whole process is at least three times faster. I guess you will see it soon.
It means your current stories can be three times bigger with the same estimate, which makes development much cheaper. The good news is that we can rewrite tons of old code, which I hope will keep us in business.

My current situation is I don't have an offer. I am not considering W2 contracts, only corp-to-corp. Sometimes I get a lot of calls from Indian recruiters on the same day for the same opportunity. I think they are in India: I hear a lot of noise on the phone and weak English. I send them everything they want from me. Sometimes I get a call from their manager, maybe from the U.S. They submit my resume to the client and send me a connection request on LinkedIn. Done, they did their job. I don't get an interview, and they do not reply to my emails.

On the positive side, I had two interviews for an application architect position for full-time work. One of them was in-office twice, but they picked another candidate. As you can see, I am OK with full-time now, but I want to be closer to the business as an architect, with more design and less coding (see above).
