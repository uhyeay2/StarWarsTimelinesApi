# StarWarsTimelinesApi

ASP.NET Core Web API (net10.0) for the Star Wars Timelines app, built with Clean Architecture / Hexagonal design. Implements an admin-managed `SourceMaterial` catalog, JWT bearer auth, per-user library tracking via a `UserSourceMaterial` mapping table (with per-unit progress for the sub-units of shows, books, comics, and games), and a timeline of `SourceMaterialEvent`s linked to `Character`/`Location`/`Vehicle` lookups, with EF Core + SQLite. Mirrors the `OpenCodeCrudApi` reference project.

## Project structure

```
src/
  StarWarsTimelines.Api/          Delivery layer: minimal API endpoints, JWT auth, DI wiring, appsettings
  StarWarsTimelines.Application/  Ports (interfaces) + use cases: SourceMaterialService, AuthService, TokenService, LibraryService, CharacterService, LocationService, VehicleService, SourceMaterialEventService, SourceMaterialUnitService, DTOs
  StarWarsTimelines.Domain/       Core domain: entities and enums only, no dependencies
  StarWarsTimelines.Persistence/  Adapters: AppDbContext (EF Core/SQLite), repositories, SeedData, migrations
tests/
  StarWarsTimelines.Application.Tests/  Unit tests for services (Moq for ports)
  StarWarsTimelines.Api.Tests/          Integration tests via WebApplicationFactory + temp SQLite DB
```

Dependency rule: `Api -> Application, Persistence`, `Persistence -> Application, Domain`, `Application -> Domain`. Domain has no references. The Application layer knows nothing about EF Core or HTTP.

## Domain model

- `SourceMaterial`: admin-managed catalog lookup (Id, Title, Medium, CanonType). Owns its sub-units via `SourceMaterialUnits`; the collection is only populated when a query explicitly includes it (library reads do; plain catalog reads do not).
- `SourceMaterialUnit`: a sub-unit of a source material (Id, SourceMaterialId, UnitType, Number, Title?, CreatedAtUtc). Unique index (SourceMaterialId, Number) so ordering is unambiguous; cascade delete from the parent material.
- `User`: Id, Username (unique), DisplayName, PasswordHash, Role, CreatedAtUtc. Password hashes use `PasswordHasher<object>`.
- `UserSourceMaterial`: composite key (UserId, SourceMaterialId); per-user TrackingStatus, IsFavorite, timestamps. Cascade deletes both ways. When the tracked material has sub-units, the stored status is ignored and the reported status is **derived** from unit progress instead (see below).
- `UserSourceMaterialUnit`: composite key (UserId, SourceMaterialUnitId); per-unit IsCompleted + UpdatedAtUtc. A row exists only once the user has explicitly set progress, so absence means "not started". Cascade deletes both ways.
- `Character`, `Location`, `Vehicle`: admin-managed lookup catalogs (Id, Name, unique name). No inverse collections back to events.
- `SourceMaterialEvent`: timeline entry (Id, Title, Description, CanonType, Year, DisplayDate, DisplayDateEnd?, SourceMaterialId). Owns its many-to-many links via `EventCharacter`/`EventLocation`/`EventVehicle` (composite keys, cascade both ways). `SourceMaterialEvent -> SourceMaterial` cascades (deleting a material removes its events), matching the `UserSourceMaterial` rule.
- Enums: `Medium` (7), `TrackingStatus` (InProgress, Completed, WishListed), `CanonType` (Canon, Legends, CanonAndLegends), `UserRole` (Standard, Admin), `UnitType` (Episode, Chapter, Issue, Level). Stored as strings via `HasConversion<string>()`.

## Auth & authorization

- JWT bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`). `JwtOptions` bound from the `Jwt` section of appsettings (Issuer, Audience, SecretKey, ExpiryMinutes).
- `TokenService` emits Sub, UniqueName, and Role claims; default inbound claim mapping lets handlers use `FindFirstValue(ClaimTypes.NameIdentifier)` and `IsInRole("Admin")`.
- Login: `POST api/auth/login` -> `AuthResponse { Token, User }`.
- Writes to the catalogs (`POST/PUT/DELETE api/source-materials`, `api/characters`, `api/locations`, `api/vehicles`, `api/source-material-events`, and `api/source-materials/{id}/units`) require the `"AdminOnly"` policy (`RequireRole("Admin")`); reads are anonymous.
- Library endpoints (`api/users/{userId}/source-materials`) require authentication and a self-or-admin check (`CanAccessLibrary` in `LibraryEndpoints.cs`). Unit progress is set with `PUT api/users/{userId}/source-materials/{sourceMaterialId}/units/{unitId}` and cleared (unit plus its children; removes the library entry when no progress remains) with `DELETE` on the same path. A single tracked item with its per-unit progress is fetched with `GET api/users/{userId}/source-materials/{sourceMaterialId}`, which the web app uses to refresh one item after mutations.
- Status derivation: for a material with sub-units, `LibraryService.DeriveStatus` computes the reported status from unit progress at read time — no completed units → `WishListed`, some → `InProgress`, all → `Completed`. Materials without units keep their manually tracked status. `PUT` on a library item that sets a status for a unit-based material is rejected with `400 Bad Request` (`ArgumentException`, `request.Status`); setting only `IsFavorite` is allowed.
- Validation errors (`ArgumentException` from services) surface as 400 Bad Request via the global `ApiExceptionHandler` (`src/StarWarsTimelines.Api/ApiExceptionHandler.cs`).
- CORS: enabled for the origins in `Cors:AllowedOrigins` (`appsettings.json`); the default is `http://localhost:4200` (Angular dev server). QA/Prod must add their hosted web origins there. Public lookup endpoints (`api/source-materials`, `api/characters`, `api/locations`, `api/vehicles`) are anonymous, so the web app can fetch them without a token.

