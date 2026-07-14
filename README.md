# HarborShield 🚧 (v1)

Event-driven maritime cargo, vessel-route anomaly, and sanctions-risk platform, with a local/offline RAG-based Risk Case Copilot.

Built as a hands-on prep project for backend/API-platform roles (auth, resilience, observability, CI, and containerized deployment on top of a real domain), not just a CRUD demo.

## What it does

1. Ingest vessel position events + cargo manifests via a REST API
2. Rule-based route/behavior anomaly detection (route deviation, dark tracking gaps, restricted-zone entry)
3. Sanctions/ownership screening against a simulated vendor API (retry, circuit breaker, timeout via Polly)
4. Risk case creation, querying, and resolution via API
5. Customer webhook notifications on risk case create/update (HMAC-signed, retried)
6. **Risk Case Copilot (RAG)** — retrieves similar historical cases via pgvector similarity search and generates a plain-language explanation of why a case was flagged, fully local, no API keys, no external service calls

## Architecture

Modular monolith (not microservices) — one deployable per process (Api, Worker), clean layer boundaries (Domain / Application / Infrastructure), so it can split into services later without a rewrite.

```
Api (REST, auth, rate limiting)  ─┐
                                   ├─> Application (use cases, CQRS via Mediator) ─> Domain (entities)
Worker (background jobs)  ────────┘                    │
                                                        └─> Infrastructure (EF Core/Postgres, HttpClient/Polly, RiskCopilot RAG)
```

- **Api** handles synchronous requests: ingest positions/manifests, query/resolve risk cases, register webhook endpoints, and (in Development) serve an interactive OpenAPI explorer at `/scalar`.
- **Worker** runs the async pipeline as four hosted background services: anomaly detection, sanctions screening, webhook delivery, and risk-case embedding (for RAG retrieval) — each polling its own outbox-style queue in Postgres.
- Both processes share the same Domain/Application/Infrastructure/RiskCopilot libraries; there's no message broker in v1 — Postgres itself is the queue.

## Solution structure

