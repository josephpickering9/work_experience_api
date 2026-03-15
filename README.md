# Work Experience API

A .NET 8 REST API serving the [Work Experience](https://experience.josephpickering.co.uk) portfolio website. Built with clean architecture, a typed Result pattern, AI-powered search via Google Vertex AI, and automated CI/CD to Digital Ocean.

**Live API:** [api.experience.josephpickering.co.uk](https://api.experience.josephpickering.co.uk)
**Swagger UI:** [api.experience.josephpickering.co.uk/swagger](https://api.experience.josephpickering.co.uk/swagger)

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 / C# 12 |
| Database | PostgreSQL (EF Core + Npgsql) |
| Authentication | Auth0 (JWT Bearer) |
| AI Search | Google Vertex AI Discovery Engine |
| Validation | FluentValidation |
| Logging | Serilog (structured, console + file) |
| Image optimisation | Tinify |
| API Docs | Swagger / Swashbuckle |
| Testing | xUnit, Moq, NetArchTest, Coverlet |
| CI/CD | GitHub Actions |
| Hosting | Digital Ocean droplet |

---

## Architecture

```
Controllers  →  Services  →  Repositories  →  Database (EF Core / PostgreSQL)
                    ↓
              External APIs (Vertex AI, Tinify, Auth0)
```

### Key Patterns

**Result pattern** — every service method returns `Result<T>`. Controllers never throw; they map failures to RFC 7807 `ProblemDetails` responses via a single `result.ToResponse()` call.

```csharp
// Service
public async Task<Result<Project>> GetProjectAsync(ProjectId id)
{
    var project = await _repository.GetAsync(id);
    return project is null
        ? new NotFoundFailure<Project>("Project not found.")
        : new Success<Project>(project);
}

// Controller
[HttpGet("{id}")]
public async Task<IActionResult> GetProject(ProjectId id) =>
    (await _projectService.GetProjectAsync(id)).ToResponse();
```

**Repository pattern with base class** — `BaseRepository` centralises cache helpers and the PostgreSQL ILIKE / LINQ fallback so each concrete repository stays focused on its queries.

**Token-based cache invalidation** — `CacheInvalidator` issues `IChangeToken` instances per entity type. Write operations (add / update / delete) cancel the relevant token, atomically evicting all related cache entries without explicit key tracking.

**Typed IDs** — every entity uses a strongly-typed ID wrapper (e.g. `ProjectId`, `CompanyId`) backed by `Guid`, surfaced as plain UUIDs in the OpenAPI spec.

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) (for the local PostgreSQL instance)
- An [Auth0](https://auth0.com/) application and API
- *(Optional)* Google Cloud project with Vertex AI Discovery Engine for AI search

### Installation

1. **Clone the repository**

   ```bash
   git clone https://github.com/josephpickering9/work_experience_api.git
   cd work_experience_api
   ```

2. **Start the database**

   ```bash
   docker-compose -f WorkExperienceSearch/docker-compose.yml up -d
   ```

3. **Configure environment variables**

   Create a `.env` file in `WorkExperienceSearch/` (use `.env.example` as a reference):

   ```env
   DefaultConnection=Host=localhost;Port=5432;Database=work_experience;Username=postgres;Password=postgres
   Auth0__Domain=your-auth0-domain.auth0.com
   Auth0__ClientId=your-client-id
   Auth0__ClientSecret=your-client-secret
   Auth0__Audience=your-api-audience

   # Optional — required only for AI search
   VertexAi__ProjectId=your-gcp-project-id
   VertexAi__Location=global
   VertexAi__Collection=default_collection
   VertexAi__Branch=default_branch
   VertexAi__Environment=your-environment
   VertexAi__Model=gemini-1.5-flash-001/answer_gen/v1
   VertexAi__ModelLocation=global
   VertexAi__QueryDataStoreSuffix=your-datastore-suffix
   VertexAi__CredentialsFile=/path/to/service-account.json
   ```

4. **Apply database migrations**

   ```bash
   cd WorkExperienceSearch
   dotnet ef database update
   ```

5. **Run the application**

   ```bash
   dotnet run --project WorkExperienceSearch
   ```

   The API starts at `http://localhost:5105`. Swagger is available at `http://localhost:5105/swagger`.

6. **Check health**

   ```bash
   curl http://localhost:5105/health
   ```

---

## Testing

The test suite covers three levels:

| Type | Location | Tooling |
|---|---|---|
| Unit | `WorkExperienceSearchTests/Tests/Unit/` | xUnit, Moq, SQLite in-memory |
| Integration | `WorkExperienceSearchTests/Tests/Integration/` | `WebApplicationFactory`, real HTTP client |
| Architecture | `WorkExperienceSearchTests/Tests/Architecture/` | NetArchTest |

Run all tests:

```bash
dotnet test
```

Run with coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Architecture rules enforced

- Controllers must not reference Repository types directly
- Services must not reference Controller types
- Repositories must not reference Service or Controller types

---

## CI/CD

Two GitHub Actions workflows run on every push:

**`.github/workflows/dotnet-tests.yml`** — spins up a PostgreSQL container via Docker Compose, restores, builds, and runs the full test suite. Pull requests cannot be merged if this workflow fails.

**`.github/workflows/deploy.yml`** — triggers on `develop` branch merges. Publishes the app, uploads the artifact to the Digital Ocean droplet via SCP, runs `dotnet ef database update` on the server, and hot-swaps the running process with zero-downtime by symlinking the new release before restarting.

---

## Project Structure

```
WorkExperienceSearch/
├── Controllers/          # Thin HTTP layer — receive, delegate, respond
├── Services/             # Business logic and orchestration
├── Repositories/         # EF Core data access
├── Requests/             # Immutable record DTOs for all endpoints
├── Validators/           # FluentValidation validators (one per request type)
├── Models/               # EF Core entity models
├── Types/                # Result<T>, typed IDs, TagType enum
├── Exceptions/           # Typed exception hierarchy
└── Utils/                # String extensions (slug generation, etc.)

WorkExperienceSearchTests/
├── Tests/Unit/           # Service-level tests with mocked dependencies
├── Tests/Integration/    # Controller-level tests against a real HTTP server
└── Tests/Architecture/   # NetArchTest layer-boundary assertions
```
