# Contributing to HowToSoftware

Thank you for your interest in contributing! **HowToSoftware** is a from-scratch
**ASP.NET Core 10** reimplementation of the Ghost publishing platform, running on
SQL Server 2025. This document explains how to set up your environment and the
workflow we follow. For the big picture, start with the [README](README.md) and
[docs/CLONE-ARCHITECTURE.md](docs/CLONE-ARCHITECTURE.md).

## Code of Conduct

This project and everyone participating in it is governed by our
[Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to
uphold it. Report unacceptable behavior to
[henry.cahill@howtoosoftware.com](mailto:henry.cahill@howtoosoftware.com).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server 2025 — a local instance, LocalDB, or the SQL Server container
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — required to
  run the full stack and for the integration tests, which use Testcontainers
- Node.js 20+ — optional, only for rebuilding front-end/theme assets with Vite

## Getting Started

1. Fork the repository and clone your fork:
   ```powershell
   git clone <your-fork-url> howtoosoftware-rework
   cd howtoosoftware-rework
   ```
2. Restore and build the solution:
   ```powershell
   dotnet build
   ```
3. Store local secrets with **User Secrets** — never commit connection strings or
   provider keys:
   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=HowToSoftware;Trusted_Connection=True;TrustServerCertificate=True;" --project src/HowToSoftware.Web
   ```
4. Run an app locally:
   ```powershell
   dotnet run --project src/HowToSoftware.Web     # public site → http://localhost:5032
   dotnet run --project src/HowToSoftware.Admin   # admin panel → http://localhost:5105
   ```

See the [Quick Start](README.md#quick-start) in the README for database creation,
Docker Compose, and Ghost migration details.

## Project Layout

| Project | Responsibility |
|---|---|
| [HowToSoftware.Core](src/HowToSoftware.Core) | Domain entities, service interfaces, and framework-free logic |
| [HowToSoftware.Infrastructure](src/HowToSoftware.Infrastructure) | EF Core, repositories, external integrations, and DI wiring |
| [HowToSoftware.Web](src/HowToSoftware.Web) | Public website — Razor Pages + Content API |
| [HowToSoftware.Admin](src/HowToSoftware.Admin) | Blazor Server admin panel |
| [HowToSoftware.Migrator](src/HowToSoftware.Migrator) | Ghost MySQL → SQL Server migration CLI |

Each source project has a matching xUnit project under `tests/`.

## How to Contribute

### Reporting Bugs

- Search existing issues first to avoid duplicates.
- Open a GitHub issue with steps to reproduce, expected vs. actual behavior, and
  environment details (OS, .NET SDK version, database, Docker).
- For anything security-related, **do not** open a public issue — follow the
  [Security Policy](SECURITY.md) instead.

### Suggesting Enhancements

- Open an issue describing the problem and your proposed solution.
- Explain the use case, who benefits, and how it fits the Ghost feature-parity
  goal (see the phase tracker in [BUILD.md](BUILD.md)).

### Pull Requests

1. Create a branch from `main` (see conventions below).
2. Follow the coding standards below and add or update tests for your change.
3. Update relevant documentation (for example, the [README](README.md) or the
   [BUILD.md](BUILD.md) task tracker).
4. Ensure the solution builds warning-free in Release and all tests pass:
   ```powershell
   dotnet build -c Release
   dotnet test
   ```
5. Open a PR with a clear title and description, and link related issues.

## Coding Standards

- **Formatting is enforced by [.editorconfig](.editorconfig)** — 4-space indent,
  CRLF line endings, UTF-8, final newline. Run `dotnet format` before committing.
- Framework and language are pinned in
  [Directory.Build.props](Directory.Build.props): **.NET 10 / C# 14**, with
  nullable reference types and implicit usings enabled. Package versions are
  managed centrally in `Directory.Packages.props` — add new packages there.
- **Warnings are treated as errors in Release builds**, so keep the tree clean.
- Respect the layering: `Core` stays free of framework/infrastructure
  dependencies; persistence and external integrations live in `Infrastructure`
  and are exposed through interfaces declared in `Core`.
- Use parameterized queries (EF Core) and sanitize any rendered HTML — see the
  Security section of the README for the OWASP considerations.
- **AI / LLM features target Ollama by default.** Use the configured
  `OllamaSettings` / `IAiChatService` pattern instead of hard-coding endpoints or
  adding other providers without approval — see
  [docs/OLLAMA_SETUP.md](docs/OLLAMA_SETUP.md).

## Branch & Commit Conventions

- **Branches:** `feature/<short-name>`, `fix/<short-name>`, `docs/<short-name>`,
  branched from `main`.
- **Commits:** Follow [Conventional Commits](https://www.conventionalcommits.org/):
  `type(scope): summary` — e.g. `feat(auth): add magic-link expiry`.

## Review Process

- At least one approving review is required before merge.
- Address review comments or explain why a suggestion doesn't apply.
- Keep PRs focused and reasonably small for faster review.

## Questions?

Open a GitHub discussion or issue, or reach out to the maintainers at
[henry.cahill@howtoosoftware.com](mailto:henry.cahill@howtoosoftware.com).
