# AGENTS.md — Pelican Town System (PTS)

## Project

Pelican Town System — municipal management platform inspired by Stardew Valley. Modular monolith evolving into microservices. C# 13 / .NET 9, ASP.NET Core, PostgreSQL, Redis, RabbitMQ, Clean Architecture + DDD.

## Workflow per phase

**Before implementing any phase**, read the phase's topic list in `ROADMAP.md` and critically review it:

1. **O que falta?** — Existe alguma feature relevante do jogo ou do domínio que o roadmap não listou? Adicione.
2. **O que pode ser melhorado?** — Os tópicos cobrem bem o conceito que a fase quer ensinar? Existe um edge case ou cenário real que enriquece a implementação?
3. **Quais padrões se aplicam?** — Essa fase é a oportunidade certa para introduzir um pattern de arquitetura (ex: Strategy, Factory, Observer) ou uma classe do framework .NET (ex: `Channel<T>`, `IHostedService`, `IDistributedCache`)?
4. **Como isso se conecta ao resto?** — Esse módulo publica ou consome eventos? Precisa de integração com módulos já existentes? Atualize o roadmap com esses eventos se necessário.

**After the review**, update `ROADMAP.md` with any new topics discovered, then implement. The review findings go in the `details/` file as part of "Alternativas consideradas" e "Pontos de atenção".

**Pacing**: a partir da Fase 1, implemente **um tópico por vez**. Após cada tópico:
1. Mostre o que foi feito (arquivos criados/modificados).
2. Gere o `.md` correspondente na pasta `details/`.
3. Marque `[x]` no `ROADMAP.md`.
4. Pergunte se pode prosseguir para o próximo tópico.

Isso garante que cada conceito seja compreendido antes de avançar.

## Prerequisites

```bash
dotnet --version          # .NET 9 SDK
docker compose version    # Docker + Compose v2
```

## Quick start

```bash
# One-command setup (infrastructure + migrations + seed data)
./scripts/setup.sh

# Or step by step:
# Start infrastructure (Postgres, Redis, RabbitMQ, Seq)
docker compose up -d

# Run full API (modular monolith)
dotnet run --project src/API/PelicanTown.Api

# Run a specific module in isolation (during development)
dotnet run --project src/Modules/Identity/Identity.Api
```

## Adding a new module

```bash
# Scaffold a new module with all 4 projects + test project
./scripts/scaffold-module.sh <ModuleName>

# Example: creates Crystalarium module with namespaces, DI registration, and tests
./scripts/scaffold-module.sh Crystalarium
```

The script generates:
- `src/Modules/<ModuleName>/<ModuleName>.Domain/` — Entity base, Value Objects, Aggregate root, Repository interface
- `src/Modules/<ModuleName>/<ModuleName>.Application/` — Commands, Queries, Handlers, DTOs, Behaviors
- `src/Modules/<ModuleName>/<ModuleName>.Infrastructure/` — EF Core DbContext with schema, Repository implementations
- `src/Modules/<ModuleName>/<ModuleName>.Api/` — Controller or Minimal API endpoints, DI extension (`Add<ModuleName>()`)
- `tests/<ModuleName>.Tests/` — Unit test project with xUnit + Moq + AutoFixture pre-configured

After scaffolding, register the module in `src/API/PelicanTown.Api/Program.cs` via `builder.Services.Add<ModuleName>()`, create the initial migration, and add seed data.

## Learning documentation (`details/`)

This project doubles as a learning resource. Every topic implemented **must** generate a companion `.md` file inside the `details/` directory explaining *what* was done, *why* it was done, and *how* it works.

**Idioma**: código-fonte em inglês; arquivos `.md` da pasta `details/` em português.

### Directory structure

