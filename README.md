# 🏗️ Library.Jewelix.Infrastructure

[![CI / CD](https://github.com/Achi054/Library.Jewelix.Infrastructure/actions/workflows/cicd.yml/badge.svg)](https://github.com/Achi054/Library.Jewelix.Infrastructure/actions/workflows/cicd.yml)
[![Latest Release](https://img.shields.io/github/v/release/Achi054/Library.Jewelix.Infrastructure?label=Jewelix.Logging&color=blue&logo=nuget)](https://github.com/Achi054/Library.Jewelix.Infrastructure/pkgs/nuget/Jewelix.Logging)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> Cross-cutting infrastructure library for the **Jewelix** ecosystem. Currently ships the `Jewelix.Logging` package — a production-ready Serilog integration with request/response middleware, correlation IDs, and a Microsoft.Extensions.Logging adapter.

---

## 📦 Packages

| Package | Description |
|---|---|
| `Jewelix.Logging` | Serilog-backed logger adapter, HTTP middleware, and DI registration helpers |

---

## 🚀 Quick Start

### 1️⃣ Register the logger

```csharp
// Program.cs
builder.Services.AddJewelixLogger();
```

### 2️⃣ Wire up the middleware

```csharp
// Program.cs — after Build()
app.UseJewelixLogger(app.Configuration);
```

### 3️⃣ Configure Serilog in `appsettings.json`

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] [{RequestId}] [{CorrelationId}] [{ElapsedMs}ms] HTTP {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

> 💡 **Console sink** is automatically configured by `UseJewelixLogger` with the standard output template — no additional `WriteTo.Console` entry is needed in `appsettings.json`. For additional sinks (File, Seq, etc.) add them under `Serilog:WriteTo` and use `LoggerExtensions.OutputTemplate` as the `outputTemplate` value to keep all sinks consistent.

---

## 📋 Log Format

Every HTTP request produces a structured log event in this format:

```
[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] [{RequestId}] [{CorrelationId}] [{ElapsedMs}ms] HTTP {Message:lj}{NewLine}{Exception}
```

**Example output:**

```
[2025-05-03 14:22:01] [INF] [0HMXYZ:00000001] [a1b2c3d4-...] [42ms] HTTP GET /api/orders responded 200
```

| Token | Description |
|---|---|
| `{Timestamp:yyyy-MM-dd HH:mm:ss}` | UTC timestamp of the log event |
| `{Level:u3}` | Three-letter uppercase level (`INF`, `WRN`, `ERR`, …) |
| `{RequestId}` | ASP.NET Core `TraceIdentifier` for the current request |
| `{CorrelationId}` | Value of `X-Correlation-ID` header (generated if absent) |
| `{ElapsedMs}` | Total request processing time in milliseconds |
| `{Message:lj}` | Structured log message in literal JSON format |

> ⚠️ `{ElapsedMs}` is pushed onto `LogContext` only for the HTTP summary event. Other log events (e.g. Debug body captures) will render an empty value for that token — this is by design.

---

## 🔍 Features

### 🪪 Correlation IDs
- Reads `X-Correlation-ID` from incoming requests; generates a new GUID if absent.
- Echoes the ID back on the response header.
- Stamps every log event in the request scope with `CorrelationId` and `RequestId` properties.

### 🛡️ Sensitive Field Masking
Request and response bodies logged at `Debug` level are sanitised before writing. The following JSON field names are masked (case-insensitive):

`password` · `token` · `secret` · `creditCard` · `apiKey`

**Before:** `{"password":"hunter2","username":"alice"}`  
**After:** `{"password":"***MASKED***","username":"alice"}`

Bodies larger than **4 000 characters** are truncated and marked `...(truncated)`.

### ⏱️ Elapsed Time
`ElapsedMs` is measured from the moment the middleware receives the request to the moment the response is fully written. It is pushed onto `LogContext` and referenced by `{ElapsedMs}` in the output template.

### 📝 Body Capture (Debug)
When the configured minimum level is `Debug` or lower:
- Request body is read, sanitised, and logged as `RequestBody={Body}`.
- Response body is captured, sanitised, and logged as `ResponseBody={Body}`.
- Original streams are restored transparently; callers receive their response unchanged.

---

## 🚦 CI / CD

The pipeline lives in [`.github/workflows/cicd.yml`](.github/workflows/cicd.yml) and runs two jobs:

| Job | Trigger | Steps |
|---|---|---|
| **Build & Test** | Every push to `main` and every PR | restore → build → test → upload TRX results |
| **Pack & Publish** | Push of a `v*.*.*` tag (e.g. `v1.2.3`) | restore → build → pack → push to GitHub Packages |

### 🏷️ Releasing a new version

1. Update `<Version>` in [`Directory.Build.props`](Directory.Build.props).
2. Commit and push to `main`.
3. Create and push a matching tag:
   ```bash
   git tag v1.2.3
   git push origin v1.2.3
   ```
4. The **Pack & Publish** job picks up the tag, extracts `1.2.3`, rebuilds, packs, and pushes `Jewelix.Logging 1.2.3` to [GitHub Packages](https://github.com/Achi054/Library.Jewelix.Infrastructure/pkgs/nuget/Jewelix.Logging).

### 📥 Consuming from GitHub Packages

Add the feed to your `nuget.config`:

```xml
<configuration>
  <packageSources>
    <add key="github-jewelix"
         value="https://nuget.pkg.github.com/Achi054/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github-jewelix>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_PAT" />
    </github-jewelix>
  </packageSourceCredentials>
</configuration>
```

Then reference the package normally:

```xml
<PackageReference Include="Jewelix.Logging" Version="1.0.0" />
```

---

## 🗂️ File Structure

```
Library.Jewelix.Infrastructure/
├── .github/
│   └── workflows/
│       └── cicd.yml                    # GitHub Actions CI/CD pipeline
├── src/
│   └── Jewelix.Logging/
│       ├── SerilogLogger.cs          # ILogger<T> → Serilog adapter
│       ├── LoggerExtension.cs        # AddJewelixLogger / UseJewelixLogger
│       ├── LoggerMiddleware.cs       # HTTP middleware + Sanitize helper
│       └── GlobalUsings.cs
├── test/
│   └── Jewelix.Logging.Tests/
│       ├── Helper/
│       │   ├── InMemorySink.cs       # Thread-safe Serilog sink for tests
│       │   └── SerilogTestCollection.cs  # xUnit collection (sequential execution)
│       ├── SerilogLoggerTests.cs
│       ├── DependencyInjectionTests.cs
│       ├── LoggerMiddlewareTests.cs
│       ├── SanitizeTests.cs
│       └── UseJewelixLoggerTests.cs
├── .editorconfig
├── .gitignore
├── Directory.Build.props
├── Directory.Build.targets
├── Library.Jewelix.Infrastructure.slnx
└── README.md
```

---

## 🧪 Running Tests

```bash
dotnet test
```

Tests use **xUnit** + **Shouldly** + **Microsoft.AspNetCore.TestHost** (in-process HTTP server — no network required). The `Serilog` xUnit collection enforces sequential execution to prevent races on the static `Log.Logger`.

---

## 📐 Tech Stack

| Concern | Library / Version |
|---|---|
| Logging abstraction | `Microsoft.Extensions.Logging` (via ASP.NET Core framework ref) |
| Logging implementation | `Serilog` 4.3.1 |
| ASP.NET Core integration | `Serilog.AspNetCore` 9.0.0 |
| Configuration binding | `Serilog.Settings.Configuration` 10.0.0 |
| Console sink | `Serilog.Sinks.Console` 6.0.0 |
| File sink | `Serilog.Sinks.File` 6.0.0 |
| Test framework | `xUnit` 2.9.2 |
| Assertion library | `Shouldly` 4.3.0 |
| Test host | `Microsoft.AspNetCore.TestHost` 10.0.0 |
