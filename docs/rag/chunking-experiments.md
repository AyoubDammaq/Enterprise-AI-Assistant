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

