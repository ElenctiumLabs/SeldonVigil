# Vector Store

SeldonVigil uses **pgvector** for vector search, backed by PostgreSQL. The `vector` extension must be available on the database.

`PgVectorStore` stores embeddings in the `vector_memories` table with an HNSW cosine index. The embedding dimension is configured via `Vectors:Dimensions` (default: 3072, matching `text-embedding-3-large`). The schema is created automatically on first use.
