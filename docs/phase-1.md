# Phase 1 — API: upload and list documents

> **Status:** in progress.

## What was built so far

- ASP.NET Core Web API project on .NET 10, using controllers.
- `Document` model mirroring the `documents` table (see [phase-0.md](phase-0.md)).
- `GET /documents`, returning an in-memory list until PostgreSQL is connected.
- `GET /documents/{id}`: 200 with the document, 404 if it does not exist.
- `POST /documents`: the client sends a `CreateDocumentRequest` (no id); the
  server assigns the id and returns 201 with a `Location` header.
  A missing required field is rejected with 400 by `[ApiController]` before the
  method runs.
- `LegalRag.Api.http`: manual requests for every endpoint, kept in the repo.

**Known limitation:** documents live in memory, so anything created with POST
is lost when the server restarts. PostgreSQL replaces the list next.

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

## Troubleshooting practice

Problems reproduced on purpose to practise diagnosis.

### Ticket 1: a new endpoint returns 404
- **Report:** "I added `GET /documents/{id}`, saved the file and tested it.
  `/documents/1` returns 404, but document 1 exists. `/documents` works."
- **Possible causes, checked by layer:**
  1. Server: still running the old build, so the new route does not exist.
  2. Route: the URL or HTTP method does not match the declared route.
  3. Method: the request never reaches `GetById`.
  4. Data: the method runs, but the lookup finds nothing and returns `NotFound()`.
- **Key evidence:** the body of the 404.

  | Request | Status | Body | Who answered |
  |---|---|---|---|
  | `GET /documents/99` | 404 | ProblemDetails JSON | My code: `NotFound()` |
  | `GET /libros/1` | 404 | Empty (`Content-Length: 0`) | ASP.NET: no route matched |

- **Most likely cause:** the server was started before the endpoint was added.
  Saving a file does not change a running server.
- **Fix:** stop the server (Ctrl+C) and run `dotnet run` again.
- **Interview question:** *"You added a new endpoint, but calling it returns
  404. How do you diagnose it?"*
- **Answer:** "I check by layers, bottom up: that the server is running the new
  code, that the URL and method match the route, that the request reaches the
  method, and finally the data. In ASP.NET, an empty 404 means no route matched;
  a 404 with a JSON error body came from my own code."
