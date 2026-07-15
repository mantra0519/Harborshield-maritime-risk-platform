# HarborShield

[![CI](https://github.com/mantra0519/Harborshield-maritime-risk-platform/actions/workflows/ci.yml/badge.svg)](https://github.com/mantra0519/Harborshield-maritime-risk-platform/actions/workflows/ci.yml)

**Live demo: [http://3.139.112.141:8080/](http://3.139.112.141:8080/)** — no login required, browse real vessels and risk cases directly. Interactive API docs at [/scalar](http://3.139.112.141:8080/scalar).

A maritime cargo, vessel-route anomaly, and sanctions-risk platform: it ingests vessel position/cargo data, flags anomalous behavior with rule-based detection, screens vessels against a simulated sanctions vendor, and generates a plain-language explanation of each flagged case using a fully local, offline LLM — no OpenAI/Anthropic API key, no external AI service, no per-request cost.

Built as a real, deployed system (not just code that compiles) — every piece described below has been run, tested, and verified working, including the live AWS deployment.

## What it does

1. **Ingests** vessel position events and cargo manifests via a REST API
2. **Detects anomalies** with rule-based checks: route deviation (implausible speed/distance), restricted-zone entry, and tracking gaps (AIS signal loss)
3. **Screens vessels** against a simulated sanctions/ownership vendor, with retry + circuit breaker + timeout handling for a vendor that's deliberately unreliable
4. **Creates and tracks risk cases** — open, acknowledge, resolve — queryable via API
5. **Notifies customers** of risk case events via HMAC-signed, retried webhooks
6. **Explains itself**: the Risk Case Copilot retrieves similar historical cases (pgvector similarity search) and generates a plain-language explanation of why a case was flagged — entirely on a locally-hosted LLM, cached after first generation
7. **Serves a dashboard** — a plain server-rendered page (no SPA framework) showing live vessels and risk cases, so the system is understandable without an API client or auth token

## Architecture

Modular monolith (not microservices) — two deployables (Api, Worker) sharing clean layer boundaries (Domain / Application / Infrastructure), structured so it could split into real services later without a rewrite.

```
Api (REST + dashboard, auth, rate limiting)  ─┐
                                                ├─> Application (CQRS via Mediator) ─> Domain (entities)
Worker (background jobs)  ─────────────────────┘                    │
                                                                     └─> Infrastructure (EF Core/Postgres, HttpClient/Polly, RiskCopilot RAG)
```

- **Api** — the public surface. Serves the JSON REST API (auth-protected, rate-limited) and a read-only Razor Pages dashboard (unauthenticated, for anyone to browse).
- **Worker** — four hosted background services, each polling its own Postgres-backed queue: anomaly detection, sanctions screening, webhook delivery, and risk-case embedding (for RAG retrieval). No message broker in v1 — Postgres itself is the queue.
- Both processes share the same Domain/Application/Infrastructure/RiskCopilot libraries and are built from the same solution.

## Live deployment

Running right now on AWS: a single EC2 instance (free-tier eligible `t2/t3.micro`, 1 vCPU / 1GB RAM) running the exact `docker-compose.yml` in this repo — Postgres, Api, and Worker as three containers, Elastic IP for a stable address. No managed database, no load balancer, no orchestration platform — deliberately minimal so it costs nothing beyond the AWS free tier.

Deploying an update means SSHing in, `git pull`, and `docker compose up -d --build` — there's no CI/CD pipeline pushing changes automatically yet (see Roadmap).

## Solution structure

| Project | Type | Purpose |
|---|---|---|
| `HarborShield.Api` | ASP.NET Core Web API | REST endpoints, dashboard (Razor Pages), auth, rate limiting, idempotency |
| `HarborShield.Worker` | Worker Service | Anomaly detection, sanctions screening, webhook delivery, risk-case embedding |
| `HarborShield.Domain` | Class Library | Entities: Vessel, CargoManifest, RiskCase, RestrictedZone, WebhookEndpoint/Delivery, IdempotencyRecord |
| `HarborShield.Application` | Class Library | Use cases/handlers (CQRS via [Mediator](https://github.com/martinothamar/Mediator)), FluentValidation pipeline |
| `HarborShield.Infrastructure` | Class Library | EF Core + PostgreSQL/PostGIS/pgvector, HttpClientFactory + Polly resilience, health checks |
| `HarborShield.RiskCopilot` | Class Library | RAG: local embeddings + pgvector retrieval + LLamaSharp generation |
| `HarborShield.Contracts` | Class Library | Shared DTOs/integration events |
| `HarborShield.ServiceDefaults` | Class Library | Shared OpenTelemetry/health-check/resilience defaults (Aspire) |
| `HarborShield.AppHost` | .NET Aspire App Host | Local orchestration of Api + Worker for `dotnet run`-based development |
| `HarborShield.UnitTests` | xUnit | Handler/domain-logic unit tests |
| `HarborShield.IntegrationTests` | xUnit | Testcontainers (real, isolated Postgres per run) + WireMock.Net (mock vendor) + WebApplicationFactory |

## Tech stack

**Runtime & language**
- C# / .NET 10 — ASP.NET Core Web API, Worker Service, Razor Pages

**CQRS & validation**
- [Mediator](https://github.com/martinothamar/Mediator) — source-generated, MIT-licensed (not MediatR, which requires a commercial license for this kind of use)
- FluentValidation, wired into the Mediator pipeline as a behavior

**Data**
- PostgreSQL, with two extensions in the same database: **PostGIS** (geospatial queries — restricted-zone containment checks) and **pgvector** (384-dim embeddings for RAG similarity search)
- EF Core, code-first migrations

**Resilience**
- `HttpClientFactory` + `Microsoft.Extensions.Http.Resilience` (Polly) — retry with exponential backoff, circuit breaker, timeout — wrapping calls to the (deliberately flaky) sanctions vendor

**Security**
- Custom API key authentication scheme (`X-Api-Key` header, timing-safe comparison, fails closed if unconfigured)
- ASP.NET Core built-in rate limiter (fixed window, per-IP, 100 req/min on customer-facing endpoints)
- Idempotency-key middleware on ingestion endpoints (replays cached response instead of creating duplicates)
- HMAC-SHA256 signed webhook payloads

**Observability**
- Serilog structured console logging, correlation IDs (`X-Correlation-Id`) propagated through every log line
- Separate liveness (`/alive`) and readiness (`/health`, real Postgres connectivity check) endpoints

**AI / RAG (Risk Case Copilot)**
- [LLamaSharp](https://github.com/SciSharp/LLamaSharp) running fully local, quantized GGUF models on CPU — no external API call, no per-token cost
- `bge-small-en-v1.5` (Q8, ~35MB) for embeddings, `Qwen2.5-0.5B-Instruct` (Q4_K_M, ~490MB) for generation — sized deliberately small so the whole stack (API + worker + both models + Postgres) fits comfortably on a 1GB free-tier cloud instance
- Explanations are generated once and cached in Postgres; retrieval pulls similar historical cases via pgvector cosine-distance before generating, so answers are grounded in real prior cases, not just the current one

**Docs**
- OpenAPI + [Scalar](https://github.com/scalar/scalar) interactive API explorer (enabled in Development)

**Testing & CI**
- xUnit, FluentAssertions 7.x (last Apache-2.0-licensed release), Testcontainers.PostgreSql (real, isolated Postgres per test run), WireMock.Net (mock vendor)
- GitHub Actions — restore, build, unit tests, integration tests against a real containerized Postgres, on every push

**Containerization & deployment**
- Multi-stage Dockerfiles for Api and Worker, `docker-compose.yml` for a one-command full stack (Postgres + Api + Worker)
- Deployed on AWS EC2 (see [Live deployment](#live-deployment) above)

## Running it locally

### Option A — Docker Compose (full stack, closest to how it's actually deployed)

```bash
./scripts/download-models.sh          # downloads both GGUF models into ./models (~530 MB)
docker compose up -d --build
```

This builds and starts three containers: `harborshield-postgres` (PostGIS + pgvector), `harborshield-api` (port `8080`), and `harborshield-worker`. The `./models` folder is mounted read-only into both the Api and Worker containers.

Verify it's up:

```bash
curl http://localhost:8080/alive     # liveness - always 200 once the process is running
curl http://localhost:8080/health    # readiness - 200 only once Postgres is reachable
```

Everything under `/api/*` (except the `/api/fake-vendor/*` and `/api/fake-customer/*` test doubles, which simulate external systems) requires an `X-Api-Key` header — the compose file sets it to `dev-local-harborshield-key-2026` (the same key the live demo uses). Interactive docs are at `http://localhost:8080/scalar`.

`docker compose down` stops everything; add `-v` to also drop the Postgres data volume.

### Option B — Visual Studio / `dotnet run` (day-to-day development)

1. `docker compose up -d postgres` (just the database)
2. Create your own `appsettings.Development.json` (both `HarborShield.Api` and `HarborShield.Worker`; this file is gitignored on purpose — it holds local connection strings/paths, not anything that should be shared or committed) pointing `RiskCopilot:GenerationModelPath` / `RiskCopilot:EmbeddingModelPath` at your local `./models/*.gguf` files
3. Run `HarborShield.AppHost` (or `HarborShield.Api` and `HarborShield.Worker` directly) from Visual Studio 2026 / `dotnet run`

### Trying the golden path

1. `POST /api/vessels` — register a vessel (IMO number, name, flag country)
2. `POST /api/vessels/{vesselId}/positions` — report a position; repeat with a position inside a restricted zone or a large speed/heading jump to trigger anomaly detection
3. The Worker picks it up, creates a `RiskCase`, and (if sanctions screening flags the vessel) enriches it
4. `GET /api/risk-cases/{riskCaseId}` — see the case, or browse it on the dashboard at `/`
5. `GET /api/risk-cases/{riskCaseId}/explain` — the RAG copilot retrieves similar past cases and generates a plain-language explanation (first call per case runs live inference; cached afterward)
6. `POST /api/webhook-endpoints` to register a receiver, then watch `harborshield-worker`'s logs deliver signed webhook payloads on case create/resolve

All ingestion endpoints (`positions`, `cargo-manifests`) accept an `Idempotency-Key` header — replaying the same key + path returns the original cached response instead of creating a duplicate.

## Testing

```bash
dotnet test HarborShield/HarborShield.UnitTests
docker build -t harborshield-maritime-risk-platform-postgres:latest ./docker/postgres   # integration tests spin this image up via Testcontainers
dotnet test HarborShield/HarborShield.IntegrationTests
```

Integration tests spin up a real, isolated Postgres container per test run (via Testcontainers) and a WireMock server standing in for the sanctions vendor — no shared state between runs, and no dependency on any locally-running database. CI (`.github/workflows/ci.yml`) runs the same steps on every push to `main`.

## Security notes

- The database is never exposed to the internet — only the application layer is. On the live deployment, the cloud firewall (AWS security group) only allows inbound traffic on the ports the app actually needs; the database port isn't opened at all.
- Every write endpoint requires an API key; the dashboard is read-only with no endpoints that mutate data.
- Secrets (the demo API key, DB password) are placeholder dev values, deliberately not meant to guard anything sensitive — this is a demo project with synthetic data, not a production system handling real customer data.

## Roadmap (deferred)

- Split into true microservices
- RabbitMQ/Wolverine event bus (replacing the Postgres-backed outbox used in v1)
- Managed database (RDS) + proper secrets management (AWS Secrets Manager), Terraform for infrastructure-as-code
- Dead-letter queues, centralized metrics/tracing backend
- Partition sanctions/webhook workers by customer API key instead of a single shared rate-limit/circuit-breaker pool
- gRPC between services, SignalR live updates
