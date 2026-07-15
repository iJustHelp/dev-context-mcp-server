---
type: Reference
title: Coding Standards (Agent Skills)
description: The repo-authored agent skills that enforce company architecture, naming, and testing conventions.
resource: .claude/skills
tags: [standards, skills, conventions, csharp]
timestamp: 2026-07-15T00:00:00Z
---

# Coding Standards (Agent Skills)

Company coding standards are delivered as project agent skills (under
`.claude/skills/` and mirrored in `.agents/skills/`), not through the MCP index.
Most vendored skills are third-party; the four repo-authored, company-standard
skills are below. See [Operations](operations.md) for the surrounding conventions.

# Schema

| Skill | Enforces |
|-------|----------|
| **api-architecture** | STI API project structure, dependency direction, and service/repository patterns (`STI.{ApiName}` projects, controllers, services, repositories). Use when creating or refactoring API solutions or STI projects. |
| **csharp-naming** | Company C# naming conventions, member ordering, and constructor style. Applied when generating or refactoring C# classes, methods, properties, fields, and async members. |
| **unit-test-generation** | Mandatory xUnit + Moq test structure, naming, templates, and verification rules — including `Formula.SimpleRepo` repository mocks. Use when generating or extending C# unit tests. |
| **dev-context** | Directs the agent to research internal NuGet packages and uncertain .NET APIs via the live `dev_context` MCP server before implementing or reviewing dependent code. |

Identical copies exist under `.agents/skills/`. Third-party vendored skills are
tracked in `skills-lock.json`.

# Citations

[1] [.claude/skills/api-architecture/SKILL.md](../.claude/skills/api-architecture/SKILL.md)
[2] [.claude/skills/csharp-naming/SKILL.md](../.claude/skills/csharp-naming/SKILL.md)
[3] [.claude/skills/unit-test-generation/SKILL.md](../.claude/skills/unit-test-generation/SKILL.md)
[4] [.claude/skills/dev-context/SKILL.md](../.claude/skills/dev-context/SKILL.md)
