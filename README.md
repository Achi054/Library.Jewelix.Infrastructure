# 🏗️ Library.Jewelix.Infrastructure

[![CI / CD](https://github.com/Achi054/Library.Jewelix.Infrastructure/actions/workflows/cicd.yml/badge.svg)](https://github.com/Achi054/Library.Jewelix.Infrastructure/actions/workflows/cicd.yml)
[![Latest Release](https://img.shields.io/github/v/release/Achi054/Library.Jewelix.Infrastructure?label=Jewelix.Logging&color=blue&logo=nuget)](https://github.com/Achi054/Library.Jewelix.Infrastructure/pkgs/nuget/Jewelix.Logging)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

> Cross-cutting infrastructure library for the **Jewelix** ecosystem. Ships production-ready, opinionated packages that plug directly into the ASP.NET Core pipeline with zero boilerplate.

---

## 📦 Packages

| Package | Version | Target | Description |
|---|---|---|---|
| [`Jewelix.Logging`](src/Jewelix.Logging) | `1.0.0` | `net10.0` | Serilog-backed `ILogger<T>` adapter, HTTP request/response middleware, correlation-ID propagation, sensitive-field masking, and DI registration helpers |

> Additional packages (e.g. `Jewelix.Identity`, `Jewelix.Caching`) will be added as the ecosystem grows. Each package gets its own `Package.props` so versions are managed independently.

---

## 🚀 Quick Start — `Jewelix.Logging`

### 1️⃣ Add the NuGet reference

```xml
<PackageReference Include="Jewelix.Logging" Version="1.0.0" />
```

### 2️⃣ Register the logger (DI)

```csharp
// Program.cs
builder.Services.AddJewelixLogger();
```

`AddJewelixLogger` replaces any existing `ILogger<>` registration with the Serilog-backed `SerilogLogger<T>`, so the rest of your application keeps depending on `Microsoft.Extensions.Logging` unchanged.

### 3️⃣ Wire up the middleware

```csharp
// Program.cs — after Build()
app.UseJewelixLogger(app.Configuration);
```

`UseJewelixLogger` reads the `Serilog` section from `appsettings.json`, configures the static `Log.Logger`, and inserts `LoggerMiddleware` into the pipeline.

### 4️⃣ Configure Serilog in `appsettings.json`

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
          "path": "logs/Jewelix-.log",
          "rollingInterval": "Day",
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] [{RequestId}] [{CorrelationId}] [{ElapsedMs}ms] HTTP {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

> 💡 **Console sink** is automatically added by `UseJewelixLogger` — no `WriteTo.Console` entry is needed in `appsettings.json`. Use `LoggerExtensions.OutputTemplate` as the `outputTemplate` value for any additional sinks to keep output consistent.
>
> 🐞 **Debug body-capture sinks** should use `LoggerExtensions.DebugOutputTemplate` instead — it omits `{ElapsedMs}`, which is only pushed onto `LogContext` for the HTTP summary event, not for individual body-capture events.

---

## 📋 Log Format

### Standard (Information and above)

```
[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] [{RequestId}] [{CorrelationId}] [{ElapsedMs}ms] HTTP {Message:lj}{NewLine}{Exception}
```

**Example:**

```
[2026-05-18 17:04:22] [INF] [0HMXYZ:00000001] [a1b2c3d4-e5f6-...] [42ms] HTTP GET /api/orders responded 200
```

### Debug (body-capture events)

```
[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] [{RequestId}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}
```

**Example:**

```
[2026-05-18 17:04:22] [DBG] [0HMXYZ:00000001] [a1b2c3d4-e5f6-...] RequestBody={"username":"alice"}
```

### Token reference

| Token | Source | Description |
|---|---|---|
| `{Timestamp:yyyy-MM-dd HH:mm:ss}` | Serilog | UTC timestamp of the log event |
| `{Level:u3}` | Serilog | Three-letter uppercase level (`INF`, `WRN`, `ERR`, `DBG`, `FTL`) |
| `{RequestId}` | `LogContext` | ASP.NET Core `TraceIdentifier` for the request |
| `{CorrelationId}` | `LogContext` | `X-Correlation-ID` header value (auto-generated GUID if absent) |
| `{ElapsedMs}` | `LogContext` | Total request duration in milliseconds — **summary event only** |
| `{Message:lj}` | Serilog | Structured log message rendered as literal JSON |

---

## 🔍 Features

### 🪪 Correlation ID propagation
- Reads `X-Correlation-ID` from incoming request headers.
- Generates a new GUID when the header is absent.
- Echoes the ID back on the response via the same header.
- Pushes both `CorrelationId` and `RequestId` onto `LogContext` for the duration of the request, stamping every log event in the scope automatically.

### 🛡️ Sensitive field masking
Bodies logged at `Debug` level are sanitised before writing. The following JSON field names are masked (case-insensitive) by a source-generated regex — zero runtime compilation overhead:

| Field name | Example before | Example after |
|---|---|---|
| `password` | `"password":"hunter2"` | `"password":"***MASKED***"` |
| `token` | `"token":"abc.def.ghi"` | `"token":"***MASKED***"` |
| `secret` | `"secret":"s3cr3t"` | `"secret":"***MASKED***"` |
| `creditCard` | `"creditCard":"4111-..."` | `"creditCard":"***MASKED***"` |
| `apiKey` | `"apiKey":"sk_live_xyz"` | `"apiKey":"***MASKED***"` |

Bodies larger than **4 000 characters** are truncated and appended with `...(truncated)`.

### ⏱️ Elapsed time tracking
`ElapsedMs` is measured from the first byte received by the middleware to the last byte written to the response stream. It is pushed onto `LogContext` only around the HTTP summary log event so it renders correctly in `OutputTemplate` without polluting body-capture events.

### 📝 Request / response body capture (Debug)
When the configured minimum level is `Debug` or lower, the middleware automatically:
- Enables request body buffering (safe to re-read by downstream middleware).
- Reads, sanitises, and logs the request body as `RequestBody={Body}`.
- Swaps the response stream for a `MemoryStream`, captures, sanitises, and logs it as `ResponseBody={Body}`.
- Restores the original stream transparently — callers receive their response unchanged.

Only `application/json`, `application/xml`, `application/x-www-form-urlencoded`, and `text/*` content types are captured; binary payloads are skipped.

### 🔌 Microsoft.Extensions.Logging adapter
`SerilogLogger<T>` implements `ILogger<T>` and forwards every call to Serilog while:
- Lazily resolving `Log.ForContext<T>()` on each write — safe to resolve before Serilog is configured.
- Mapping all MEL log levels to Serilog `LogEventLevel` equivalents (including `LogLevel.None` suppression).
- Pushing scope state onto `LogContext` via `BeginScope`.

---

## 📐 Package dependencies

### `Jewelix.Logging` — runtime

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.AspNetCore.App` _(framework ref)_ | `10.0` | HTTP abstractions, `ILogger<T>`, configuration |
| `Serilog` | `4.3.1` | Core logging pipeline and `Log.Logger` |
| `Serilog.AspNetCore` | `9.0.0` | ASP.NET Core request logging integration |
| `Serilog.Settings.Configuration` | `10.0.0` | `appsettings.json` → Serilog binding |
| `Serilog.Sinks.Console` | `6.0.0` | Formatted console output |
| `Serilog.Sinks.File` | `6.0.0` | Rolling-file output |

### `Jewelix.Logging.Tests` — test-only

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.NET.Test.Sdk` | `17.12.0` | Test runner host |
| `Microsoft.AspNetCore.TestHost` | `10.0.0` | In-process HTTP server (no network required) |
| `xunit` | `2.9.2` | Test framework |
| `xunit.runner.visualstudio` | `2.8.2` | VS / `dotnet test` adapter |
| `Shouldly` | `4.3.0` | Fluent assertion library |

---

## 🚦 CI / CD

The pipeline lives in [`.github/workflows/cicd.yml`](.github/workflows/cicd.yml) and runs on **ubuntu-latest** with .NET 10.

### Pipeline overview

```
push / PR ──► ci  (Build & Test)
                │
                └── tag v*.*.* ──► publish  (Pack & Push)
```

| Job | Trigger | Steps |
|---|---|---|
| **Build & Test** (`ci`) | Every push to `main`; every pull request | restore → build → test → upload TRX results → annotate PR via `dorny/test-reporter` |
| **Pack & Publish** (`publish`) | Push of a `v*.*.*` tag (e.g. `v1.2.3`) | restore → build (versioned) → pack → push `.nupkg` + `.snupkg` to GitHub Packages |

### Required permissions

| Permission | Scope | Reason |
|---|---|---|
| `contents: read` | repo | checkout |
| `packages: write` | GitHub Packages | push NuGet packages |
| `checks: write` | PR checks | `dorny/test-reporter` PR annotations |

### 🏷️ Releasing a new version

1. Bump `<Version>` in [`src/Jewelix.Logging/Jewelix.Logging.Package.props`](src/Jewelix.Logging/Jewelix.Logging.Package.props).
2. Commit and push to `main`.
3. Create and push a matching semver tag:
   ```bash
   git tag v1.2.3
   git push origin v1.2.3
   ```
4. The **Pack & Publish** job extracts `1.2.3` from the tag, rebuilds with `-p:Version=1.2.3`, packs `Jewelix.Logging.1.2.3.nupkg` + `Jewelix.Logging.1.2.3.snupkg`, and pushes both to [GitHub Packages](https://github.com/Achi054/Library.Jewelix.Infrastructure/pkgs/nuget/Jewelix.Logging).

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

Then reference the package:

```xml
<PackageReference Include="Jewelix.Logging" Version="1.0.0" />
```

---

## 🗂️ Repository structure

```
Library.Jewelix.Infrastructure/
├── .github/
│   └── workflows/
│       └── cicd.yml                        # CI: build+test; CD: pack+publish on tag
│
├── src/
│   └── Jewelix.Logging/                    # 📦 Package: Jewelix.Logging v1.0.0
│       ├── Jewelix.Logging.csproj          # SDK project — imports Package.props
│       ├── Jewelix.Logging.Package.props   # NuGet identity, version & packaging metadata
│       ├── Jewelix-logo.png                # Package display icon (PNG 64×64, ≤1 MB)
│       ├── GlobalUsings.cs                 # Global implicit usings
│       ├── LoggerExtension.cs              # AddJewelixLogger / UseJewelixLogger
│       ├── LoggerMiddleware.cs             # HTTP middleware + body capture + Sanitize
│       └── SerilogLogger.cs               # ILogger<T> → Serilog adapter
│
├── test/
│   └── Jewelix.Logging.Tests/              # 🧪 31 tests — xUnit + Shouldly + TestHost
│       ├── Helper/
│       │   ├── InMemorySink.cs             # Thread-safe Serilog sink for assertions
│       │   └── SerilogTestCollection.cs    # [CollectionDefinition] — sequential execution
│       ├── DependencyInjectionTests.cs     # AddJewelixLogger DI registration
│       ├── LoggerMiddlewareTests.cs        # Middleware: correlation ID, body capture, masking
│       ├── SanitizeTests.cs               # Sanitize: masking, truncation
│       ├── SerilogLoggerTests.cs          # SerilogLogger<T>: level mapping, scope, context
│       └── UseJewelixLoggerTests.cs       # UseJewelixLogger: pipeline + null guards
│
├── .editorconfig                           # C# code style rules
├── .gitignore
├── Directory.Build.props                   # Shared: TFM, nullable, analyzers, XML docs
├── Directory.Build.targets                 # Shared: InternalsVisibleTo
├── Library.Jewelix.Infrastructure.slnx    # Solution file (.slnx format)
└── README.md
```

---

## 🧪 Running tests

```bash
# All tests
dotnet test

# With detailed output
dotnet test --verbosity normal

# Specific test class
dotnet test --filter "FullyQualifiedName~LoggerMiddlewareTests"
```

The test suite runs **31 tests** covering:

| Test class | Coverage area |
|---|---|
| `DependencyInjectionTests` | `AddJewelixLogger` — DI registration, idempotency, null guard |
| `LoggerMiddlewareTests` | Correlation ID generation/propagation, body capture, masking, elapsed time |
| `SanitizeTests` | Field masking (all 5 field names, case-insensitive), truncation, empty input |
| `SerilogLoggerTests` | Level mapping (all 6 MEL levels), exception attachment, `SourceContext` stamping, `BeginScope` scope enrichment |
| `UseJewelixLoggerTests` | Serilog config from `appsettings.json`, middleware pipeline registration, null guards |

The `SerilogTestCollection` collection fixture forces **sequential** execution within the collection to prevent test races on the shared static `Log.Logger`.

---
