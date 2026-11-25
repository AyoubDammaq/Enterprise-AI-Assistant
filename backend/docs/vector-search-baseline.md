# Vector Search Baseline

## 1. Purpose

This document records the initial performance baseline of the existing
Vector Search implementation before introducing Hybrid Search.

The baseline must remain reproducible and serve as a reference for
future retrieval improvements.

## 2. Reference environment

| Item | Value |
|---|---|
| Git branch | `week-11/advanced-rag` |
| Reference commit | `c2d29e7362b420753383135e1678a6a54e723f4c` |
| .NET SDK | `10.0.402` |
| Target framework | `net10.0` |
| Chat model | `llama3.2` |
| Embedding model | `nomic-embed-text:latest` |
| Embedding dimensions | `768` |
| Embedding provider | Ollama |
| Vector store | `InMemoryVectorStore` |

## 3. Dataset

Corpus location:

`tests/EnterpriseAIAssistant.Infrastructure.Tests/TestData/RAG/Corpus`

Documents:

- `product-guide.md`
- `support-faq.md`
- `troubleshooting.md`

Observed indexing results:

- Documents loaded: 3
- Chunks indexed: 6

Evaluation questions:

`tests/EnterpriseAIAssistant.Infrastructure.Tests/TestData/RAG/Evaluation/evaluation-questions.md`

The evaluation dataset contains 7 questions:

- 5 questions with expected supporting information.
- 2 questions expected to be unanswerable from the corpus.

Evaluation questions are not indexed as knowledge documents.

## 4. Retrieval configuration

API configuration:

| Setting | Value |
|---|---:|
| `Retrieval:TopK` | `3` |
| `Retrieval:MinimumSimilarityScore` | `0.60` |
| Embedding dimensions | `768` |

Document loading configuration:

| Setting | Value |
|---|---|
| Root directory | `Documents` |
| Strategy | `ParagraphAware` |
| Chunk size | `800` |
| Chunk overlap | `100` |

The document-loading settings above are the API configuration values.
The diagnostic corpus is loaded separately from the test-data directory.

## 5. Full test-suite baseline

Command:

`dotnet test .\EnterpriseAIAssistant.slnx --logger "console;verbosity=minimal"`

| Result | Count |
|---|---:|
| Total | 248 |
| Passed | 247 |
| Failed | 0 |
| Skipped | 1 |

The skipped test is:

`InMemoryMemoryServiceTests.Memory_ShouldPersistAcrossProcessRestarts`

It is skipped because the current memory implementation is volatile and does not provide durable persistence across process restarts.

Project results:

| Test project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Domain.Tests | 1 | 0 | 0 |
| Application.Tests | 19 | 0 | 0 |
| API.Tests | 10 | 0 | 0 |
| Infrastructure.Tests | 217 | 0 | 1 |

## 6. Vector Search ranking metrics

Diagnostic: `DiagnoseRetrieval_WithRealOllamaEmbeddings`

| Metric | TopK = 1 | TopK = 3 | TopK = 5 |
|---|---:|---:|---:|
| Precision@K | 100% | 33.33% | 20% |
| Recall@K | 100% | 100% | 100% |
| HitRate@K | 100% | 100% | 100% |
| MRR | 1.0 | 1.0 | 1.0 |

Interpretation:

- All five answerable questions retrieved an expected relevant chunk.
- The first relevant result was ranked first for all five positive questions.
- Increasing TopK increased the number of returned chunks without improving recall.
- Precision decreased as more chunks were returned.

These ranking metrics concern the five positive questions only.

## 7. Similarity score observations

Observed scores for the seven evaluation questions:

| Question | Expected | Similarity score |
|---|---|---:|
| Q1 | Answerable | 0.7761 |
| Q2 | Answerable | 0.8199 |
| Q3 | Answerable | 0.7629 |
| Q4 | Answerable | 0.7323 |
| Q5 | Answerable | 0.6184 |
| Q6 | Unanswerable | 0.6856 |
| Q7 | Unanswerable | 0.7884 |

Important observation:

Both unanswerable questions retrieve semantically similar chunks.
A high cosine similarity score alone does not prove that the retrieved
context contains enough information to answer a question.

## 8. Similarity threshold experiment

Diagnostic: `DiagnoseRetrieval_WithDifferentSimilarityThresholds`

TopK remained fixed at 3.

| Threshold | Positive questions retrieved | Negative questions rejected |
|---|---:|---:|
| 0.50 | 5/5 | 0/2 |
| 0.60 | 5/5 | 0/2 |
| 0.70 | 4/5 | 1/2 |
| 0.80 | 1/5 | 2/2 |

Interpretation:

- Thresholds 0.50 and 0.60 preserve all positive retrievals.
- Threshold 0.70 loses Q5, whose score is 0.6184.
- Threshold 0.80 rejects both negative questions but also loses most positive questions.
- Threshold adjustment alone cannot reliably distinguish answerable from unanswerable questions.

Provisional configuration retained:

- TopK = 3
- MinimumSimilarityScore = 0.60

This is a baseline configuration, not a claim that it is optimal.

## 9. Context relevance and abstention

Diagnostic: `ContextRelevanceDiagnosticTests`

Observed evaluation:

| Classification | Count |
|---|---:|
| True Positives | 5 |
| False Positives | 0 |
| True Negatives | 2 |
| False Negatives | 0 |

Observed metrics:

- Precision: 100%
- Recall: 100%
- Specificity: 100%
- Accuracy: 100%

The context evaluator classified Q1-Q5 as SUPPORTED and Q6-Q7 as
INSUFFICIENT.

Important limitations:

- Only seven synthetic evaluation cases were used.
- This result is not a broad reliability guarantee.
- The evaluator uses an LLM-as-judge approach.
- Its output parser accepts `SUPPORTED`; other outputs are treated as
  insufficient.
- Larger, more diverse and adversarial evaluation datasets are needed.

## 10. Current implementation limitations

- `InMemoryVectorStore` is process-local and volatile.
- Stored vectors are lost when the process restarts.
- Search uses exhaustive cosine similarity scanning, with complexity
  proportional to the number of stored vectors and embedding dimensions.
- No explicit lexical or Hybrid Search implementation was found during
  the initial source-code search.
- Similarity scores alone cannot establish whether context is sufficient.
- The current evaluation dataset is small.

## 11. Baseline conclusion

The existing Vector Search implementation provides a reproducible
starting point:

- All five positive evaluation questions retrieve a relevant chunk.
- The first relevant result is ranked first in the current diagnostic.
- TopK = 3 and threshold = 0.60 preserve all positive retrievals.
- The current retrieval threshold does not independently prevent
  retrieval for unanswerable questions.
- Context relevance evaluation currently distinguishes the seven
  synthetic cases correctly, but requires broader validation.

The baseline must be retained for comparison with future Hybrid Search
experiments.

Do not compare future results unless the corpus, evaluation dataset,
embedding model, and measurement procedure are controlled and documented.

---

Baseline status: Initial diagnostic recorded.
