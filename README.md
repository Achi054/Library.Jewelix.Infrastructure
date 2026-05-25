# 🏗️ Library.Jewelix.Infrastructure

[![Jewelix.Logging CI](https://github.com/Achi054/Library.Jewelix.Infrastructure/actions/workflows/cicd-logging.yml/badge.svg)](https://github.com/Achi054/Library.Jewelix.Infrastructure/actions/workflows/cicd-logging.yml)
[![Jewelix.OpenApi CI](https://github.com/Achi054/Library.Jewelix.Infrastructure/actions/workflows/cicd-openapi.yml/badge.svg)](https://github.com/Achi054/Library.Jewelix.Infrastructure/actions/workflows/cicd-openapi.yml)
[![Latest Release](https://img.shields.io/github/v/release/Achi054/Library.Jewelix.Infrastructure?label=Jewelix.Logging&color=blue&logo=nuget)](https://github.com/Achi054/Library.Jewelix.Infrastructure/pkgs/nuget/Jewelix.Logging)
[![Latest Release](https://img.shields.io/github/v/release/Achi054/Library.Jewelix.Infrastructure?label=Jewelix.OpenApi&color=purple&logo=nuget)](https://github.com/Achi054/Library.Jewelix.Infrastructure/pkgs/nuget/Jewelix.OpenApi)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

> Cross-cutting infrastructure library for the **Jewelix** ecosystem. Ships production-ready, opinionated packages that plug directly into the ASP.NET Core pipeline with zero boilerplate.

---

## 📦 Packages

| Package | Version | Target | Description |
|---|---|---|---|
| [`Jewelix.Logging`](src/Jewelix.Logging) | `1.0.0` | `net10.0` | Serilog-backed `ILogger<T>` adapter, HTTP request/response middleware, correlation-ID propagation, sensitive-field masking, and DI registration helpers |
| [`Jewelix.OpenApi`](src/Jewelix.OpenApi) | `1.0.0` | `net10.0` | Microsoft OpenAPI + Scalar UI integration with multi-document support, optional Bearer/JWT auth, and config-section override |

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

## 🚀 Quick Start — `Jewelix.OpenApi`

### 1️⃣ Add the NuGet reference

```xml
<PackageReference Include="Jewelix.OpenApi" Version="1.0.0" />
```

### 2️⃣ Register services (DI)

```csharp
// Program.cs — zero-config: registers a single "v1" document
builder.Services.AddJewelixOpenApi();

// Or configure explicitly:
builder.Services.AddJewelixOpenApi(opts =>
{
    opts.Documents =
    [
        new() { Name = "v1", Title = "Jewelix API v1", EnableBearerAuth = true },
        new() { Name = "v2", Title = "Jewelix API v2", EnableBearerAuth = true },
    ];
});
```

### 3️⃣ Wire up the endpoints

```csharp
// Program.cs — after Build()
app.UseJewelixOpenApi(app.Configuration);
```

`UseJewelixOpenApi` maps one OpenAPI JSON endpoint and one Scalar UI page per document:

| Document | JSON endpoint | Scalar UI |
|---|---|---|
| `v1` | `/openapi/v1.json` | `/scalar/v1/` |
| `v2` | `/openapi/v2.json` | `/scalar/v2/` |

### 4️⃣ Optional — override presentation properties from `appsettings.json`

`UseJewelixOpenApi` merges the `OpenApi` config section on top of the code-configured options at startup. Only presentation properties are overridable — `EnableBearerAuth` is excluded because its transformer must be registered during DI setup.

#### Single-document app

```json
{
  "OpenApi": {
    "Documents": [
      {
        "Name": "v1",
        "Title": "Jewelix API — Production",
        "Version": "2.1",
        "Description": "Public-facing REST API. Contact platform-team@jewelix.io for access.",
        "ScalarRoutePrefix": "docs"
      }
    ]
  }
}
```

This configuration produces:

| Endpoint | URL |
|---|---|
| OpenAPI JSON | `/openapi/v1.json` |
| Scalar UI | `/docs/v1/` |

#### Multi-document app (e.g. public + internal APIs)

```json
{
  "OpenApi": {
    "Documents": [
      {
        "Name": "public",
        "Title": "Jewelix Public API",
        "Version": "1.0",
        "Description": "Stable, versioned endpoints for third-party integrations.",
        "ScalarRoutePrefix": "scalar"
      },
      {
        "Name": "internal",
        "Title": "Jewelix Internal API",
        "Version": "1.0",
        "Description": "Back-office endpoints — not intended for external consumers.",
        "ScalarRoutePrefix": "scalar"
      }
    ]
  }
}
```

#### Combined with the Serilog section

Both packages read from the same `appsettings.json` — the sections are independent:

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
          "rollingInterval": "Day"
        }
      }
    ]
  },
  "OpenApi": {
    "Documents": [
      {
        "Name": "v1",
        "Title": "Jewelix API",
        "Version": "1.0",
        "Description": "Primary API surface."
      }
    ]
  }
}
```

#### Property reference

| Property | Type | Overridable via config | Default |
|---|---|---|---|
| `Name` | `string` | Used as lookup key only — must match a code-registered document | `"v1"` |
| `Title` | `string` | ✅ Yes | `"API"` |
| `Version` | `string` | ✅ Yes | `"1.0"` |
| `Description` | `string?` | ✅ Yes | `null` |
| `ScalarRoutePrefix` | `string` | ✅ Yes | `"scalar"` |
| `EnableBearerAuth` | `bool` | ❌ Code only | `false` |

> ⚠️ **Partial overrides are safe.** Properties absent from the config section are left untouched — only keys that are explicitly present and differ from the default are applied. Code-configured values (e.g. a custom `Version` or `ScalarRoutePrefix`) are preserved when the config section omits them.
>
> ⚠️ **`EnableBearerAuth` is code-only.** It must be set in `AddJewelixOpenApi` — it drives transformer registration which happens during DI setup and cannot be changed at runtime via config.

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

### `Jewelix.OpenApi` — runtime

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.AspNetCore.App` _(framework ref)_ | `10.0` | HTTP abstractions, routing, configuration |
| `Microsoft.AspNetCore.OpenApi` | `10.0.8` | OpenAPI document generation and transformer pipeline |
| `Scalar.AspNetCore` | `2.14.14` | Modern API documentation UI |

### `Jewelix.OpenApi.Tests` — test-only

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.NET.Test.Sdk` | `17.12.0` | Test runner host |
| `Microsoft.AspNetCore.TestHost` | `10.0.0` | In-process HTTP server |
| `Microsoft.AspNetCore.OpenApi` | `10.0.8` | OpenAPI model access in tests |
| `xunit` | `2.9.2` | Test framework |
| `xunit.runner.visualstudio` | `2.8.2` | VS / `dotnet test` adapter |
| `Shouldly` | `4.3.0` | Fluent assertion library |

---

## 🚦 CI / CD

Each package has its own independent pipeline so versions can be released separately.

| Pipeline | File | Package |
|---|---|---|
| `cicd-logging.yml` | [`.github/workflows/cicd-logging.yml`](.github/workflows/cicd-logging.yml) | `Jewelix.Logging` |
| `cicd-openapi.yml` | [`.github/workflows/cicd-openapi.yml`](.github/workflows/cicd-openapi.yml) | `Jewelix.OpenApi` |

### Pipeline overview

```
push / PR ──► ci  (Build & Test)
                │
                └── tag v*.*.* ──► publish  (Pack & Push)
```

| Job | Workflow | Trigger | Steps |
|---|---|---|---|
| **Build & Test** | `cicd-logging.yml` | Push to `main`; every PR | restore → build → test → upload TRX → annotate PR |
| **Pack & Publish** | `cicd-logging.yml` | Push of `v*.*.*` tag | restore → build → pack `Jewelix.Logging` → push `.nupkg` + `.snupkg` |
| **Build & Test** | `cicd-openapi.yml` | Push to `main`; every PR | restore → build → test → upload TRX → annotate PR |
| **Pack & Publish** | `cicd-openapi.yml` | Push of `v*.*.*` tag | restore → build → pack `Jewelix.OpenApi` → push `.nupkg` + `.snupkg` |

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
│       ├── cicd-logging.yml                # CI/CD for Jewelix.Logging (independent release)
│       └── cicd-openapi.yml                # CI/CD for Jewelix.OpenApi (independent release)
│
├── src/
│   ├── Jewelix.Logging/                    # 📦 Package: Jewelix.Logging v1.0.0
│   │   ├── Jewelix.Logging.csproj          # SDK project — imports Package.props
│   │   ├── Jewelix.Logging.Package.props   # NuGet identity, version & packaging metadata
│   │   ├── Jewelix-logo.png                # Package display icon (PNG 64×64, ≤1 MB)
│   │   ├── GlobalUsings.cs                 # Global implicit usings
│   │   ├── LoggerExtension.cs              # AddJewelixLogger / UseJewelixLogger
│   │   ├── LoggerMiddleware.cs             # HTTP middleware + body capture + Sanitize
│   │   └── SerilogLogger.cs               # ILogger<T> → Serilog adapter
│   │
│   └── Jewelix.OpenApi/                    # 📦 Package: Jewelix.OpenApi v1.0.0
│       ├── Jewelix.OpenApi.csproj          # SDK project — imports Package.props
│       ├── Jewelix.OpenApi.Package.props   # NuGet identity, version & packaging metadata
│       ├── Jewelix-logo.png                # Package display icon (PNG 64×64, ≤1 MB)
│       ├── GlobalUsings.cs                 # Global implicit usings
│       ├── JewelixOpenApiDocument.cs       # Per-document config (Name, Title, Version, etc.)
│       ├── JewelixOpenApiOptions.cs        # Root options — Documents collection + SectionName
│       ├── BearerSecuritySchemeTransformer.cs  # Internal IOpenApiDocumentTransformer
│       └── OpenApiExtensions.cs            # AddJewelixOpenApi / UseJewelixOpenApi
│
├── test/
│   ├── Jewelix.Logging.Tests/              # 🧪 31 tests — xUnit + Shouldly + TestHost
│   │   ├── Helper/
│   │   │   ├── InMemorySink.cs             # Thread-safe Serilog sink for assertions
│   │   │   └── SerilogTestCollection.cs    # [CollectionDefinition] — sequential execution
│   │   ├── DependencyInjectionTests.cs     # AddJewelixLogger DI registration
│   │   ├── LoggerMiddlewareTests.cs        # Middleware: correlation ID, body capture, masking
│   │   ├── SanitizeTests.cs               # Sanitize: masking, truncation
│   │   ├── SerilogLoggerTests.cs          # SerilogLogger<T>: level mapping, scope, context
│   │   └── UseJewelixLoggerTests.cs       # UseJewelixLogger: pipeline + null guards
│   │
│   └── Jewelix.OpenApi.Tests/              # 🧪 29 tests — xUnit + Shouldly + TestHost
│       ├── BearerSecuritySchemeTransformerTests.cs  # Transformer: Bearer scheme injection
│       ├── DependencyInjectionTests.cs     # AddJewelixOpenApi DI registration
│       ├── JewelixOpenApiOptionsTests.cs   # Options defaults and SectionName
│       ├── OpenApiExtensionsTests.cs       # UseJewelixOpenApi guards + config override
│       └── UseJewelixOpenApiTests.cs       # Full pipeline via WebApplication + TestServer
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

The test suite runs **60 tests** across two test projects.

### `Jewelix.Logging.Tests` — 31 tests

| Test class | Coverage area |
|---|---|
| `DependencyInjectionTests` | `AddJewelixLogger` — DI registration, idempotency, null guard |
| `LoggerMiddlewareTests` | Correlation ID generation/propagation, body capture, masking, elapsed time |
| `SanitizeTests` | Field masking (all 5 field names, case-insensitive), truncation, empty input |
| `SerilogLoggerTests` | Level mapping (all 6 MEL levels), exception attachment, `SourceContext` stamping, `BeginScope` scope enrichment |
| `UseJewelixLoggerTests` | Serilog config from `appsettings.json`, middleware pipeline registration, null guards |

The `SerilogTestCollection` collection fixture forces **sequential** execution within the collection to prevent test races on the shared static `Log.Logger`.

### `Jewelix.OpenApi.Tests` — 29 tests

| Test class | Coverage area |
|---|---|
| `JewelixOpenApiOptionsTests` | Default values for all options properties; `SectionName` constant |
| `BearerSecuritySchemeTransformerTests` | Bearer scheme injection, idempotency, preservation of existing schemes |
| `DependencyInjectionTests` | `AddJewelixOpenApi` — null guard, singleton registration, multi-document, chaining |
| `OpenApiExtensionsTests` | `UseJewelixOpenApi` null guard, non-`IEndpointRouteBuilder` guard, config override, `EnableBearerAuth` exclusion |
| `UseJewelixOpenApiTests` | Full pipeline via `WebApplication` + `TestHost` — JSON endpoints, Scalar UI, Bearer auth in spec |

---
