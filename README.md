# HarborShield 🚧 (v1 — under active development)

Event-driven maritime cargo, vessel-route anomaly, and sanctions-risk platform, with a local/offline RAG-based Risk Case Copilot.

## What it does (v1 scope)

1. Ingest vessel position events + cargo manifests via REST API
2. Rule-based route/behavior anomaly detection (route deviation, dark tracking gaps, restricted-zone entry)
3. Simulated sanctions/ownership screening against an external vendor API (retry, circuit breaker, timeout)
4. Risk case creation, querying, and resolution via API
5. Customer webhook notifications on risk case create/update (HMAC-signed, retried)
6. **Risk Case Copilot (RAG)** — retrieves similar historical cases/vessel history and generates a plain-language explanation of why a case was flagged, fully local, no API keys

## Architecture

Modular monolith (not microservices yet) — one deployable, clean module boundaries so it can split into services later without a rewrite.

## Solution structure

| Project | Type | Purpose |
|---|---|---|
| `HarborShield.Api` | ASP.NET Core Web API | Ingestion, risk case, and webhook-registration endpoints |
| `HarborShield.Worker` | Worker Service | Anomaly detection + sanctions screening (async, off an outbox) |
| `HarborShield.Domain` | Class Library | Entities: Vessel, CargoManifest, RiskCase, etc. |
| `HarborShield.Application` | Class Library | Use cases / handlers (MediatR) |
| `HarborShield.Infrastructure` | Class Library | EF Core, PostgreSQL, HttpClientFactory/Polly |
| `HarborShield.RiskCopilot` | Class Library | RAG: embeddings + pgvector retrieval + LLamaSharp generation |
| `HarborShield.Contracts` | Class Library | Shared DTOs/events |
| `HarborShield.ServiceDefaults` | Class Library | Shared OpenTelemetry/health-check/resilience defaults (Aspire) |
| `HarborShield.AppHost` | .NET Aspire App Host | Local orchestration of Api + Worker |
| `HarborShield.UnitTests` | xUnit | |
| `HarborShield.IntegrationTests` | xUnit | Testcontainers + WireMock.Net |

## Tech stack (v1)

- **Runtime**: C#, .NET 10, ASP.NET Core Web API, Worker Service
- **Data**: PostgreSQL + PostGIS (geospatial) + pgvector (RAG vectors), EF Core
- **Resilience**: `HttpClientFactory` + Polly/.NET resilience handlers
- **Validation/Auth**: FluentValidation, JWT/API key (added after basic API works), idempotency keys, rate limiting
- **Docs**: OpenAPI/Swagger
- **Observability**: Serilog structured logs, correlation IDs, OpenTelemetry traces/metrics, health checks
- **Testing**: xUnit, FluentAssertions, Testcontainers (Postgres), WireMock.Net (mock vendor API)
- **CI**: GitHub Actions (build, test)
- **Local orchestration**: .NET Aspire AppHost
- **RAG (Risk Case Copilot)**: local ONNX embedding model (e.g. all-MiniLM-L6-v2) + pgvector store + LLamaSharp running a small quantized open model (e.g. Phi-3-mini / Llama-3.2-3B-Instruct GGUF) — fully offline, no API key, no external service

## Roadmap (v2, deferred)

- Split into true microservices
- RabbitMQ/Wolverine event bus (replacing in-process/outbox for v1)
- AWS deployment (ECS/Fargate) + Terraform
- SQS + dead-letter queues, CloudWatch
- MongoDB/time-series store
- gRPC between services
- SignalR live updates
- Automated incident/runbook actions
