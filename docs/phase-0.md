# Phase 0 — Data design

## What was built

The database design for storing legal documents and their chunks,
so every answer can be traced back to an exact article.

```
laws (1) → (N) documents (1) → (N) chunks
```

| Table | Purpose |
|---|---|
| `laws` | Each law, stored once (e.g. Código de Trabajo) |
| `documents` | Each version of a law, with the date it takes effect |
| `chunks` | Pieces of a document, with their text and embedding |

## Schema

```
laws
  id              SERIAL     PRIMARY KEY
  name            TEXT       NOT NULL UNIQUE

documents
  id              SERIAL     PRIMARY KEY
  law_id          INTEGER    NOT NULL, FK → laws, ON DELETE RESTRICT
  source_url      TEXT       NOT NULL
  effective_date  DATE       NOT NULL
  UNIQUE (law_id, effective_date)

chunks
  id              SERIAL     PRIMARY KEY
  document_id     INTEGER    NOT NULL, FK → documents, ON DELETE CASCADE
  article_number  (pending)  NOT NULL
  chunk_index     INTEGER    NOT NULL DEFAULT 1
  content         TEXT       NOT NULL
  embedding       vector(N)  NOT NULL
  UNIQUE (document_id, article_number, chunk_index)
```

**Pending types:**
- `article_number`: the Code has articles like "177 bis" and a section of
  "Principios Fundamentales" numbered in Roman numerals, which do not fit an
  INTEGER. Decided in Phase 2, after looking at the real text.
- `embedding`: the dimension `N` depends on the embedding model, decided in
  Phase 3.

## Decisions

### Chunk by article, not by fixed size
- **Chosen:** one chunk per article; long articles split into parts that
  share the same `article_number`, ordered by `chunk_index`.
- **Alternative:** fixed size (every N words).
- **Why:** fixed size can mix two articles in one chunk, making the
  citation ambiguous. By article, every chunk has exactly one source.
- **Trade-off:** chunk sizes vary a lot between short and long articles.

### Only articles are stored
- **Chosen:** preambles and "considerandos" are not stored, so
  `article_number` can be NOT NULL.
- **Why:** they explain why the law exists, not what it says. They would
  compete in search with articles that actually answer questions.
- **Trade-off:** questions about the intent of a law cannot be answered.

### Versions by effective date
- **Chosen:** `effective_date` only. The current version is the latest one
  on or before today.
- **Alternatives:** an `is_current` boolean, or both.
- **Why:** the date keeps history (a 2021 case follows the 2021 law) and
  derives the current version. Storing both duplicates the fact, and the
  two could disagree.

### Separate `laws` table
- **Chosen:** versions are grouped by `law_id`.
- **Alternative:** grouping by the title text.
- **Why:** a typo or a missing accent in a title would create a different
  law. An id cannot be misspelled.

### Embedding NOT NULL, inside a transaction
- **Chosen:** a document and all its chunks are saved together; if the
  embedding service fails, everything rolls back.
- **Alternative:** nullable embedding, generated later with retries.
- **Why:** documents are loaded rarely, so retrying the whole document is
  cheap, and no chunk is ever stored without being searchable.
- **Trade-off:** at thousands of documents, a two-step process with a
  status column would be better.

### CASCADE vs RESTRICT
- `documents → chunks`: **CASCADE**. Deleting a wrong document is common
  and easy to redo; its chunks are useless without it.
- `laws → documents`: **RESTRICT**. Deleting a whole law is rare and would
  lose all its history.

### PostgreSQL with pgvector
- **Alternative:** a dedicated vector database (Pinecone, Qdrant).
- **Why:** text, vector and relations stay in one place, so citations are
  a JOIN away. A dedicated database would make sense at millions of vectors.

## Stack decisions

- **LLM:** any OpenAI-compatible API (OpenRouter free models). Switching
  provider means changing the base URL and key, not the code.
- **Embeddings:** a local model through Ollama, free, where most calls happen.
  The exact model is decided in Phase 3.

## Known limitations

- The embedding model fixes the vector size. Changing it means
  re-embedding every chunk.
- Switching LLM provider keeps the code working but may change answer
  quality. The Phase 4 evaluation set exists to measure that.
- Laws are identified by a manually entered name; a duplicate law could
  still be created with a different spelling of its name.