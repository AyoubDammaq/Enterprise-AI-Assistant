# Chunking Experiments — Week 10

## 1. Objective

Compare different chunking strategies and evaluate their impact on document structure, chunk distribution, and RAG retrieval quality.

The objective is to identify whether a different chunking strategy provides measurable improvements over the existing implementation.

## 2. Experimental Environment

| Parameter | Value |
|---|---|
| Embedding Model | nomic-embed-text |
| Embedding Dimension | 768 |
| Documents | support-faq.md, product-guide.md, troubleshooting.md |
| Top-K | 5 |
| Similarity Threshold | 0.50 |
| Chunk Size | 800 characters |
| Chunk Overlap | 100 characters |
| Vector Store | InMemoryVectorStore |
| Embedding Provider | Ollama |
| Number of Evaluation Questions | 7 |

All strategies were tested using the same corpus, embedding model, retrieval configuration, and evaluation questions.

Chunk lengths are measured using C# `string.Length`, which counts UTF-16 code units rather than tokens or Unicode grapheme clusters.

## 3. Strategies Compared

- FixedSize: Splits documents into fixed-length windows with overlap.
- ParagraphAware: Preserves paragraph boundaries while respecting the configured chunk size.
- RecursiveCharacter: Splits content hierarchically using separators such as paragraphs, lines, sentences, and spaces.
- Current implementation: The original implementation, equivalent to ParagraphAware.

## 4. Chunk Statistics

| Strategy | Total Chunks | Min Length | Average Length | Max Length |
|---|---:|---:|---:|---:|
| Current (ParagraphAware) | 6 | 280 | 569.33 | 766 |
| FixedSize | 6 | 354 | 635.00 | 800 |
| ParagraphAware | 6 | 280 | 569.33 | 766 |
| RecursiveCharacter | 6 | 384 | 635.00 | 796 |

### Observations

- All three strategies generated exactly 6 chunks.
- FixedSize generated chunks with lengths between 354 and 800 characters.
- ParagraphAware produced the shortest average chunk length.
- RecursiveCharacter produced a similar average length to FixedSize.
- All strategies respected the configured maximum chunk size in this experiment.

## 5. Retrieval Results

Five positive questions were evaluated against their expected source documents. Two additional questions tested retrieval when the expected information was absent from the corpus.

| Question | Expected Source | Current | FixedSize | ParagraphAware | Recursive |
|---|---|---|---|---|---|
| Q1 | support-faq.md | Rank 1 | Rank 1 | Rank 1 | Rank 1 |
| Q2 | support-faq.md | Rank 1 | Rank 1 | Rank 1 | Rank 1 |
| Q3 | product-guide.md | Rank 1 | Rank 1 | Rank 1 | Rank 1 |
| Q4 | product-guide.md | Rank 1 | Rank 1 | Rank 1 | Rank 1 |
| Q5 | troubleshooting.md | Rank 1 | Rank 1 | Rank 1 | Rank 1 |

### Retrieval Metrics

| Metric | FixedSize | ParagraphAware | RecursiveCharacter |
|---|---:|---:|---:|
| HitRate@1 | 100% | 100% | 100% |
| HitRate@2 | 100% | 100% | 100% |
| Mean Reciprocal Rank (MRR) | 1.0000 | 1.0000 | 1.0000 |

These metrics are based on expected source-document matching for the five positive questions. They do not measure the relevance of every retrieved chunk.

### Negative Query Evaluation

| Question | Expected Result | FixedSize | ParagraphAware | RecursiveCharacter |
|---|---|---|---|---|
| Q6: What is the monthly subscription price? | No relevant information | 0 results | 0 results | 0 results |
| Q7: Which database engine is used in production? | No relevant information | 4 irrelevant chunks | 4 irrelevant chunks | 5 irrelevant chunks |

Manual inspection of all retrieved Q7 chunks found that none contained useful evidence to answer the question.

