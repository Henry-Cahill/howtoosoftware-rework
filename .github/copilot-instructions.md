# Ollama / local LLM — coding rules

This project's LLM backend is **Ollama**, served OpenAI-compatible at `http://localhost:11434/v1`. Human install/setup lives in [docs/OLLAMA_SETUP.md](../docs/OLLAMA_SETUP.md) — do not restate it. This file governs how code is written.

**Trigger:** when a task involves AI, an LLM, text generation, summarization, or embeddings, target Ollama by default. Ask before introducing any other provider.

## Configuration (source of truth)

- Config section `Ollama` in `appsettings*.json`. Keys: `BaseUrl`, `Model` (default `llama3.2`), `TimeoutSeconds`.
- Bind it the same way as `Mail`/`Stripe`: define an `OllamaSettings` class (mirroring `MailSettings`/`StripeSettings`) and register with `services.Configure<OllamaSettings>(configuration.GetSection("Ollama"))` inside `AddInfrastructure` in [src/HowToSoftware.Infrastructure/DependencyInjection.cs](../src/HowToSoftware.Infrastructure/DependencyInjection.cs).

## Structure (follow existing conventions)

- Declare interface `IAiChatService` in `HowToSoftware.Core/Interfaces` (`I`-prefixed, like every peer there).
- Put the implementation in `HowToSoftware.Infrastructure/Services` and register it `AddScoped` in `AddInfrastructure`.
- Use a typed `HttpClient` via `IHttpClientFactory`; set `Timeout` from `OllamaSettings.TimeoutSeconds`.
- Read `BaseUrl` from settings and append `/v1/chat/completions` in code. Request body: `{ "model": <Model>, "messages": [ { "role": "user", "content": "..." } ] }`.

## Never

- Never hard-code `http://localhost:11434`, the port, or a model name in `.cs` files — read them from `OllamaSettings`.
- Never add `OpenAI`, `Anthropic`, `Azure.AI.OpenAI`, or similar SDK packages without explicit approval.
- Never add an API key for Ollama (none is required). For any future keyed provider, use User Secrets or environment variables — never `appsettings.json`.
