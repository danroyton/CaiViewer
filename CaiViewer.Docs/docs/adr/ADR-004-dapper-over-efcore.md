# ADR-004: Database Access – Dapper over EF Core
## Status: Accepted
## Date: 2025-08

---

## Context

Two ORM / data access options were considered for the SQLite database:

| Option | Pros | Cons |
|---|---|---|
| **EF Core + SQLite provider** | LINQ queries, migrations built-in, less boilerplate for CRUD | FTS5 queries require raw SQL fallback; heavier dependency; WASM support limited |
| **Dapper + raw SQL** | Full control over FTS5 MATCH syntax, minimal overhead, tiny binary, works identically in WASM | More boilerplate for mapping; migrations must be written manually |

The critical query path — full-text search via SQLite FTS5 — requires non-standard SQL (`MATCH`, `bm25()`, `highlight()`) that EF Core cannot generate via LINQ.  
The database schema is largely read-heavy (Import writes, then Browse is almost entirely reads).

## Decision

Use **Dapper** with **raw parameterized SQL**. Schema migrations are managed via a simple `MigrationRunner` that applies numbered `.sql` files from an embedded resource folder.

No EF Core dependency in `CaiViewer.Core`.

## Consequences

- **Positive:** FTS5 queries are written exactly as needed with no ORM translation layer.
- **Positive:** `Microsoft.Data.Sqlite` + `Dapper` together add ~300 KB to the binary; suitable for WASM.
- **Positive:** Migrations are plain SQL files, easily reviewed and version-controlled.
- **Negative:** No automatic change tracking; callers must call explicit update methods.
- **Mitigation:** Repository interfaces in `CaiViewer.Core/Database/Repositories/` enforce a clean boundary so the data access implementation can be swapped without affecting ViewModels.
