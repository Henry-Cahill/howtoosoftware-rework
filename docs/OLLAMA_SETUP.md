# Ollama + LLM Setup for HowToSoftware

This document describes how to run **Ollama** with local LLMs and use them from **Cursor** (and optionally from the .NET app) in this environment.

---

## 1. Install Ollama (Windows)

1. Download the installer: [https://ollama.com/download](https://ollama.com/download) → **Windows**.
2. Run the installer. Ollama runs as a system service and exposes an API at **http://localhost:11434**.
3. Confirm it’s running: open [http://localhost:11434](http://localhost:11434) in a browser (you should see “Ollama is running”).

---

## 2. Pull a model

In PowerShell or Command Prompt:

```powershell
# General-purpose / chat (good for Cursor)
ollama pull llama3.2

# Code-focused (recommended for coding in Cursor)
ollama pull codellama
# or a smaller variant
ollama pull codellama:7b-instruct

# Other options
ollama pull mistral
ollama pull phi3
ollama list   # list installed models
```

Use the **exact model name** from `ollama list` when configuring Cursor (e.g. `codellama`, `llama3.2`, `mistral`).

---

## 3. Configure Cursor to use Ollama

Cursor can use Ollama via its **OpenAI-compatible** API.

1. **Start Ollama** (it should already be running after install).
2. In Cursor: **Settings** (gear icon) → **Cursor Settings** → **Models**.
3. Under **OpenAI API Key** (or “Add model” / “OpenAI-compatible”):
   - **API Key**: enter any non-empty value (e.g. `ollama`); Ollama does not validate it.
   - **Override OpenAI Base URL**: set to  
     `http://localhost:11434/v1`  
     (include `/v1`; Cursor expects the OpenAI-style path).
   - **Model**: set to the exact Ollama model name (e.g. `codellama`, `llama3.2`, `mistral`).
4. Save and choose this model in the model selector when chatting.

**If you get connection or TLS errors:**

- Go to **Cursor Settings → Network** and enable **HTTP Compatibility Mode** (HTTP/1.1).

**Optional (remote access):**  
If you need Cursor on another machine to use this Ollama instance, you can expose it with a tunnel (e.g. ngrok: `ngrok http 11434`) and set the base URL to the tunnel’s `/v1` URL. For local use, `http://localhost:11434/v1` is enough.

---

## 4. Optional: Use Ollama from the .NET app

To call Ollama from the HowToSoftware backend (e.g. for drafts, summaries, or future AI features):

1. **Config**  
   Add to `appsettings.Development.json` (or another environment):

   ```json
   "Ollama": {
     "BaseUrl": "http://localhost:11434",
     "Model": "llama3.2",
     "TimeoutSeconds": 60
   }
   ```

2. **API**  
   Ollama’s chat endpoint is OpenAI-compatible:

   - Chat: `POST {BaseUrl}/v1/chat/completions`  
   - Body shape: `{ "model": "<Model>", "messages": [ { "role": "user", "content": "..." } ] }`

   Use `HttpClient` or an OpenAI client pointed at `BaseUrl` (with path `/v1/chat/completions`) to call it from C#.

3. **Secrets**  
   Do not put production API keys in appsettings; use User Secrets or environment variables for any future external keys. For local Ollama, no key is required.

---

## 5. Quick reference

| Item            | Value                    |
|-----------------|--------------------------|
| Ollama API      | http://localhost:11434   |
| Cursor base URL | http://localhost:11434/v1 |
| Chat endpoint   | POST /v1/chat/completions |
| List models     | `ollama list`            |
| Pull model      | `ollama pull <name>`     |

---

## Reference

- [Ollama](https://ollama.com/) — run local LLMs.
- [Ollama API](https://github.com/ollama/ollama/blob/main/docs/api.md) — REST API (OpenAI-compatible).
- Cursor: **Settings → Models** for custom OpenAI-compatible endpoint and model name.