This demonstrates that a high similarity score does not necessarily indicate relevant information.

## 6. Observations

### Current Strategy — ParagraphAware

**Strengths**
- Preserves paragraph structure.
- Produces readable chunks.
- Achieves 100% HitRate@1 on the positive evaluation questions.
- Provides a reliable baseline for comparison.

**Weaknesses**
- Can retrieve semantically related but irrelevant chunks.
- Does not completely eliminate false-positive retrievals for questions whose answers are absent from the corpus.

### FixedSize

**Strengths**
- Simple and predictable implementation.
- Produces chunks close to the configured maximum size.
- Achieves the same source-based retrieval metrics as the other strategies.

**Weaknesses**
- Can split content at awkward boundaries.
- Some observed chunks contained incomplete fragments, such as a partially split list item.
- Does not explicitly preserve paragraph structure.

### ParagraphAware

**Strengths**
- Preserves paragraph boundaries and improves readability.
- Produces the smallest average chunk length in this experiment.
- Maintains the same retrieval metrics as the other strategies.

**Weaknesses**
- Paragraph boundaries do not always correspond to semantic boundaries.
- Still retrieves irrelevant chunks for some negative queries.

### RecursiveCharacter

**Strengths**
- Uses a hierarchical splitting approach.
- Preserves larger text units when possible.
- Achieves the same source-based retrieval metrics as the other strategies.

**Weaknesses**
- Some observed chunks contained small fragments at boundaries.
- Retrieved five irrelevant chunks for Q7, compared with four for the other strategies.
- No measurable retrieval advantage was demonstrated in this experiment.

## 7. Conclusion

**Best strategy:** No strategy demonstrated a measurable advantage in the current evaluation.

**Why:**
- All three strategies generated six chunks.
- All achieved 100% HitRate@1 and HitRate@2 on the five positive questions.
- All achieved an MRR of 1.0000.
- None retrieved relevant evidence for Q7.
- RecursiveCharacter did not demonstrate a qualitative improvement over ParagraphAware.

**Remaining limitations:**
- The evaluation corpus is small, containing only three knowledge documents.
- Only five positive retrieval questions were used to calculate the retrieval metrics.
- The metrics evaluate source-document matching rather than individual chunk relevance.
- Similarity scores alone cannot establish whether retrieved content answers a question.
- More diverse documents and evaluation questions are needed.

**Strategy selected for the project:** ParagraphAware remains the default strategy provisionally. This decision is based on its preservation of paragraph structure and continuity with the existing implementation, not on proven superiority in retrieval performance.

## 8. Future Improvements

- Sentence-aware chunking.
- Token-based chunk sizing instead of character-based sizing.
- Semantic chunking based on embedding similarity.
- Chunk-level precision and ranking metrics.
- A larger evaluation dataset with manually labelled relevant and irrelevant chunks.
- Evaluation of retrieval quality across different chunk sizes and overlap values.
- Testing retrieval thresholds and reranking techniques.


## 9. Reproducibility

### Prerequisites

The chunking comparison diagnostic requires:

- A compatible .NET SDK for the solution.
- Ollama installed and running locally.
- The `nomic-embed-text` embedding model available in Ollama.
- The evaluation corpus containing:
  - `support-faq.md`
  - `product-guide.md`
  - `troubleshooting.md`

### Corpus Configuration

The diagnostic reads the corpus location from the `RAG_DOCUMENTS_PATH` environment variable.

Example PowerShell configuration (from the `backend` directory):

```powershell
$env:RAG_DOCUMENTS_PATH = (Resolve-Path ".\tests\EnterpriseAIAssistant.Infrastructure.Tests\TestData\RAG\Corpus").Path
```

The evaluation questions are defined in the diagnostic test itself. They are not indexed as knowledge documents.

### Embedding Configuration

The diagnostic uses:

| Parameter | Value |
|---|---|
| Provider | Ollama |
| Endpoint | http://localhost:11434 |
| Embedding Model | nomic-embed-text |
| Embedding Dimensions | 768 |
| HTTP Timeout | 60 seconds |

