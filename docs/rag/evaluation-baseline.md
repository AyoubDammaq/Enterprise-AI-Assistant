# RAG Evaluation Baseline

## 1. Purpose

This document records the initial reproducible evaluation baseline of the
Retrieval-Augmented Generation (RAG) pipeline.

The evaluation covers:

- Document retrieval quality.
- Retrieval ranking.
- Context relevance evaluation.
- Abstention when the knowledge base does not contain an answer.
- Unit and integration test execution.

This baseline is an initial experimental reference, not a production
quality guarantee.

## 2. Environment

| Component | Version / Value |
|---|---|
| Operating system | Windows |
| .NET SDK | 10.0.402 |
| Target framework | net10.0 |
| Ollama | 0.40.13 |
| Chat model | llama3.2 |
| Embedding model | nomic-embed-text |
| Embedding dimensions | 768 |
| Vector store | In-memory |
| Test framework | xUnit |

### Required Ollama models

```powershell
ollama pull llama3.2
ollama pull nomic-embed-text
```

Verify the installation:

```powershell
ollama --version
ollama list
```

Make sure the Ollama service is running before executing integration tests.

## 3. RAG Configuration

Configuration from `src/EnterpriseAIAssistant.API/appsettings.json`:

| Parameter | Value |
|---|---:|
| Ollama endpoint | http://localhost:11434 |
| Chat model | llama3.2 |
| Embedding model | nomic-embed-text |
| Embedding dimensions | 768 |
| Chunk size | 800 |
| Chunk overlap | 100 |
| Retrieval TopK | 5 |
| Minimum similarity score | 0.50 |

The document loader splits documents into chunks of 800 characters
with an overlap of 100 characters.

The retriever requests at most five chunks and applies a minimum
similarity score of 0.50.

## 4. Evaluation Dataset

Corpus directory:

`tests/EnterpriseAIAssistant.Infrastructure.Tests/TestData/RAG/Corpus`

Documents:

- `support-faq.md`
- `product-guide.md`
- `troubleshooting.md`

Evaluation questions:

| ID | Question | Expected classification |
|---|---|---|
| Q1 | How can a user reset their Demo Portal password? | SUPPORTED |
| Q2 | How long is a Demo Portal account locked after failed login attempts? | SUPPORTED |
| Q3 | What is the default low-stock threshold in Atlas Inventory? | SUPPORTED |
| Q4 | Which user role can modify inventory settings and manage users? | SUPPORTED |
| Q5 | How should a user resolve error INV-001? | SUPPORTED |
| Q6 | What is the monthly subscription price of Atlas Inventory? | INSUFFICIENT |
| Q7 | Which database engine does Atlas Inventory use in production? | INSUFFICIENT |

The first five questions have answers in the corpus.
The last two deliberately request information that is not documented.

## 5. Reproducing the Tests

Run commands from the `backend` directory.

### 5.1 Unit tests

Infrastructure tests, excluding integration tests:

```powershell
dotnet test .\tests\EnterpriseAIAssistant.Infrastructure.Tests\EnterpriseAIAssistant.Infrastructure.Tests.csproj --filter "Category!=Integration" --verbosity minimal
```

Application tests:

```powershell
dotnet test .\tests\EnterpriseAIAssistant.Application.Tests\EnterpriseAIAssistant.Application.Tests.csproj --verbosity minimal
```

Domain tests:

```powershell
dotnet test .\tests\EnterpriseAIAssistant.Domain.Tests\EnterpriseAIAssistant.Domain.Tests.csproj --verbosity minimal
```

API tests:

```powershell
dotnet test .\tests\EnterpriseAIAssistant.API.Tests\EnterpriseAIAssistant.API.Tests.csproj --verbosity minimal
```

### 5.2 Integration tests

Ensure Ollama is running and the required models are available.

```powershell
dotnet test .\tests\EnterpriseAIAssistant.Infrastructure.Tests\EnterpriseAIAssistant.Infrastructure.Tests.csproj --filter "Category=Integration" --verbosity normal
```

Integration tests use real Ollama models and should not be considered
offline unit tests.

## 6. Unit Test Results

Execution date: 2026-11-11

| Test project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Infrastructure.Tests | 144 | 0 | 1 |
| Application.Tests | 18 | 0 | 0 |
| Domain.Tests | 1 | 0 | 0 |
| API.Tests | 10 | 0 | 0 |
| **Total** | **173** | **0** | **1** |

The skipped test is:

`Memory_ShouldPersistAcrossProcessRestarts`

It is intentionally skipped because the current in-memory memory
implementation does not provide durable persistence across process restarts.

## 7. Integration Test Results

Execution date: 2026-11-11

| Metric | Result |
|---|---:|
| Total integration tests | 3 |
| Passed | 3 |
| Failed | 0 |
| Skipped | 0 |
| Duration | 79.9 seconds |

The integration test suite includes:

- `KnowledgeRetrievalDiagnosticTests`
- `ContextRelevanceDiagnosticTests`
- `RagPipelineIntegrationTests`

The context relevance diagnostic indexed three knowledge documents
and six chunks using the real Ollama embedding model.

## 8. Retrieval Evaluation Results

The following results were observed during the real Ollama context
relevance diagnostic.

Scores are cosine similarity scores returned by the vector search.

### Q1 — Password reset

Expected document: `support-faq.md`

| Rank | Source | Chunk | Score |
|---:|---|---:|---:|
| 1 | support-faq.md | 0 | 0.7761 |
| 2 | support-faq.md | 1 | 0.6579 |
| 3 | troubleshooting.md | 1 | 0.5236 |

Expected: SUPPORTED  
Actual: SUPPORTED

### Q2 — Account lockout duration

