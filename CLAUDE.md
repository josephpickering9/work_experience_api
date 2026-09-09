# Work Experience Search — Backend (.NET API)

## Code Style
Code should be intuitive enough to read without comments. Default to writing no comments at all. Only add one when there's vital context a reader can't get from the code itself — a non-obvious constraint, a workaround for a specific bug, a subtle invariant. Never write a comment that restates what the code already says. When editing existing code, remove comments that don't meet this bar rather than leaving them.
- No commented-out code — delete it. Git has history.
- No TODO comments in merged code.
- No XML doc comments on internal methods.

## Layering
`Controller → Service → Repository → EF Core (Database)`. This is enforced by `WorkExperienceSearchTests/Tests/Architecture/ArchitectureTests.cs` (NetArchTest rules) — Controllers can't reference Repositories directly, Services can't reference Controllers, Repositories can't reference Services/Controllers. Keep new code inside these boundaries; if a test starts failing after adding a dependency, that's the architecture test catching a layering violation, not a bug in the test.

**Primary constructors for DI everywhere** — no `readonly` field boilerplate:
```csharp
public class ProjectController(IProjectService projectService) : ControllerBase
public class ProjectService(IProjectRepository projectRepository, ITagService tagService) : IProjectService
```

**`Result<T>` pattern, not exceptions, for expected failures.** `Types/Result.cs` defines `Success<T>`, `NotFoundFailure<T>`, `ConflictFailure<T>`, `BadRequestFailure<T>`, etc. Controllers call `.ToResponse()` on the service result and never branch on errors themselves:
```csharp
// Service
public async Task<Result<Project>> GetProjectAsync(ProjectId id)
{
    var project = await projectRepository.GetAsync(id);
    if (project == null) return new NotFoundFailure<Project>("Project not found.");
    return new Success<Project>(project);
}

// Controller
[HttpGet("{id:guid}")]
public async Task<ActionResult<Project>> GetProject(ProjectId id) =>
    (await projectService.GetProjectAsync(id)).ToResponse();
```
Real exceptions (`Exceptions/NotFoundException`, `ConflictException`) still exist for genuinely unexpected failures and are caught globally by `UseExceptionHandler` in `Program.cs` — don't use them to signal expected/handled failures.

**Strongly-typed IDs.** Every domain entity ID (`ProjectId`, `CompanyId`, `TagId`, etc.) is a `readonly record struct` wrapping `Guid`, defined in `Types/Ids.cs`, with a custom `TypeConverter`, JSON converter, and EF `HasGuidIdConversion()` value conversion. Never use a raw `Guid` for a domain entity's primary or foreign key — add a new ID type in `Types/Ids.cs` when adding a new entity.

**Repository reads use `IMemoryCache`.** `BaseRepository` wraps caching; concrete repositories build cache keys manually and invalidate via `CacheInvalidator` (`IChangeToken` per entity type) on writes — see `Repositories/Project/ProjectRepository.cs`. When adding a new query method that should be cached, follow the existing key-building convention in the same repository rather than inventing a new scheme.

**Async conventions:** all repository/service methods are `async Task<...>`; repository methods accept `CancellationToken cancellationToken = default`. Never `.Result` or `.Wait()` on a `Task`.

## Code quality standing rules

Established by an audit-driven cleanup pass (see `docs/CODE_QUALITY_BACKLOG.md` at the monorepo root) — code review should hold new code to these, not just legacy code:

