# RAG Evaluation Results — Week 10

## 1. Objective

Re-run the complete Q1–Q7 RAG evaluation after the Day 8 improvements.

The objectives are to:
- Compare the new classification results with the baseline.
- Verify that negative questions remain correctly rejected.
- Identify retrieval weaknesses.
- Investigate possible causes without changing multiple parameters simultaneously.
- Document remaining limitations and future experiments.


## 2. Evaluation Environment

### 2.1 Execution environment

| Property | Value |
|---|---|
| Branch | `week-10/rag-dataset-stabilization` |
| Latest commit at documentation review | `705b605` |
| Target framework | .NET 10 |
| Chat model | `llama3.2` |
| Embedding model | `nomic-embed-text` |
| Embedding dimensions | 768 |
| Knowledge documents | 3 |
| Indexed chunks | 6 |
| Evaluation dataset | Q1–Q7 |
| Operating environment | Windows |
| Ollama version | 0.40.13 |

### 2.2 Application retrieval configuration

The following values were verified in `backend/src/EnterpriseAIAssistant.API/appsettings.json`.

| Parameter | Configured value |
|---|---|
| Top-K | 3 |
| Minimum similarity score | 0.60 |
| Chunking strategy | ParagraphAware |
| Chunk size | 800 |
| Chunk overlap | 100 |
| Embedding model | nomic-embed-text |
| Embedding dimensions | 768 |

The retrieval parameter experiments documented separately use controlled diagnostic configurations. Their experimental thresholds must not be confused with the application's configured threshold.

### 2.3 Full solution test execution

The complete solution test suite was executed using:

```powershell
dotnet test .\EnterpriseAIAssistant.slnx
```

Execution summary:

| Result | Count |
|---|---:|
| Total tests | 225 |
| Passed | 224 |
| Failed | 0 |
| Skipped | 1 |
| Test duration | 134.9 seconds |

The skipped test is:

`Memory_ShouldPersistAcrossProcessRestarts`

This test is skipped because the current memory implementation uses volatile in-memory storage.

The complete test suite passed without failures.

## 3. Baseline Results

The previously recorded baseline produced the following results:

| Metric | Baseline |
|---|---:|
| True Positives | 5 |
| False Positives | 0 |
| True Negatives | 2 |
| False Negatives | 0 |
| Accuracy | 100% |
| Precision | 100% |
| Recall | 100% |
| Specificity | 100% |

All seven questions were correctly classified.

## 4. New Evaluation Results

The full Q1–Q7 evaluation diagnostic completed successfully during the full solution test execution.

The classification results were:

| Metric | Baseline | Week 10 |
|---|---:|---:|
| True Positives | 5 | 5 |
| False Positives | 0 | 0 |
| True Negatives | 2 | 2 |
| False Negatives | 0 | 0 |
| Accuracy | 100% | 100% |
| Precision | 100% | 100% |
| Recall | 100% | 100% |
| Specificity | 100% | 100% |

Confusion matrix:

- TP = 5
- FP = 0
- TN = 2
- FN = 0

No classification errors were detected in the seven-question evaluation dataset.

These metrics describe the current small evaluation dataset. They must not be interpreted as evidence of production-level reliability.

## 5. Question-Level Analysis

| Question | Expected | Actual | Result |
|---|---|---|---|
| Q1 | SUPPORTED | SUPPORTED | Correct |
| Q2 | SUPPORTED | SUPPORTED | Correct |
| Q3 | SUPPORTED | SUPPORTED | Correct |
| Q4 | SUPPORTED | SUPPORTED | Correct |
| Q5 | SUPPORTED | SUPPORTED | Correct |
| Q6 | INSUFFICIENT | INSUFFICIENT | Correct |
| Q7 | INSUFFICIENT | INSUFFICIENT | Correct |

### Q1 — Password reset procedure

The most relevant chunk was retrieved:

- Source: `support-faq.md`
- Chunk: 0
- Similarity score: 0.7761

However, two additional chunks were retrieved:
- `support-faq.md`, chunk 1 — score 0.6579.
- `troubleshooting.md`, chunk 1 — score 0.5236.

The additional content concerns changing the registered email and troubleshooting Atlas Inventory.

**Observed weakness:** retrieval precision. Relevant information is found, but unrelated content is also included.

**Potential causes:** chunking strategy and Top-K selection.

**Root cause:** not yet confirmed.

### Q7 — Production database engine

