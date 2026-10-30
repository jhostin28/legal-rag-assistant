# Phase 1 — Troubleshooting practice

Interview-style tickets for [phase 1](../phase-1.md). Each one starts as a report
from a teammate, is diagnosed layer by layer, and ends with the general
interview question it prepares for.

Format: report → possible causes → evidence → cause → fix → interview question
and answer. From ticket 2 on, the fault is introduced on purpose in a
`chaos/<topic>` branch and diagnosed without reading the diff.

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
