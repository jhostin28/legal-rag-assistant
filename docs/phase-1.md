# Phase 1 — API: upload and list documents

> **Status:** in progress.

## What was built so far

- ASP.NET Core Web API project on .NET 10, using controllers.
- `Document` and `Law` models mirroring the Phase 0 design (see [phase-0.md](phase-0.md)).
- `GET /documents`: 200 with every document.
- `GET /documents/{id}`: 200 with the document, 404 if it does not exist.
- `POST /documents`: the client sends a `CreateDocumentRequest` (no id);
  PostgreSQL assigns the id and the API returns 201 with a `Location` header.
  A missing required field is rejected with 400 by `[ApiController]` before the
  method runs.
- `LegalRag.Api.http`: manual requests for every endpoint, kept in the repo.
- `docker-compose.yml`: local PostgreSQL 17 with pgvector, on host port 5433.
- EF Core with the Npgsql provider, code-first. `AppDbContext` exposes the
  `documents` and `laws` tables; the connection string lives in
  `appsettings.Development.json` (development-only credentials).
- Snake_case naming (`EFCore.NamingConventions`), so tables and columns match
  the Phase 0 design and need no quoting in SQL.
- Migrations applied:
  - `InitialCreate`: the `documents` table.
  - `AddLawsAndRules`: the `laws` table, `UNIQUE (name)`, the foreign key
    `documents.law_id → laws.id ON DELETE RESTRICT`, and
    `UNIQUE (law_id, effective_date)`. The rules live in
    `AppDbContext.OnModelCreating`.
- Data survives a server restart: the in-memory list was replaced by PostgreSQL.

**Next:** a way to create laws (`POST /laws`). Until then, `POST /documents`
cannot succeed, because every document needs an existing law.

## Problems found

Real problems hit while building this phase.

### 1. Build failed: "file is locked by another process"
- **Symptom:** `dotnet run` failed with `MSB3027: Could not copy ... apphost.exe ...
  The file is locked by: "LegalRag.Api (22224)"`.
- **Cause:** a previous `dotnet run` was still running in another terminal. While
  a process runs, Windows locks its `.exe`, so the build cannot replace it.
- **Diagnosis:** the error message itself named the process holding the file and
  its process id (22224).
- **Fix:** stopped that process (`Stop-Process -Id 22224`) and ran again.
- **Lesson:** read the full error before guessing; it often names the culprit.
  Stop a running server (Ctrl+C) before starting a new one.

### 2. `git push` rejected: "Password authentication is not supported"
- **Symptom:** the commit succeeded locally, but `git push` failed with
  `Invalid username or token. Password authentication is not supported`.
- **Cause:** GitHub no longer accepts account passwords for Git operations, and
  the sign-in dialog had been closed before finishing.
- **Fix:** pushed again and signed in through the browser.
- **Lesson:** a commit and a push are separate steps; a failed push leaves the
  commit safe locally. Git authentication uses tokens or browser sign-in,
  not passwords.

### 3. Compiler warning CS8618 on `SourceUrl`
- **Symptom:** `Non-nullable property 'SourceUrl' must contain a non-null value
  when exiting constructor`.
- **Cause:** `string` (without `?`) promises the value is never null, but nothing
  forced a value to be set. `int` and `DateOnly` have defaults; a `string` starts
  as null.
- **Options considered:** `string?` (contradicts the NOT NULL column),
  `= string.Empty` (hides a missing value), `required`.
- **Fix:** `public required string SourceUrl { get; set; }`, the C# equivalent of
  a NOT NULL column without a DEFAULT: whoever creates a `Document` must set it.
- **Lesson:** keep the model's nullability consistent with the database schema.

### 4. Started on a framework version about to lose support
- **Symptom:** the installed SDK was .NET 9.
- **Cause:** .NET 9 is a Standard Term Support release that reaches end of
  support on November 10, 2026.
- **Fix:** installed the .NET 10 SDK (Long Term Support, supported until
  November 2028) before creating the project.
- **Lesson:** check a framework's support lifecycle before starting a project;
  migrating later costs more than choosing right at the start.

### 5. `docker compose up` failed: "failed to connect to the docker API"
- **Symptom:** `unable to get image ... failed to connect to the docker API at
  npipe:////./pipe/dockerDesktopLinuxEngine ... check if the daemon is running`.
- **Cause:** Docker Desktop was closed. The `docker` command is only a client;
  the daemon (the engine that runs containers) starts with Docker Desktop.
- **Fix:** opened Docker Desktop, waited for "Engine running", ran it again.
- **Lesson:** the error said exactly what to check ("is the daemon running").
  A client/server tool fails like this whenever the server side is down.
  The same root cause later showed up from the API side as
  `Failed to connect to 127.0.0.1:5433 ... actively refused it`: the database
  container was not running.