Verify model availability with:

```powershell
ollama list
```

### Running the Experiment

From the `backend` directory, execute:

```powershell
dotnet test .\tests\EnterpriseAIAssistant.Infrastructure.Tests\EnterpriseAIAssistant.Infrastructure.Tests.csproj --filter "FullyQualifiedName~DiagnoseRetrievalAcrossChunkingStrategies" --logger "console;verbosity=normal"
```

The diagnostic belongs to the `Integration` test category and requires a running Ollama instance and a valid corpus path.

### Evaluation Methodology

The experiment uses seven questions:

- Five positive questions (Q1-Q5), each associated with an expected source document.
- Two negative questions (Q6-Q7), for which no expected source document is defined.

HitRate@1, HitRate@2, and MRR are calculated using only the five positive questions.

For negative questions, the diagnostic records whether any chunks were returned. A returned chunk is not automatically considered irrelevant: its content must be reviewed to determine whether it provides useful evidence.

### Reproducibility Considerations

For meaningful comparisons, keep the following conditions identical across strategies:

- Corpus content.
- Embedding model and dimensions.
- Top-K and similarity threshold.
- Chunk size and overlap.
- Evaluation questions.

The historical results documented above describe the original experiment. A subsequent execution should be recorded separately, with its execution date and observed results, rather than silently replacing historical measurements.


## 10. Validation Run — November 20, 2026

### Execution Environment

| Parameter | Observed Value |
|---|---|
| Runtime | .NET 10.0 |
| Embedding Provider | Ollama |
| Embedding Model | nomic-embed-text:latest |
| Embedding Dimensions | 768 |
| Corpus | Three Markdown documents |
| Diagnostic Test | DiagnoseRetrievalAcrossChunkingStrategies |
| Test Result | Passed |
| Total Tests | 1 |
| Failed Tests | 0 |
| Skipped Tests | 0 |

### Observed Retrieval Metrics

| Strategy | Chunks | HitRate@1 | HitRate@2 | MRR | Negative Queries With Results |
|---|---:|---:|---:|---:|---:|
| FixedSize | 6 | 100% | 100% | 1.0000 | 1 |
| ParagraphAware | 6 | 100% | 100% | 1.0000 | 1 |
| RecursiveCharacter | 6 | 100% | 100% | 1.0000 | 1 |

### Negative Query Analysis

- Q6 returned zero results for all three strategies.
- Q7 returned four chunks with FixedSize, four with ParagraphAware, and five with RecursiveCharacter.
- A keyword inspection of the three corpus documents found no explicit database-engine or production-database information matching the search terms used during validation.
- Q7 results therefore represent potentially unsupported retrieval and require chunk-level relevance review before being classified as false positives.

### Interpretation and Limitations

- The observed metrics reproduce the historical retrieval results.
- The diagnostic logs retrieval metrics but does not assert a fixed performance baseline.
- The evaluation uses only five positive and two negative questions over three documents.
- Successful execution does not establish production readiness or guarantee retrieval quality on a larger corpus.
- Further evaluation should include chunk-level relevance judgments, a larger corpus, and additional negative queries.


## 11. C4 — Retrieval Evaluation and Abstention — November 25, 2026

### 11.1 Evaluation Dataset

The C4 evaluation uses a centralized dataset containing seven questions:

| Category | Questions | Count |
|---|---|---:|
| Positive | Q1–Q5 | 5 |
| Negative | Q6–Q7 | 2 |
| **Total** | **Q1–Q7** | **7** |

The evaluation corpus contains three Markdown documents:

- `support-faq.md`
- `product-guide.md`
- `troubleshooting.md`

Each positive question is associated with manually annotated relevant chunk identifiers generated using the `ParagraphAware` chunking configuration.

The negative questions represent information expected to be absent from the corpus.

