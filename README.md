# legal-rag-assistant

RAG assistant for the Dominican Labor Code. Answers questions citing the
exact article, and says "I don't know" when no source supports the answer.

**Stack:** C#, ASP.NET Core, PostgreSQL + pgvector, Ollama (embeddings),
OpenAI-compatible API (generation).

## Why

Answers are only useful if they can be verified. Every answer cites the
article it came from, and quality is measured with an evaluation set,
not assumed.

## Phases

| Phase | Scope | Status | Docs |
|---|---|---|---|
| 0 | Data design | Done | [phase-0.md](docs/phase-0.md) |
| 1 | API: upload and list documents | Pending | |
| 2 | Chunking | Pending | |
| 3 | Embeddings and pgvector search | Pending | |
| 4 | Evaluation set (30 questions) | Pending | |
| 5 | Generation with mandatory citations | Pending | |
| 6 | Measure, tune, measure again | Pending | |
| 7 | Cost and latency per query | Pending | |