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