### 11.2 Evaluation Metrics

Positive and negative questions are evaluated separately.

Positive retrieval metrics:

- Precision@K: proportion of retrieved chunks that are relevant.
- Recall@K: proportion of annotated relevant chunks retrieved.
- Hit Rate@K: proportion of questions for which at least one relevant result is retrieved within the first K results.
- Mean Reciprocal Rank (MRR): average reciprocal rank of the first relevant result.

Negative retrieval metrics:

- Abstained query: no retrieval results were returned.
- Query with results: one or more retrieval results were returned.
- Abstention Rate: proportion of negative questions for which retrieval returned no results.

A returned result is not automatically a false positive. Its content must be reviewed to determine whether it provides useful evidence.

### 11.3 Experiment A — TopK Evaluation

This experiment evaluates the impact of the number of retrieved chunks on positive retrieval metrics.

The evaluation uses the same embedding model, corpus, similarity threshold (0.50), and five positive questions.

#### Positive Retrieval Results

| TopK | Precision@K | Recall@K | Hit Rate@K | MRR |
|---:|---:|---:|---:|---:|
| 1 | 100% | 100% | 100% | 1.0000 |
| 3 | 33.33% | 100% | 100% | 1.0000 |
| 5 | 20% | 100% | 100% | 1.0000 |

All five positive questions retrieved their annotated relevant chunk at rank 1.

Precision decreases as more chunks are retrieved, while recall remains at 100% because all annotated relevant chunks are retrieved.

#### Negative Query Results

Both negative questions returned retrieval results at the 0.50 similarity threshold.

| Metric | Result |
|---|---:|
| Negative Questions | 2 |
| Queries With Results | 2 |
| Abstained Queries | 0 |
| Abstention Rate | 0% |

For both negative questions, the `product-guide.md::chunk-0000` chunk was retrieved despite not providing the requested information.

This demonstrates that successful positive retrieval does not guarantee appropriate abstention for questions outside the knowledge base.

### 11.4 Experiment B — Similarity Threshold Evaluation

This experiment evaluates the impact of different minimum similarity thresholds.

The experiment uses TopK = 3 and evaluates four thresholds.

#### Retrieval Results

| Threshold | Hit Rate@1 | Hit Rate@2 | MRR | Negative Queries With Results | Abstention Rate |
|---:|---:|---:|---:|---:|---:|
| 0.50 | 100% | 100% | 1.0000 | 2/2 | 0% |
| 0.60 | 100% | 100% | 1.0000 | 2/2 | 0% |
| 0.70 | 80% | 80% | 0.8000 | 1/2 | 50% |
| 0.80 | 20% | 20% | 0.2000 | 0/2 | 100% |

#### Observations

- At thresholds 0.50 and 0.60, all positive questions retrieve relevant information, but neither negative question is rejected.
- At 0.70, positive retrieval performance decreases to 80%, while one of the two negative questions is rejected.
- At 0.80, both negative questions are rejected, but positive Hit Rate@1 decreases to 20%.
- At 0.70, Q5 no longer retrieves its expected relevant chunk, while Q6 is rejected and Q7 still returns a result.
- At 0.80, only Q2 retrieves a relevant result among the five positive questions.

Increasing the similarity threshold improves abstention but can also reject relevant information.

A threshold of 0.80 is therefore too restrictive for the current evaluation dataset.

These results do not justify selecting a production threshold. A larger evaluation dataset is required to assess the trade-off reliably.

### 11.5 Experiment C — Chunking Strategy Comparison

This experiment compares FixedSize, ParagraphAware, and RecursiveCharacter using the same corpus and evaluation questions.

#### Positive Retrieval Results

| Strategy | Chunks | HitRate@1 | HitRate@2 | MRR |
|---|---:|---:|---:|---:|
| FixedSize | 6 | 100% | 100% | 1.0000 |
| ParagraphAware | 6 | 100% | 100% | 1.0000 |
| RecursiveCharacter | 6 | 100% | 100% | 1.0000 |