### 6. Image download failed: connections reset by the network card's software
- **Symptom:** `docker pull` failed with `failed to fetch oauth token ... EOF`,
  and later with `TLS handshake timeout`, after some layers had downloaded.
- **Diagnosis, layer by layer:**

  | Test | Result | Rules out |
  |---|---|---|
  | `curl.exe -I https://auth.docker.io/token` | Connection reset | Docker itself |
  | `curl.exe -I https://www.google.com` | Connection reset | Only Docker Hub |
  | `nslookup auth.docker.io` | Real Cloudflare IPs | DNS / sinkhole |
  | `curl.exe -4 -I https://www.google.com` | Connection reset | IPv6 |
  | `curl.exe -I https://github.com` | 200 OK | A full outage: something filters |
  | `netsh winhttp show proxy` | Direct access | A system proxy |
  | Antivirus list (SecurityCenter2) | Only Windows Defender | Third-party antivirus |
  | `Get-NetAdapter` | No VPN adapter | A VPN |
  | Same test on a phone hotspot | Still failing | Router and ISP: it is the PC |
  | `Get-Service *killer*` | Two Killer services running | (suspect found) |

- **Cause:** the Killer network software (Killer Network Service and Killer
  Analytics Service) filters traffic at the driver level and was cutting some
  TLS connections from the terminal, on any network.
- **Fix:** stopped both services; `curl` returned 200 at once and the image
  downloaded. Then set them to `Disabled` so they do not start after a reboot.
  The network driver itself is unaffected.
- **Lesson:** change one variable at a time and let each test rule something
  out. Switching networks was the test that moved the search from the network
  to the PC.

### 7. Migration failed after switching to snake_case: `column "migration_id" does not exist`
- **Symptom:** after enabling `UseSnakeCaseNamingConvention()` and adding a
  second migration, `dotnet ef database update` failed with
  `42703: column "migration_id" does not exist`. PostgreSQL hinted:
  *Perhaps you meant `__EFMigrationsHistory.MigrationId`*.
- **Cause:** EF Core keeps its own history table, `__EFMigrationsHistory`. It
  was created by the first migration with PascalCase columns (`MigrationId`).
  With the new convention, EF Core looked for `migration_id` and could not read
  its own history.
- **Fix:** the database only held test data, so it was rebuilt: dropped it
  (`dotnet ef database drop --force`), deleted the `Migrations` folder, and
  created one clean `InitialCreate` migration with snake_case from the start.
- **Lesson:** decide the naming convention before the first migration. In
  production a database is never dropped; the change would need a migration
  that renames tables, columns and the history table carefully.

### 8. Migration failed because existing rows broke the new rules
- **Symptom:** the `AddLawsAndRules` migration failed twice: first with
  `23505: could not create unique index "ix_documents_law_id_effective_date"`,
  then, after removing the duplicate, with
  `23503: ... violates foreign key constraint "fk_documents_laws_law_id"`.
- **Cause:** `documents` already held test rows created before the rules
  existed: two rows with the same `law_id` and `effective_date`, and rows
  pointing to law 1, while the new `laws` table started empty.
- **Diagnosis:** predicted before running it. A database never deletes data to
  satisfy a new rule; it refuses to create the rule. The SQLSTATE code told
  which rule failed: `23505` is a unique violation, `23503` a foreign key
  violation (class `23` = integrity constraint violation).
- **Detail:** each failed attempt also undid the `CREATE TABLE laws` that ran
  before it. EF Core applies a migration inside a transaction: all or nothing.
- **Fix:** test data only, so the rows were deleted (`DELETE FROM documents;`)
  and the migration applied cleanly.
- **Lesson:** before adding a constraint, check whether existing data violates
  it. In production the fix is a migration that corrects the data first, never
  deleting rows by hand.

### 9. `git push` rejected: the remote history had been rewritten
- **Symptom:** `git push` was rejected; `git pull` then showed `(forced update)`
  and `fatal: refusing to merge unrelated histories`.
- **Diagnosis:** `git log --oneline` locally and on `origin/main` showed the same
  commit messages with different hashes: the remote history had been rewritten,
  so Git found no common ancestor. Only the newest local commit was missing
  from the remote.
- **Fix:** kept a backup branch (`git branch respaldo`), reset `main` to the
  remote (`git reset --hard origin/main`), re-applied the missing commit with
  `git cherry-pick <hash>`, checked the log, then pushed.
- **Lesson:** rewriting a shared history breaks every other copy of the repo.
  Compare both logs before acting, and make a backup branch before any
  `reset --hard`.

## Troubleshooting practice

Interview-style tickets for this phase, some reproduced by breaking the code on
purpose: [troubleshooting/phase-1.md](troubleshooting/phase-1.md).