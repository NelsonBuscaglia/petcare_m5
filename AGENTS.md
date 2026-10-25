# AGENTS.md — project context

> **DAW template.** Fill in the `[...]` with what is true of YOUR project and delete what does not
> apply. This file describes **the project**; **the process** is DAW's job (phases, gates, when to
> test, when to commit). Do not mix the two: process rules written here compete with the pipeline's.
>
> It is **tool-agnostic on purpose**: Claude Code reads it through the import in `CLAUDE.md`, Codex
> CLI, Copilot CLI, Cursor and OpenCode read it directly, and Gemini CLI gets it through
> `GEMINI.md`. The same file serves whichever tool you open the repo with — which is the point:
> porting the pipeline to another tool must not mean rewriting what your project is.

---

## Language

**Always respond in the language the user writes in.** Write every artifact you produce — PRDs,
specs, ADRs, reports, commit messages, status lines — in that same language, regardless of the
language these instructions are written in.

If this project has a fixed working language, state it here and use it instead:

> Working language: `Spanish — write all artifacts in Spanish`

---

## What this project is

Web app mobile-first para que los integrantes de un hogar vean y registren el cuidado de sus mascotas
(paseo, comida, medicación, baño, veterinario), con alarmas por email ante tareas olvidadas o vencidas.

**Reference PRD:** `docs/daw/prd/PRD.md`

---

## Stack

| Field | Value |
|-------|-------|
| Language | C#, ASP.NET Core Razor Pages (sin Blazor), HTMX |
| Framework | .NET 10 LTS |
| Database | SQLite + EF Core 10 |
| Test runner | xUnit |
| Autenticación | por cookie + `PasswordHasher<T>` |
| Library | MailKit |

---

## Architecture conventions

- **Folder structure:** `src/features/<feature>/` with `ui`, `domain`, `data`
- **Layer separation:** the UI never talks to the database; always through a service
- **Error handling:** typed errors; never a silent catch
- **Naming:** files in kebab-case, components in PascalCase
- **Dependencies:** no new libraries without justifying them in the spec

---

## Code conventions

- No `any`. If it is unavoidable, it comes with a comment explaining why.
- Pure functions wherever possible; side effects at the edges.
- Comments only when the *why* is not obvious from the code.

---

## What NOT to do in this project

- No usar `DateTime.Now`/`UtcNow`: toda hora sale del `TimeProvider` inyectado, o no se pueden testear vencimientos y cambios de día.
- No tocar la base por fuera de las migraciones: nada de `EnsureCreated()`, `Migrate()` al arrancar ni borrar `petcare.db`. Los cambios de esquema van con `dotnet ef migrations add`.
- No mandar emails reales en desarrollo (se escriben en la consola) ni poner credenciales SMTP en `appsettings*.json`.

---

> ℹ️ **What does NOT belong in this file, because DAW provides it:** the order work happens in, when
> the spec gets written, when tests run, when to commit, what it takes to move between phases. All
> of that lives in `.daw/` and applies on its own.

<!-- BEGIN DAW (managed by DAW — do not edit by hand) -->
# DAW — Dilux Agentic Workflow

This repo uses **DAW**: an agent-driven development pipeline with the phases
`CLASSIFY → DEFINE → PLAN → CODE → VERIFY → RELEASE`.

Before answering, read `.daw/orchestrator.md` and run its Boot Sequence. It is a strict state
machine: it decides what you are allowed to do based on the phase recorded in `.daw-state.json`.

The project's own context — stack, architecture, domain — is elsewhere in this file. It lives here,
in `AGENTS.md`, and not in any one tool's file, on purpose: it is tool-agnostic and comes along
unchanged when the pipeline is ported to another agent.
<!-- END DAW -->

<!-- daw:declined FEAT-001 domain-glossary -->