Expected document: `support-faq.md`

| Rank | Source | Chunk | Score |
|---:|---|---:|---:|
| 1 | support-faq.md | 0 | 0.8199 |
| 2 | support-faq.md | 1 | 0.6001 |
| 3 | troubleshooting.md | 1 | 0.5205 |

Expected: SUPPORTED  
Actual: SUPPORTED

### Q3 — Default low-stock threshold

Expected document: `product-guide.md`

| Rank | Source | Chunk | Score |
|---:|---|---:|---:|
| 1 | product-guide.md | 0 | 0.7629 |
| 2 | troubleshooting.md | 0 | 0.6720 |
| 3 | troubleshooting.md | 1 | 0.6072 |
| 4 | product-guide.md | 1 | 0.5452 |

Expected: SUPPORTED  
Actual: SUPPORTED

### Q4 — User role and inventory settings

Expected document: `product-guide.md`

| Rank | Source | Chunk | Score |
|---:|---|---:|---:|
| 1 | product-guide.md | 1 | 0.7323 |
| 2 | product-guide.md | 0 | 0.6040 |
| 3 | support-faq.md | 1 | 0.5638 |
| 4 | troubleshooting.md | 1 | 0.5632 |
| 5 | support-faq.md | 0 | 0.5032 |

Expected: SUPPORTED  
Actual: SUPPORTED

### Q5 — Error INV-001 resolution

Expected document: `troubleshooting.md`

| Rank | Source | Chunk | Score |
|---:|---|---:|---:|
| 1 | troubleshooting.md | 0 | 0.6184 |
| 2 | troubleshooting.md | 1 | 0.6044 |
| 3 | support-faq.md | 0 | 0.5589 |
| 4 | support-faq.md | 1 | 0.5307 |

Expected: SUPPORTED  
Actual: SUPPORTED

## 9. Retrieval Metrics

For Q1–Q5, the expected source document was retrieved at rank 1
in every case.

| Metric | Result |
|---|---:|
| Hit Rate@1 | 100% |
| Hit Rate@2 | 100% |
| Mean Reciprocal Rank (MRR) | 1.00 |

Definitions:

- Hit Rate@K: proportion of questions for which the expected document
  appears among the first K retrieved results.
- MRR: average reciprocal rank of the first relevant result.

For this evaluation, relevance is defined by the expected source document.

## 10. Abstention Evaluation

Q6 and Q7 deliberately request information absent from the corpus.

### Q6 — Monthly subscription price

Expected classification: INSUFFICIENT

| Rank | Source | Chunk | Score |
|---:|---|---:|---:|
| 1 | product-guide.md | 0 | 0.6856 |
| 2 | product-guide.md | 1 | 0.5568 |
| 3 | troubleshooting.md | 0 | 0.5517 |
| 4 | troubleshooting.md | 1 | 0.5461 |

Actual classification: INSUFFICIENT  
Result: CORRECT

### Q7 — Production database engine

Expected classification: INSUFFICIENT

| Rank | Source | Chunk | Score |
|---:|---|---:|---:|
| 1 | product-guide.md | 0 | 0.7884 |
| 2 | troubleshooting.md | 0 | 0.6781 |
| 3 | product-guide.md | 1 | 0.6469 |
| 4 | troubleshooting.md | 1 | 0.6413 |

Actual classification: INSUFFICIENT  
Result: CORRECT

### Context Relevance Confusion Matrix

| Metric | Result |
|---|---:|
| True Positives | 5 |
| False Positives | 0 |
| True Negatives | 2 |
| False Negatives | 0 |

| Classification metric | Result |
|---|---:|
| Accuracy | 100% |
| Precision | 100% |
| Recall | 100% |
| Specificity | 100% |

All seven questions were classified correctly in this diagnostic run.

## 11. Important Observations

### Similarity is not evidence of answerability

Q7 obtained a similarity score of 0.7884, higher than the Q5
score of 0.6184.

However, Q7 has no answer in the knowledge corpus.

This demonstrates that cosine similarity alone cannot determine
whether retrieved context contains sufficient evidence to answer
a question.

The context relevance evaluator provides an additional verification
step before grounded answer generation.

### Abstention is a separate evaluation dimension

Retrieval can return semantically related passages even when the
requested information is absent.

The system must distinguish between:

- Relevant context that supports an answer.
- Context that is semantically related but insufficient.
- Queries for which no useful evidence is available.

## 12. Known Limitations

1. The evaluation dataset contains only seven questions.
2. The corpus contains only three Markdown documents and six chunks.
3. Classification metrics are based on a very small sample.
4. A 100% result on this dataset does not demonstrate production reliability.
5. Similarity scores are not calibrated probabilities.
6. The vector store is in-memory and does not provide durable persistence.
7. The memory persistence test remains skipped.
8. The context relevance evaluator depends on the local LLM and its prompt.

The current results should be treated as an initial baseline.

## 13. Future Improvements

Potential future evaluation improvements:

- Expand the dataset with more positive and negative questions.
- Add ambiguous and partially answerable questions.
- Add adversarial and prompt-injection test cases.
- Evaluate multiple runs to observe LLM variability.
- Track false positives and false negatives.
- Compare different embedding and chat models.
- Evaluate retrieval independently from answer generation.
- Introduce a persistent vector database when required.

Hybrid search and reranking are outside the scope of this baseline.

## 14. Baseline Conclusion

The current RAG implementation demonstrates:

- Successful retrieval of the expected document for all five positive questions.
- Hit Rate@1 of 100% and MRR of 1.00 on the current positive dataset.
- Correct abstention on both negative questions.
- Successful unit and integration test executions.

This baseline establishes a reproducible starting point for future
RAG evaluation and retrieval improvements.
