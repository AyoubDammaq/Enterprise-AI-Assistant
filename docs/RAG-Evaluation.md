# RAG Retrieval Evaluation

## 1. Objective

Evaluate the retrieval component independently of the LLM.

The objective is to verify whether the system retrieves the expected documents and relevant chunks for supported questions, while avoiding misleading retrieval results for unsupported questions.

## 2. Reference Dataset

The evaluation uses a single reference dataset containing seven questions.

### 2.1 Corpus

The indexed corpus consists of exactly three documents:

| Document | Description |
|---|---|
| `support-faq.md` | Password reset and account lockout procedures |
| `product-guide.md` | Atlas Inventory features, roles and configuration |
| `troubleshooting.md` | Troubleshooting procedures and error resolution |

Corpus location:

`tests/EnterpriseAIAssistant.Infrastructure.Tests/TestData/RAG/Corpus/`

### 2.2 Evaluation Questions

Evaluation dataset:

`tests/EnterpriseAIAssistant.Infrastructure.Tests/TestData/RAG/Evaluation/evaluation-questions.md`

| ID | Question | Expected Source | Type |
|---|---|---|---|
| Q1 | How can a user reset their Demo Portal password? | `support-faq.md` | Positive |
| Q2 | How long is a Demo Portal account locked after failed login attempts? | `support-faq.md` | Positive |
| Q3 | What is the default low-stock threshold in Atlas Inventory? | `product-guide.md` | Positive |
| Q4 | Which user role can modify inventory settings and manage users? | `product-guide.md` | Positive |
| Q5 | How should a user resolve error INV-001? | `troubleshooting.md` | Positive |
| Q6 | What is the monthly subscription price of Atlas Inventory? | None | Negative |
| Q7 | Which database engine does Atlas Inventory use in production? | None | Negative |

### 2.3 Dataset Rules

- Q1–Q5 are answerable from the reference corpus.
- Q6–Q7 are intentionally unanswerable.
- Negative questions must not be considered successful merely because the retriever returns chunks.
- Retrieved chunks for negative questions require relevance assessment.
- The corpus and evaluation questions must remain synchronized.

## 3. Evaluation Configuration

The diagnostic integration test uses:

| Parameter | Value |
|---|---|
| Embedding provider | Ollama |
| Embedding model | `nomic-embed-text` |
| Embedding dimensions | 768 |
| Chunk size | 800 |
| Chunk overlap | 100 |
| Top-K | 5 |
| Minimum similarity score | 0.50 |

The test loads documents exclusively from the `Corpus` directory.

## 4. Retrieval Evaluation Results

### 4.1 Positive Questions

| ID | Expected Source | Evaluation |
|---|---|---|
| Q1 | `support-faq.md` | Executed |
| Q2 | `support-faq.md` | Executed |
| Q3 | `product-guide.md` | Executed |
| Q4 | `product-guide.md` | Executed |
| Q5 | `troubleshooting.md` | Executed |

The diagnostic test calculates HitRate@1, HitRate@2 and MRR for positive questions.

### 4.2 Negative Questions

| ID | Question | Expected Behavior |
|---|---|---|
| Q6 | Monthly subscription price | No unsupported answer |
| Q7 | Production database engine | No unsupported answer |

A retrieved chunk does not necessarily contain a relevant answer.

For example, Q7 retrieved chunks from the corpus, but the context relevance diagnostic classified the context as insufficient.

This demonstrates why retrieval relevance and answer sufficiency must be evaluated separately.

## 5. Evaluation Metrics

The evaluation uses the following metrics:

- **HitRate@K:** proportion of positive queries for which the expected document appears in the first K results.
- **MRR (Mean Reciprocal Rank):** measures how highly the first relevant result is ranked.
- **Precision@K:** proportion of relevant retrieved chunks among the first K results.
- **Recall@K:** proportion of relevant chunks retrieved among all known relevant chunks.
- **Negative-query retrieval analysis:** checks whether unsupported questions retrieve potentially misleading chunks.

Metrics must be calculated using the same reference dataset and clearly documented relevance judgments.

## 6. Test Organization

| Test | Category | Purpose |
|---|---|---|
| `RetrievalEvaluationTests` | Unit | Validate ranking, similarity threshold and empty results using fake embeddings |
| `KnowledgeRetrievalDiagnosticTests` | Integration | Evaluate retrieval against the real reference corpus using Ollama |
| `ContextRelevanceDiagnosticTests` | Integration | Evaluate whether retrieved context is sufficient to answer questions |
| `RagPipelineIntegrationTests` | Integration | Validate the complete RAG pipeline with real Ollama embeddings |

Unit tests must not require a running Ollama instance.

Integration tests require the configured Ollama service and embedding model.

## 7. Observations and Limitations

- The reference corpus contains only three documents.
- The evaluation dataset contains five positive and two negative questions.
- The dataset is useful for validating the current retrieval implementation but is too small to establish general RAG performance.
- Similarity scores alone do not guarantee that retrieved chunks contain the requested information.
- Negative questions must be evaluated independently from positive questions.
- Context sufficiency and retrieval relevance are separate evaluation concerns.

## 8. Conclusion

This evaluation dataset establishes a consistent reference between the indexed documents, evaluation questions and automated tests.

Future iterations should expand the corpus and evaluation dataset, introduce manually validated chunk-level relevance judgments, and compare retrieval configurations using consistent metrics.