```
details/
├── fase-0-fundacao/
│   ├── 01-shared-kernel.md              # Value Objects, Result<T>, BaseEntity, IDomainEvent
│   ├── 02-docker-compose.md             # PostgreSQL, Redis, RabbitMQ, Seq
│   ├── 03-serilog-seq.md                # Structured logging with correlation-id
│   ├── 04-swagger-openapi.md            # Swagger with Bearer config
│   ├── 05-scaffold-script.md            # Module scaffolding script
│   ├── 06-setup-script.md               # One-command setup script
│   ├── 07-seed-data-system.md           # ISeedData, canonical citizens, demo data
│   └── 08-github-actions-ci.md          # CI pipeline
├── fase-1-identity/
│   ├── 01-aspnet-identity.md            # ASP.NET Core Identity setup
│   ├── 02-jwt-auth.md                   # JWT + refresh token with rotation
│   ├── 03-rbac.md                       # Roles, permissions, policy-based auth
│   ├── 04-rate-limiting-login.md        # Rate limiting on auth endpoints
│   ├── 05-redis-refresh-tokens.md       # Redis as refresh token store
│   ├── 06-audit-authentication.md       # Login/logout/failure audit
│   └── 07-testing-identity.md           # Unit + integration tests
├── fase-2-infra/
├── fase-3-prefeitura/
├── ...
└── fase-34-cicd-deploy/
```

### File naming convention

`NN-topic-slug.md` where `NN` is sequential within the phase. Example: `03-rbac.md`, `07-testing-identity.md`.

### Content template

Each detail file follows this structure:

```markdown
# <Topic Name> — Fase X

## Contexto
Onde esse tópico se encaixa no projeto e na fase atual.

## Conceitos abordados
Lista de conceitos do ecossistema .NET que esse tópico cobre (ex: `IOptions<T>`, Middleware pipeline, CQRS, FluentValidation, etc.)

## Implementação

### Estrutura de arquivos criados/modificados
Tree view dos arquivos afetados.

### Trechos principais
Código comentado explicando cada decisão — não o arquivo inteiro, apenas os trechos que ensinam algo.

#### Exemplo: `Program.cs` — registro do módulo
Explica `builder.Services.AddIdentity()`, o que cada extensão faz, por que a ordem importa.

#### Exemplo: `RegisterCitizenCommand.cs` — padrão CQRS
Explica Command, Handler, validação via FluentValidation, retorno `Result<T>`.

### Fluxo de execução
Diagrama ASCII ou descrição passo a passo do que acontece quando um endpoint é chamado (request → middleware → controller → mediatr → handler → repository → db → response).

## Por que essa abordagem?
Justificativa da decisão de design comparada com alternativas comuns.

### Alternativas consideradas
| Alternativa | Por que não usei |
|---|---|
| Alternativa A | Motivo |
| Alternativa B | Motivo |

## Pontos de atenção
Armadilhas comuns, edge cases, ou coisas que não ficaram óbvias durante a implementação.

## Próximo tópico
Link para o próximo arquivo de detalhe na sequência da fase.
```

### Rules

- **Escrever durante a implementação, não depois.** Cada tópico concluído = um `.md` gerado.
- **Foco no "por que", não no "o que".** O código já mostra *o que* foi feito. O `.md` explica *por que* aquela abordagem foi escolhida.
- **Conectar com ecossistema .NET.** Sempre mencionar quais namespaces, classes ou padrões do framework estão sendo usados (ex: `IOptions<T>`, `IHttpContextAccessor`, `Channel<T>`).
- **Referenciar documentação oficial.** Links para Microsoft Learn quando relevante.
- **Manter atualizado.** Se refatorar algo em uma fase posterior, voltar e atualizar o `.md` da fase original com uma nota de "Atualizado na Fase X".

## Seed data

Every module includes seed data via `ISeedData` interface. The host calls `app.SeedAsync()` on startup (development only) to populate:
- Cidadãos canônicos: Lewis, Harvey, Clint, Robin, Gus, Willy, Rasmodius, Krobus, Marnie, Gunther, Pierre
- Roles e permissões para cada um
- Dados de demonstração por módulo (consultas agendadas, produtos em estoque, ocorrências, etc.)