| Project | Type | Purpose |
|---|---|---|
| `HarborShield.Api` | ASP.NET Core Web API | Ingestion, risk case, webhook-registration endpoints, auth, rate limiting, idempotency |
| `HarborShield.Worker` | Worker Service | Anomaly detection, sanctions screening, webhook delivery, risk-case embedding (all off Postgres-backed queues) |
| `HarborShield.Domain` | Class Library | Entities: Vessel, CargoManifest, RiskCase, RestrictedZone, WebhookEndpoint/Delivery, IdempotencyRecord |
| `HarborShield.Application` | Class Library | Use cases/handlers (CQRS via [Mediator](https://github.com/martinothamar/Mediator)), FluentValidation pipeline |
| `HarborShield.Infrastructure` | Class Library | EF Core + PostgreSQL/PostGIS/pgvector, HttpClientFactory + Polly resilience, health checks |
| `HarborShield.RiskCopilot` | Class Library | RAG: local embeddings + pgvector retrieval + LLamaSharp generation |
| `HarborShield.Contracts` | Class Library | Shared DTOs/integration events |
| `HarborShield.ServiceDefaults` | Class Library | Shared OpenTelemetry/health-check/resilience defaults (Aspire) |
| `HarborShield.AppHost` | .NET Aspire App Host | Local orchestration of Api + Worker for `dotnet run`-based development |
| `HarborShield.UnitTests` | xUnit | Handler/domain-logic unit tests |
| `HarborShield.IntegrationTests` | xUnit | Testcontainers (real Postgres) + WireMock.Net (mock vendor API) + WebApplicationFactory |

## Tech stack

- **Runtime**: C#, .NET 10, ASP.NET Core Web API, Worker Service
- **CQRS**: [Mediator](https://github.com/martinothamar/Mediator) (source-generated, MIT-licensed — not MediatR, which requires a commercial license)
- **Data**: PostgreSQL + PostGIS (geospatial queries) + pgvector (RAG similarity search), EF Core
- **Resilience**: `HttpClientFactory` + `Microsoft.Extensions.Http.Resilience` (Polly retry/circuit-breaker/timeout)
- **Auth**: Custom API key scheme (`X-Api-Key` header, timing-safe comparison)
- **Traffic control**: ASP.NET Core built-in rate limiter (fixed window, per-IP, 100 req/min on customer-facing endpoints), idempotency-key middleware on ingestion endpoints
- **Validation**: FluentValidation, wired into the Mediator pipeline
- **Docs**: OpenAPI + [Scalar](https://github.com/scalar/scalar) interactive API explorer (Development only)
- **Observability**: Serilog structured console logs, correlation IDs (`X-Correlation-Id`, propagated through logs), liveness (`/alive`) vs. readiness (`/health`, real Postgres check) health endpoints
- **Testing**: xUnit, FluentAssertions 7.x (last Apache-2.0-licensed release — 8.x requires a commercial license), Testcontainers.PostgreSql, WireMock.Net
- **CI**: GitHub Actions (restore, build, unit tests, integration tests against a real containerized Postgres)
- **Containerization**: Multi-stage Dockerfiles for Api and Worker, `docker-compose.yml` for a one-command local stack
- **Local orchestration (dev)**: .NET Aspire AppHost
- **RAG (Risk Case Copilot)**: [LLamaSharp](https://github.com/SciSharp/LLamaSharp) running fully local, quantized GGUF models — `bge-small-en-v1.5` (embeddings, 384-dim, stored in pgvector) and `Qwen2.5-0.5B-Instruct` (generation, ~500MB - chosen to keep the whole stack runnable on a small/free-tier cloud instance) — no API key, no external network call

## Running it

### Option A — Docker Compose (full stack, closest to "deployed")

```bash
./scripts/download-models.sh          # downloads both GGUF models into ./models (~530 MB)
docker compose up -d --build
```

This builds and starts three containers: `harborshield-postgres` (PostGIS + pgvector), `harborshield-api` (port `8080`), and `harborshield-worker`. The `./models` folder is mounted read-only into both the Api and Worker containers so RiskCopilot can load the GGUF files.

Verify it's up:

```bash
curl http://localhost:8080/alive     # liveness - always 200 once the process is running
curl http://localhost:8080/health    # readiness - 200 only once Postgres is reachable
```

Everything under `/api/*` (except the `/api/fake-vendor/*` and `/api/fake-customer/*` test doubles) requires an `X-Api-Key` header — the compose file sets it to `dev-local-harborshield-key-2026` for local use. Interactive docs are at `http://localhost:8080/scalar` when `ASPNETCORE_ENVIRONMENT=Development` (the compose default).

`docker compose down` stops everything; add `-v` to also drop the Postgres data volume.

### Option B — Visual Studio / `dotnet run` (day-to-day development)

1. `docker compose up -d postgres` (just the database)
2. Set `RiskCopilot:GenerationModelPath` / `RiskCopilot:EmbeddingModelPath` in `appsettings.Development.json` (both projects) to your local `./models/*.gguf` paths
3. Run `HarborShield.AppHost` (or `HarborShield.Api` and `HarborShield.Worker` directly) from Visual Studio 2026 / `dotnet run`

### Trying the golden path

1. `POST /api/vessels` — register a vessel (IMO number, name, flag country)
2. `POST /api/vessels/{vesselId}/positions` — report a position; repeat with a position inside a restricted zone or a large speed/heading jump to trigger anomaly detection
3. The Worker picks it up, creates a `RiskCase`, and (if sanctions screening flags the vessel) enriches it
4. `GET /api/risk-cases/{riskCaseId}` — see the case
5. `GET /api/risk-cases/{riskCaseId}/explain` — the RAG copilot retrieves similar past cases and generates a plain-language explanation (only meaningful once a few risk cases exist to retrieve from)
6. `POST /api/webhook-endpoints` to register a receiver, then watch `harborshield-worker`'s logs deliver signed webhook payloads on case create/resolve

All ingestion endpoints (`positions`, `cargo-manifests`) accept an `Idempotency-Key` header — replaying the same key + path returns the original cached response instead of creating a duplicate.

## Testing

```bash
dotnet test HarborShield/HarborShield.UnitTests
docker build -t harborshield-maritime-risk-platform-postgres:latest ./docker/postgres   # integration tests spin this image up via Testcontainers
dotnet test HarborShield/HarborShield.IntegrationTests
```

CI (`.github/workflows/ci.yml`) runs the same steps against a real containerized Postgres on every push/PR to `main`.

## Roadmap (v2, deferred)

- Split into true microservices
- RabbitMQ/Wolverine event bus (replacing the Postgres-backed outbox used in v1)
- Cloud deployment (ECS/Fargate or similar) + Terraform
- Dead-letter queues, centralized metrics/tracing backend
- Partition sanctions/webhook workers by customer API key instead of a single shared rate-limit/circuit-breaker pool
- gRPC between services, SignalR live updates