## Seed data

`Persistence/SeedData.cs` runs idempotently after migrations at startup (skips if tables are non-empty). Seeds 4 users (admin/admin123, padme/padme123, luke/luke123, rey/rey123 with fixed GUIDs `11111111-…`/`22222222-…`/`33333333-…`/`44444444-…`), 22 catalog items with fixed GUIDs `00000000-…-000000000001..022`, sample library rows for padme/luke, a 21-event timeline (movies, plus whole-book events and events tied to a specific season/episode, volume/issue, or chapter/level), and representative sub-units (`…5000000000NN`) for a handful of materials: The Clone Wars seasons 1-7 (24 episodes), The Mandalorian season 1 (Episodes 1-8), Star Wars: Rebels seasons 1-2, Ahsoka season 1 (Episodes 1-6), Dawn of the Jedi volumes 1-2 (Issues 1-3 each), Light of the Jedi Chapters 1-3, Shatterpoint Chapters 1-3, and Jedi: Fallen Order Levels 1-3. Sample progress marks padme's first three first-season Clone Wars episodes as completed. Lookup entries (characters `…1000000000NN`, locations `…2000000000NN`, vehicles `…3000000000NN`) and events (`…4000000000NN`) use deterministic GUIDs assigned in order of first appearance across `SeedData.TimelineEvents`; events and units reference source materials by their 1-based catalog sequence. Because the local `starwarstimelines.db` is a committed-agnostic file (gitignored) that only reseeds when empty, seed changes require deleting it once so it regenerates with the updated catalog. Integration tests that rely on seeded data must restore it per test (see `LibraryEndpointsTests.ResetLibraryToSeed` and `ResetUnitProgressToSeed`).

## Commands

- Build: `dotnet build StarWarsTimelinesApi.slnx`
- Test: `dotnet test StarWarsTimelinesApi.slnx`
- Run: `dotnet run --project src/StarWarsTimelines.Api` (listens per launchSettings.json; applies EF migrations + seeds at startup)
- Add a migration: `dotnet tool run dotnet-ef migrations add <Name> --project src\StarWarsTimelines.Persistence --startup-project src\StarWarsTimelines.Api --output-dir Migrations`
- Update DB: `dotnet tool run dotnet-ef database update --project src\StarWarsTimelines.Persistence --startup-project src\StarWarsTimelines.Api`
- Migration regeneration: delete the `Migrations` folder first, then re-run `migrations add InitialCreate`.

## Conventions

- Hexagonal architecture: Domain entities and ports (`I*Repository`, `IUnitOfWork`) in Application; EF Core lives only in Persistence.
- Minimal API endpoints grouped in `src/StarWarsTimelines.Api/Endpoints/`: `SourceMaterialEndpoints.cs`, `AuthEndpoints.cs`, `LibraryEndpoints.cs`, `CharacterEndpoints.cs`, `LocationEndpoints.cs`, `VehicleEndpoints.cs`, `SourceMaterialEventEndpoints.cs`, `SourceMaterialUnitEndpoints.cs`. New resource endpoints follow the map-group pattern. Sub-resources use nested groups (units live under `api/source-materials/{id}/units`).
- Controllers are NOT used.
- DTOs are immutable records in `Application/Dtos`; responses map from domain entities via a static `FromEntity`.
- EF Core configuration via `IEntityTypeConfiguration` classes in `Persistence/Configurations`.
- Data-fetching best practices: lazy loading is NOT enabled; reads use `AsNoTracking()` and include related data only where required via explicit `Include`. Lookup catalogs (`Character`, `Location`, `Vehicle`) deliberately have no inverse collections; `SourceMaterial` only exposes `SourceMaterialUnits` (populated by library reads, never by plain catalog reads). `SourceMaterialEventRepository` reads include the source material and all links; its tracked read (`GetByIdTrackedAsync`) is used only when an event's link collections must be edited before saving — link replacement clears/re-adds the tracked collection so the change tracker deletes removed rows and inserts added ones. `LibraryService` maps units into responses by combining the material's `SourceMaterialUnits` (from the library include) with the user's `UserSourceMaterialUnit` rows (from `IUserSourceMaterialUnitRepository`), and derives the reported status from that progress for unit-based materials (`LibraryItemResponse` carries the derived status, not the stored one).
- XML documentation is mandatory: `GenerateDocumentationFile` is enabled on all `src` projects (CS1591 enforces docs on public members). Every class, enum member, property, record parameter, method, and private method carries an XML doc comment; interface implementations use `<inheritdoc />`.
- The API applies pending migrations + seed data automatically at startup (`dbContext.Database.Migrate()` + `SeedData.Seed(dbContext)` in `Program.cs`).
- Swagger via Swashbuckle (Development only). UI: `/swagger/index.html`.
- Tests: xUnit. Service unit tests use **Moq** (see `Application.Tests/*ServiceTests.cs`). Integration tests use `WebApplicationFactory<Program>` with a unique temp SQLite file and reset tables per test. Integration tests inherit `ApiTestBase`, which provides `CreateClientAsAsync(username, password)` login helpers for admin/standard clients.
- Enums (`Medium`, `TrackingStatus`, `CanonType`, `UserRole`, `UnitType`) serialize with default System.Text.Json numeric handling.
- Write endpoints rely on the global `ApiExceptionHandler` to map `ArgumentException` (blank names, missing referenced ids) to `400 Bad Request`; there is no per-endpoint try/catch.
- Connection string: `ConnectionStrings:Default` in `src/StarWarsTimelines.Api/appsettings.json` (SQLite file `starwarstimelines.db`).
- When schema changes: delete the `Migrations` folder and regenerate `InitialCreate` rather than adding incremental migrations.