All five positive questions retrieved their expected relevant chunk at rank 1 across the three strategies.

#### Negative Query and Abstention Results

| Strategy | Negative Questions | Queries With Results | Abstained Queries | Abstention Rate |
|---|---:|---:|---:|---:|
| FixedSize | 2 | 2 | 0 | 0% |
| ParagraphAware | 2 | 2 | 0 | 0% |
| RecursiveCharacter | 2 | 2 | 0 | 0% |

Detailed retrieval counts:

| Question | FixedSize | ParagraphAware | RecursiveCharacter |
|---|---:|---:|---:|
| Q6: Monthly subscription price | 4 | 4 | 4 |
| Q7: Production database engine | 4 | 4 | 5 |

None of the three strategies abstained on either negative question.

#### Observations

- All strategies generated six chunks.
- All achieved identical positive retrieval metrics.
- No measurable retrieval advantage was demonstrated by changing the chunking strategy.
- All strategies returned results for both negative questions.
- RecursiveCharacter returned one additional result for Q7.

The experiment does not establish that the three strategies are equivalent on larger or more diverse corpora.

### 11.6 Overall Limitations

The C4 evaluation has several important limitations:

- Only seven questions are included: five positive and two negative.
- The corpus contains only three Markdown documents.
- Relevant chunk annotations are manually defined.
- Results depend on the selected embedding model and retrieval configuration.
- The experiments do not establish generalization to larger or more diverse corpora.
- Similarity scores do not prove that retrieved chunks contain sufficient evidence to answer a question.
- Retrieval metrics do not measure the correctness or factual grounding of generated LLM answers.
- Negative retrieval results require manual relevance review before being classified as false positives.

The observed 100% positive retrieval metrics should be interpreted as preliminary results on a small benchmark, not as evidence of general or production-level performance.

### 11.7 Conclusion

C4 introduces a centralized evaluation dataset, positive retrieval metrics, and a separate abstention evaluation for negative questions.

The three experiments provide complementary observations:

1. **TopK evaluation:** increasing the number of retrieved chunks preserves recall but reduces precision.
2. **Similarity threshold evaluation:** higher thresholds improve abstention but can significantly reduce positive retrieval performance.
3. **Chunking strategy comparison:** all three strategies achieve identical positive retrieval metrics on the current dataset, with no demonstrated advantage in abstention.

The current ParagraphAware strategy remains a reasonable baseline, but the experiments do not establish its superiority.

Further work should focus on:

- Expanding the evaluation dataset with more positive and negative questions.
- Improving manually annotated chunk-level relevance judgments.
- Evaluating threshold selection on a larger benchmark.
- Testing reranking and context filtering.
- Evaluating whether generated answers are supported by retrieved evidence.

The results are preliminary and should not be used alone to establish production readiness.

### 11.8 Test Provenance and Reproducibility

The C4 evaluation was executed on the following environment:

| Property | Value |
|---|---|
| Git branch | `week-11/advanced-rag` |
| Base commit | `08698b45faf4a304cb0b39576a2e4e7976a50313` |
| Working tree | Modified; C4 changes were not committed |
| .NET SDK | 10.0.402 |
| Target framework | `net10.0` |
| Test command | `dotnet test .\EnterpriseAIAssistant.slnx --no-restore --logger "console;verbosity=normal"` |
| Total tests | 248 |
| Passed | 247 |
| Failed | 0 |
| Skipped | 1 |

**Important:** These results correspond to the working tree based on the specified commit, including uncommitted C4 evaluation changes. They must not be interpreted as test results for the base commit alone.

The skipped test concerns persistence across process restarts in the in-memory storage implementation. This behavior is expected because the current implementation does not provide durable storage.

To reproduce the evaluation, apply the corresponding C4 working-tree changes, configure the required RAG corpus path, ensure Ollama is running with the required models, and execute the test command above.