- **File size cap ~500-600 lines.** Split a service by responsibility or extract a sub-component before a file grows past this. No automated check for this yet (`.NET` has no built-in line-count analyzer) — a code-review convention, same as the comment-policy rules above.
- **Method parameters: max 5-6.** Beyond that, introduce a dedicated request/DTO class instead of growing the parameter list further.
- **Repository reads use `.AsNoTracking()`** unless the returned entity is mutated and saved later in the same call (e.g. an update flow that fetches, mutates, then calls `SaveChangesAsync` without an explicit `Update()`/`Entry().State` call — EF needs the tracked instance there). When adding a new read method, default to `.AsNoTracking()` and only drop it if you can point to the specific write path that needs tracking.
- **LINQ over raw SQL in repositories.** `FromSqlRaw` is a last resort for queries EF Core genuinely can't express — not a convenience default. If you do need it, parameterize it (never string-interpolate values into the SQL).
- **Entity → DTO mapping via a method, not inline construction.** No dedicated mapper class exists in this codebase; the convention is a private (usually `static`) mapping method on the service that needs it (see `VertexIngestService.FlattenProject`/`FlattenCompany`/`FlattenTag`, `VertexProjectDescriptionService.ToResponse`). Don't construct a DTO field-by-field at the call site — extract a mapping method even for single-use cases.
- **Top-of-file `using` directives over inline fully-qualified type references.** Add the `using` rather than writing `System.Text.Json.JsonSerializer.Serialize(...)` inline.
- **Structured logging on failure paths.** Inject `ILogger<T>` via the primary constructor and log `Result<T>` failures (`LogWarning`, with the entity ID as a named property, e.g. `logger.LogWarning("Tag {TagId} not found.", id)`) and caught exceptions (`LogError`, with the exception). Don't log the happy path — match the restraint already shown in `Services/VertexAi/`. Never string-interpolate log messages; always named properties, so they stay queryable as structured fields.
- **No magic values.** A literal with business meaning (a status string, a numeric limit) should be a named constant or enum member, not a bare literal at the call site.

## Structure
- `Controllers/` — thin, one action per route, no business logic.
- `Services/<Domain>/` — business logic, one interface + implementation per domain (`Services/Project/`, `Services/Company/`, `Services/VertexAi/`).
- `Repositories/<Domain>/` — EF Core queries, one per domain; `BaseRepository.cs` at root for shared caching behaviour.
- `Models/` — EF entities.
- `Requests/` — record DTOs for inbound requests, one file per domain.
- `Validators/` — FluentValidation, one validator per request type.
- `Filters/` — Swagger/OpenAPI filters.
- `Exceptions/` — `NotFoundException`, `ConflictException` for unexpected-failure paths only.
- `Types/` — `Ids.cs` (strongly-typed IDs), `Result.cs`.
- `Utils/` — extension methods.
- `Migrations/` — EF Core migrations, never hand-edited (see below).

## Migrations
Never create migration files manually. Always run `dotnet ef migrations add <Name>` from `Work Experience Search/WorkExperienceSearch/`. Manual migrations skip the `.Designer.cs` snapshot and cause EF to block `database update` with a `PendingModelChangesWarning`. Apply with `dotnet ef database update`.

## Testing
xUnit + Moq + `NetArchTest.Rules`, tests live in `WorkExperienceSearchTests/Tests/`:
- `Unit/Services/` — instantiate real repositories against EF Core's `InMemoryDatabase` provider (not SQLite, despite what older docs may say), mock only sibling services. Seeded via `BaseServiceTests : IAsyncLifetime`.
- `Integration/` — `CustomWebApplicationFactory` + real `HttpClient` against a real Postgres instance (`docker-compose.yml`, port 5433).
- `Architecture/` — layering rules described above.

Every new service method needs at minimum a happy-path and a not-found/failure-path test.

## Style rules (`.editorconfig`)
4-space indent, UTF-8 BOM, trailing whitespace trimmed, `var` preferred everywhere, interfaces must be `I`-prefixed. `is null` / `is not null` — never `== null`.

## Running locally
```
docker-compose -f WorkExperienceSearch/docker-compose.yml up -d   # Postgres
dotnet ef database update --project WorkExperienceSearch
dotnet run --project WorkExperienceSearch                          # http://localhost:5105, Swagger at /swagger
```
`.env` (via `dotenv.net`) supplies `DefaultConnection`, `Auth0__*`, `VertexAi__*` — see `README.md` for the full list. Never commit `.env`.

## Auth
Auth0 JWT Bearer scheme. `[Authorize]` on write endpoints.

## Docs
`README.md` at this project's root has the fuller getting-started guide and CI/CD notes — read it alongside this file.