This ensures the Swagger UI and Postman collection show realistic data immediately.

## Solution structure

```
PTS.sln
├── src/
│   ├── API/                          # Host project (wires all modules)
│   │   └── PelicanTown.Api/
│   ├── Modules/
│   │   ├── Identity/                 # pts-identity
│   │   │   ├── Identity.Domain/
│   │   │   ├── Identity.Application/
│   │   │   ├── Identity.Infrastructure/
│   │   │   └── Identity.Api/
│   │   ├── TownHall/                 # pts-town-hall
│   │   │   └── ...
│   │   ├── Clinic/                   # pts-clinica-harvey
│   │   │   └── ...
│   │   └── ...
│   ├── Shared/
│   │   ├── Kernel/                   # Value Objects, BaseEntity, IDomainEvent, Result<T>
│   │   └── Contracts/                # Integration event contracts (shared between modules)
│   └── BuildingBlocks/
│       ├── Auditability/             # Soft delete, audit log, LGPD helpers
│       ├── Caching/                  # Redis abstractions
│       ├── DistributedLocking/       # Redis RedLock abstractions
│       ├── EventStore/               # Event sourcing base (EventStoreContext, snapshots)
│       ├── FeatureFlags/             # Microsoft.FeatureManagement wrappers
│       ├── Messaging/                # MassTransit abstractions, Outbox helpers
│       ├── RealTime/                 # SignalR hub base and abstractions
│       ├── Resilience/               # Polly policy registry
│       └── Telemetry/                # OpenTelemetry configuration helpers
├── infra/                            # Terraform for Azure
├── details/                          # Learning documentation (one .md per topic implemented)
├── docker-compose.yml
└── .github/workflows/ci.yml
```

## Module conventions

Every module follows this internal structure:

```
<ModuleName>/
├── <ModuleName>.Domain/          # Entities, ValueObjects, Enums, DomainEvents, Aggregates, Repository interfaces
├── <ModuleName>.Application/     # Commands, Queries, DTOs, Handlers (MediatR), Behaviors, Interfaces for infrastructure
├── <ModuleName>.Infrastructure/  # EF Core DbContext, Repositories, External service clients, Messaging consumers
└── <ModuleName>.Api/             # Controllers / Minimal API endpoints, Contracts (Request/Response DTOs), Module registration DI
```

### Rules

- **No direct reference between modules.** Module A cannot reference Module B's `Domain` or `Application` directly.
- **Cross-module communication**: via integration events (MassTransit) or via interfaces defined in `Shared/Contracts/`. Only the `Api` host project wires implementations together via DI.
- **One PostgreSQL schema per module.** EF Core `DbContext` uses `.HasDefaultSchema("<module_name>")`. Migration history is per-module.
- **Shared Kernel**: Value Objects like `Money`, `Address`, `Cpf`, `Phone`, `Email` live in `Shared/Kernel/`. Modules reference this project freely.
- **Integration events** are defined in `Shared/Contracts/` as POCO records. Both publisher and consumer reference this project.

## Code conventions

### Naming

- **Projects**: `PascalCase` — `Identity.Domain`, `Clinic.Api`
- **Namespaces**: `PelicanTown.Modules.<ModuleName>.<Layer>` — `PelicanTown.Modules.Identity.Domain`
- **Entities**: singular PascalCase — `Citizen`, `Appointment`, `Product`
- **Tables**: plural snake_case — `citizens`, `appointments`, `products`
- **Columns**: snake_case — `created_at`, `citizen_id`, `tax_amount`
- **Value Objects**: PascalCase — `Money`, `Address`, `Cpf`
- **Domain Events**: past-tense verb — `CitizenRegistered`, `ConsultationScheduled`, `ProductSold`
- **Commands**: imperative verb — `RegisterCitizen`, `ScheduleConsultation`, `SellProduct`
- **Queries**: `Get<Thing>` — `GetCitizenById`, `GetUpcomingAppointments`
- **DTOs / Contracts**: suffix is implicit from context — `CitizenResponse`, `CreateCitizenRequest`
- **Interfaces**: `I` prefix — `ICitizenRepository`, `IEmailSender`
- **Async methods**: `Async` suffix — `GetByIdAsync`, `SaveChangesAsync`
- **Private fields**: `_camelCase` — `_citizenRepository`, `_logger`