Expected result: `INSUFFICIENT`.

Actual result: `INSUFFICIENT`.

Retrieved chunks included:
- `product-guide.md`, chunk 0 — score 0.7884.
- `troubleshooting.md`, chunk 0 — score 0.6781.
- `product-guide.md`, chunk 1 — score 0.6469.
- `troubleshooting.md`, chunk 1 — score 0.6413.

None of the retrieved passages specifies the production database engine.

**Observed weakness:** semantically similar content receives relatively high similarity scores despite not answering the question.

**Potential causes:** embedding relevance, Top-K selection and similarity threshold.

**Root cause:** not yet confirmed.

## 6. Negative Question Validation

| Question | Expected | Actual | Result |
|---|---|---|---|
| Q6 — Monthly subscription price | INSUFFICIENT | INSUFFICIENT | Correct |
| Q7 — Production database engine | INSUFFICIENT | INSUFFICIENT | Correct |

Both negative questions remain correctly rejected.

The evaluation demonstrates that the abstention behavior works for these two examples, even when retrieval returns semantically related but insufficient content.

This does not establish robustness across all out-of-context questions.

## 7. Changes Made and Experimental Status

The Day 8 improvements include:
- Structured logging with `ILogger<T>`.
- Centralized retrieval configuration through `RetrievalOptions`.
- Validation of similarity thresholds and Top-K limits.
- Embedding validation, including vector dimensions and finite values.
- Additional vector store and retriever tests.

During the Week 10 evaluation, no application code or retrieval configuration changes were applied.

The subsequent retrieval parameter experiments were diagnostic only. They evaluated different Top-K and similarity threshold values without changing the application's configured parameters.

The documentation updates performed during Day 10 do not modify the application code.

## 8. Proposed Controlled Experiments

No experimental changes have been applied yet. The following experiments are proposed.

| Experiment | Single variable | Objective |
|---|---|---|
| E1 | Chunk size | Determine whether smaller, more focused chunks reduce unrelated retrieval |
| E2 | Chunk overlap | Evaluate whether preserving context across chunk boundaries improves retrieval |
| E3 | Top-K | Reduce the number of unrelated chunks passed to the next stage |
| E4 | Similarity threshold | Reject low-relevance chunks without losing relevant evidence |
| E5 | Embedding model | Compare semantic retrieval quality using the same corpus and questions |

Each experiment must start from a recorded configuration and modify only one variable.

Q1 and Q7 should be monitored closely, along with Q6 to ensure negative-question rejection is preserved.

## 9. Remaining Limitations

1. The evaluation dataset contains only seven questions.
2. Only three knowledge documents and six chunks were indexed.
3. A perfect classification score on this small dataset does not prove production-level reliability.
4. Some retrieved chunks are unrelated to the exact question.
5. Retrieval scores alone do not establish whether a passage contains sufficient evidence.
6. The baseline retrieval scores are unavailable, preventing a direct before/after retrieval-quality comparison.
7. The current evaluation output does not record all retrieval configuration parameters.
8. The test run includes an Ollama-dependent diagnostic, so its execution environment must remain available for reproducibility.

One test was skipped:

`Memory_ShouldPersistAcrossProcessRestarts`

This is expected because the current memory implementation uses volatile in-memory storage.

## 10. Conclusion

The Week 10 evaluation successfully completed all seven questions.

The complete solution test suite reported:
- 224 passed tests.
- 1 skipped test.
- 0 failed tests.

Classification remained stable compared with the baseline:
- 5/5 supported questions correctly classified.
- 2/2 unsupported questions correctly rejected.
- No false positives or false negatives.

No measurable classification improvement was observed on the current seven-question dataset.

However, the retrieval diagnostics revealed two weaknesses:
- Q1 retrieves unrelated chunks alongside the relevant password-reset instructions.
- Q7 retrieves semantically related content with relatively high scores, although the requested database information is absent.

The controlled retrieval experiments further demonstrated that increasing the similarity threshold can reduce negative-query retrieval, but may also reject valid positive questions.

The next step is to investigate retrieval precision through controlled experiments. Chunking, Top-K, similarity threshold and embeddings must be evaluated independently.

The unchanged classification score is a useful experimental result: the Day 8 improvements preserved the existing behavior, while the Week 10 analysis identified opportunities to improve retrieval quality.

**Final status: Evaluation completed — retrieval optimization and answerability evaluation still required.**