### C# style

- `record` for Value Objects and DTOs; `class` for Entities and Aggregates.
- `sealed` on all classes by default, unless designed for inheritance.
- `required` keyword on non-nullable reference type properties in DTOs.
- Primary constructors for simple services and controllers.
- Expression-bodied members for single-statement methods.
- Nullable reference types enabled in all projects (`<Nullable>enable</Nullable>`).
- Implicit usings enabled. Global usings in `GlobalUsings.cs` per project.
- File-scoped namespaces everywhere.
- No regions. No `#nullable disable`.
- Minimal comments. Code should be self-documenting. Comments only for "why", never "what".
- **Zero code comments.** Toda explicação do código vai nos arquivos `.md` da pasta `details/`, não em comentários inline.

### Error handling

- Domain errors use `Result<T>` pattern from Shared Kernel (`Result<T>.Success(value)`, `Result<T>.Failure(error)`).
- Application layer returns `Result<T>` from handlers. API layer maps `Result<T>` to HTTP responses via a `ResultExtensions` helper.
- Exceptions only for truly exceptional cases (infrastructure failures, programming errors).
- No `try-catch` for control flow.

### Validation

- Input validation with FluentValidation (`AbstractValidator<T>`) on all request DTOs.
- Domain invariants enforced inside Entity/ValueObject constructors — throw `DomainException` for violations.
- Validation pipeline behavior (MediatR) automatically validates commands/queries via FluentValidation.

### EF Core

- `AsNoTracking()` on all read queries.
- `SplitQuery()` when eager-loading multiple collections.
- `Select()` projections (no cartesian explosion).
- Cursor-based pagination, not offset.
- Optimistic concurrency via `[Timestamp]` or `xmin` column.
- All dates stored as UTC (`DateTime.UtcNow`). `DateTimeOffset` for audit fields.

### DI

- Modules expose an `Add<ModuleName>()` extension method on `IServiceCollection` (in the `Api` project of the module).
- Infrastructure projects expose `Add<ModuleName>Infrastructure()` for DB context and external service registration.
- No Service Locator pattern. No `GetRequiredService` outside composition root.

## Testing

### Unit tests

- xUnit + Moq + AutoFixture + FluentAssertions.
- Test one handler/entity/service at a time.
- Mock external dependencies (repositories, message bus, external APIs).
- Domain logic tests must not touch the database.

### Integration tests

- `WebApplicationFactory<T>` with custom `IWebHostBuilder` — real PostgreSQL (Testcontainers), real Redis (Testcontainers), real RabbitMQ (Testcontainers).
- Each module has an `IntegrationTests` project (`Tests/<ModuleName>.IntegrationTests`).
- Test data: seed per test, not shared across tests. Use `Respawn` or truncate per test class.
- Test naming: `<Method>_<Scenario>_<ExpectedBehavior>` — `ScheduleAppointment_WhenSlotTaken_ReturnsConflict`.

### Contract tests

- PactNet (consumer-driven) between extracted microservices after Fase 31.
- Provider verification in CI pipeline.

### Load tests

- k6 scripts in `tests/load/`.
- Target: p95 latency < 500ms for read endpoints, < 2s for write endpoints under 200 concurrent users.
- Run before major milestones (Fase 31, Fase 39).

## Build & CI

```bash
# Build entire solution
dotnet build

# Run all tests
dotnet test

# Run tests for a specific module
dotnet test tests/Clinic.IntegrationTests

# Format check
dotnet format --verify-no-changes

# Apply formatting
dotnet format

# Database update (per module, during development)
dotnet ef database update --project src/Modules/Identity/Identity.Infrastructure
```

CI pipeline (GitHub Actions):
1. `dotnet restore`
2. `dotnet format --verify-no-changes`
3. `dotnet build --no-restore --configuration Release`
4. `dotnet test --no-build --configuration Release`
5. (On merge to main) Docker build + push to ghcr.io

## Docker

```bash
# Start all infrastructure
docker compose up -d

# Build and run the monolith locally
docker compose up --build api

# After microservices extraction (Fase 31), each service has its own Dockerfile
docker compose up --build identity-api clinic-api jojamart-api gateway
```

## Git conventions

- Branches: `feature/<module>-<description>`, `fix/<description>`, `refactor/<module>-<description>`
- Commits: conventional commits — `feat(identity): add refresh token rotation`
- PR title must match commit convention.
- No direct commits to `main`. All changes via PR with CI green.

## Key architectural decisions (ADRs)

These will be documented in `ARCHITECTURE.md` by Fase 40. Keep them in mind while building:

1. **Modular monolith first, microservices later** — domain boundaries validated in-process before network boundaries are introduced.
2. **Database-per-service (schema-per-module in monolith)** — no shared tables between modules, enforced since Fase 0.
3. **Async communication dominates** — modules communicate via events (MassTransit/RabbitMQ in dev, Azure Service Bus in prod). Sync calls (gRPC) only when latency-critical and unavoidable.
4. **Identity extracted first** — authentication is the most coupled dependency; extracting it validates the microservices strategy.
5. **Event Sourcing only where it makes sense** (Museu) — not all modules need it. Trade-off documented explicitly.
6. **Multi-tenancy via schema-per-tenant** — isolation at DB level, same code-base serves JojaMart and Pierre.
7. **Infrastructure as Code from Fase 39** — no ClickOps in Azure. Everything provisioned via Terraform.

## Stack summary

| Concern | Dev/Local | Staging/Prod |
|---|---|---|
| Runtime | .NET 9 | Container Apps (Azure) |
| Database | PostgreSQL (container) | Azure DB for PostgreSQL |
| Cache | Redis (container) | Azure Cache for Redis |
| Messaging | RabbitMQ (container) | Azure Service Bus |
| Logs | Seq (container) | Application Insights |
| Metrics | Prometheus + Grafana (container) | Application Insights |
| Secrets | User Secrets | Azure Key Vault |
| Files | Local disk / Azurite | Azure Blob Storage |
| CI/CD | GitHub Actions | GitHub Actions + OIDC |
| IaC | — | Terraform |

## Do's

- Reference the ROADMAP.md when implementing features to understand which phase you're in and what comes before/after.
- **Before implementing a phase, run the review checklist** described in "Workflow per phase" — question the roadmap, find gaps, propose additions.
- Write tests alongside production code, not after.
- Use `Result<T>` for all application handler return types.
- Keep modules isolated — no `using` statement that crosses module boundaries (except Shared Kernel and Contracts).
- **Generate a `details/` `.md` file for every topic implemented**, following the template in the "Learning documentation" section.
- Add cheat sheets to this file as patterns emerge (e.g., "how to add a new module" section is welcome).

## Don'ts

- Do not add a NuGet package without explicit permission, unless it's already in the stack summary above.
- Do not generate XML doc comments on every method. Use them only on public APIs (controllers, published NuGet packages).
- Do not add inline comments to explain code. Explicações detalhadas pertencem aos arquivos `.md` do diretório `details/`.
- Do not create a database migration across module schemas. One migration = one module.
- Do not use `dynamic`, `object` as return types, or `#nullable disable`.
- Do not introduce a new concept (Event Sourcing, SignalR, multi-tenancy) before its respective phase in ROADMAP.md.
