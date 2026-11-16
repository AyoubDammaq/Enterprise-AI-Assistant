# RAG Retrieval Parameter Experiments

## 1. Objective

The objective of these experiments is to evaluate how retrieval parameters influence the quality of document retrieval in the Enterprise AI Assistant RAG pipeline.

Two parameters are investigated:

- **Top-K:** Maximum number of chunks returned by the retriever.
- **Minimum Similarity Score:** Minimum similarity score required for a chunk to be included in the retrieval results.

The experiments aim to identify a reasonable initial configuration that retrieves relevant information while limiting unnecessary context.

These experiments are diagnostic. The selected parameters are provisional and must be validated against a larger evaluation dataset before being adopted as final production settings.

---

## 2. Experimental Environment

### 2.1 Project configuration

| Component | Configuration |
|---|---|
| Project | Enterprise-AI-Assistant |
| RAG implementation | Custom retrieval pipeline |
| Embedding provider | Ollama |
| Embedding model | nomic-embed-text |
| Embedding dimensions | 768 |
| Vector store | InMemoryVectorStore |
| Retrieval component | KnowledgeRetriever |
| Evaluation framework | xUnit |
| Backend framework | .NET 10 |

### 2.2 Evaluation corpus

The experiments use a small support-oriented knowledge corpus containing three indexed documents.

| Document | Content |
|---|---|
| support-faq.md | Password reset and account lockout |
| product-guide.md | Product configuration, stock thresholds and user roles |
| troubleshooting.md | Troubleshooting procedures and error resolution |

The corpus also contains `evaluation-questions.md`, which provides evaluation questions but is not indexed as a knowledge document.

**Corpus statistics:**

- Knowledge documents: 3
- Total indexed chunks: 6
- Embedding model: nomic-embed-text
- Embedding dimensions: 768

---

## 3. Evaluation Dataset

Seven questions are used to evaluate retrieval behavior.

Five questions are positive samples: the expected information exists in the corpus.

Two questions are negative samples: the requested information is not documented in the corpus.

| ID | Evaluation question | Expected source | Type |
|---|---|---|---|
| Q1 | How can I reset my password? | support-faq.md | Positive |
| Q2 | What should I do if my account is locked? | support-faq.md | Positive |
| Q3 | How can I configure the low-stock threshold? | product-guide.md | Positive |
| Q4 | What permissions does the Administrator role have? | product-guide.md | Positive |
| Q5 | How can I resolve the INV-001 error? | troubleshooting.md | Positive |
| Q6 | What is the monthly subscription price? | None | Negative |
| Q7 | Which database engine is used in production? | None | Negative |

The positive samples evaluate whether the retriever finds the expected source document.

The negative samples evaluate whether the retriever returns results even when the requested information is absent from the corpus.

---

## 4. Evaluation Metrics

The following metrics are used.

### 4.1 Hit Rate@1

Measures the proportion of positive questions for which the expected source document appears at rank 1.

\[
HitRate@1 = \frac{\text{Queries with expected source at rank 1}}{\text{Positive queries}}
\]

### 4.2 Hit Rate@2

Measures the proportion of positive questions for which the expected source document appears within the first two retrieved results.

\[
HitRate@2 = \frac{\text{Queries with expected source in top 2}}{\text{Positive queries}}
\]

### 4.3 Mean Reciprocal Rank (MRR)

Measures the average reciprocal rank of the first result matching the expected source.

\[
MRR = \frac{1}{N}\sum_{i=1}^{N}\frac{1}{rank_i}
\]

If the expected source is not retrieved, its reciprocal rank is zero.

### 4.4 Negative query inspection

For Q6 and Q7, we record whether the retriever returns any chunks.

This is a diagnostic indicator, not a direct relevance metric.

A returned chunk does not necessarily mean the requested information is present. The retrieved content must be inspected to determine whether it actually supports an answer.

---

## 5. Experiment A — Top-K Evaluation

### 5.1 Objective

Evaluate the impact of changing the maximum number of retrieved chunks while keeping the similarity threshold constant.

### 5.2 Configuration

- Minimum similarity score: `0.50`
- Top-K values: `1`, `3`, `5`
- Same corpus and evaluation questions
- Same embedding model
- Same retrieval implementation

### 5.3 Results

| Top-K | Hit Rate@1 | Hit Rate@2 | MRR | Q6 returned chunks | Q7 returned chunks |
|---:|---:|---:|---:|---:|---:|
| 1 | 100% | 100% | 1.0000 | 1 | 1 |
| 3 | 100% | 100% | 1.0000 | 3 | 3 |
| 5 | 100% | 100% | 1.0000 | 4 | 4 |

**Observation:** With Top-K = 5, only four chunks were returned for Q6 and Q7 because the similarity threshold filtered out the remaining chunks.

### 5.4 Detailed observations

All five positive questions retrieved their expected source document at rank 1 for all three Top-K configurations.

Examples with Top-K = 3:

| Question | First retrieved source | First result score |
|---|---|---:|
| Q1 | support-faq.md | 0.7761 |
| Q2 | support-faq.md | 0.8199 |
| Q3 | product-guide.md | 0.7629 |
| Q4 | product-guide.md | 0.7323 |
| Q5 | troubleshooting.md | 0.6184 |

Increasing Top-K did not improve Hit Rate@1, Hit Rate@2 or MRR on this dataset.

It did increase the number of retrieved chunks, potentially introducing additional context that may or may not be useful to the language model.

### 5.5 Interpretation

- Top-K = 1 provides the smallest context but limits the amount of information available to answer a question.
- Top-K = 3 provides additional context while keeping the retrieved set relatively small.
- Top-K = 5 retrieves more chunks without improving the measured positive-query ranking metrics.

### 5.6 Experiment A conclusion

**Top-K = 3** is selected as a provisional configuration.

It provides a reasonable balance between context availability and retrieval noise.

This choice is not proof that Top-K = 3 is optimal. Further evaluation with a larger corpus is required.

---

## 6. Experiment B — Minimum Similarity Threshold Evaluation

### 6.1 Objective

Evaluate how the minimum similarity score affects retrieval quality and the system's behavior when a question cannot be answered from the available knowledge base.

### 6.2 Configuration

- Top-K: `3`
- Minimum similarity thresholds: `0.50`, `0.60`, `0.70`, `0.80`
- Same corpus and evaluation questions
- Same embedding model
- A new vector store is created for each threshold.

### 6.3 Results

| Minimum similarity score | Hit Rate@1 | Hit Rate@2 | MRR | Q6 returned results? | Q7 returned results? |
|---:|---:|---:|---:|---|---|
| 0.50 | 100% | 100% | 1.0000 | Yes | Yes |
| 0.60 | 100% | 100% | 1.0000 | Yes | Yes |
| 0.70 | 80% | 80% | 0.8000 | No | Yes |
| 0.80 | 20% | 20% | 0.2000 | No | No |

### 6.4 Detailed observations

#### Threshold = 0.50

All five positive questions retrieved their expected source at rank 1.

Both negative questions also returned chunks.

This configuration provides good positive retrieval coverage but does not prevent retrieval for questions whose answers are absent from the corpus.

#### Threshold = 0.60

All five positive questions still retrieved their expected source at rank 1.

The positive-query metrics remain unchanged:

- Hit Rate@1: 100%
- Hit Rate@2: 100%
- MRR: 1.0000

Q6 and Q7 still returned results.

This threshold provides the same measured positive retrieval performance as 0.50 while filtering out some lower-scoring chunks.

#### Threshold = 0.70

The retriever achieved:

- Hit Rate@1: 80%
- Hit Rate@2: 80%
- MRR: 0.8000

Q5, concerning the INV-001 error, returned no chunks because its best similarity score was approximately `0.6184`.

Q6 returned no chunks, but Q7 still returned results.

This threshold is too restrictive for the current evaluation dataset because it rejects a valid positive question.

#### Threshold = 0.80

The retriever achieved:

- Hit Rate@1: 20%
- Hit Rate@2: 20%
- MRR: 0.2000

Only Q2 retrieved its expected source.

Most valid questions were rejected because their similarity scores were below the threshold.

This configuration is unsuitable for the current corpus.

### 6.5 Important finding: Similarity is not factual evidence

The experiments reveal an important limitation of using a single global similarity threshold.

The highest similarity score for the positive Q5 query is approximately:

`0.6184`

However, the negative Q6 query receives a chunk with a similarity score of:

`0.6856`

The negative Q7 query receives a chunk with a similarity score of:

`0.7884`

Therefore, a higher similarity score does not necessarily mean that the retrieved content contains the requested factual information.

**Conclusion:** Similarity measures semantic closeness, not factual support or answerability.

A similarity threshold alone cannot reliably distinguish all answerable questions from unanswerable questions.

---

## 7. Comparison and Selected Configuration

Based on the two experiments, the following configuration is selected for further testing:

| Parameter | Selected value | Reason |
|---|---:|---|
| Top-K | 3 | Provides additional context without unnecessarily increasing the retrieved set |
| Minimum Similarity Score | 0.60 | Preserves all five positive queries in the current evaluation dataset |

Provisional configuration:

```csharp
public sealed class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    public int TopK { get; set; } = 3;

    public double MinimumSimilarityScore { get; set; } = 0.60;
}
```

These values are experimental defaults, not final production parameters.

They should remain subject to validation as the corpus and evaluation dataset grow.

---

## 8. Limitations

The experiments provide useful initial observations, but several limitations must be considered.

### 8.1 Small evaluation corpus

The evaluation uses only three knowledge documents and six indexed chunks.

The results cannot be generalized to a larger enterprise knowledge base.

### 8.2 Limited number of evaluation questions

Only five positive and two negative questions are evaluated.

A larger dataset with different wording, difficulty levels and question types is required.

### 8.3 Source-level evaluation

The current metrics evaluate whether the expected source document appears in the retrieved results.

They do not establish whether the exact retrieved chunk contains all the information needed to answer the question.

Future evaluations should include manually annotated relevant chunks and chunk-level retrieval metrics.

### 8.4 Negative-query evaluation

For negative questions, the current diagnostic records whether chunks are returned.

It does not automatically determine whether those chunks contain relevant or sufficient evidence.

Manual inspection and answerability evaluation are necessary.

### 8.5 No complete answer-generation evaluation

These experiments focus on retrieval.

They do not yet measure whether the language model:

- Uses only the retrieved evidence.
- Produces factually supported answers.
- Avoids inventing missing information.
- Correctly abstains when evidence is insufficient.

These aspects will be evaluated in subsequent RAG experiments.

---

## 9. Next Steps

The next stage will focus on grounding and answerability.

### Step 1 — Inspect negative retrieval results

Manually inspect the chunks returned for Q6 and Q7.

Determine whether the retrieved content actually contains the requested information.

### Step 2 — Introduce answerability evaluation

Classify questions into three categories:

- **Answerable:** The retrieved context contains sufficient evidence.
- **Partially answerable:** The retrieved context contains some relevant information but does not fully support the requested answer.
- **Unanswerable:** The retrieved context does not contain sufficient evidence.

### Step 3 — Implement grounded generation

Ensure that the language model generates answers using the retrieved context rather than unsupported assumptions.

### Step 4 — Implement abstention

When the available context does not contain sufficient evidence, the assistant should clearly state that it cannot answer the question from the available documentation.

### Step 5 — Strengthen automated evaluation

Add explicit test assertions and expand the evaluation dataset.

Future metrics should include:

- Chunk-level Precision@K.
- Chunk-level Recall@K.
- MRR.
- Answer correctness.
- Groundedness.
- Abstention accuracy.

---

## 10. Final Conclusion

The experiments demonstrate that retrieval parameters have a direct impact on the balance between retrieving useful context and filtering potentially irrelevant chunks.

The current evaluation supports the following provisional configuration:

- **Top-K = 3**
- **Minimum Similarity Score = 0.60**

This configuration preserves the expected source for all five positive evaluation questions while avoiding some of the additional chunks returned at a lower threshold.

However, the negative-query experiments demonstrate that similarity filtering alone is insufficient to determine whether a question can be answered.

The next development priority is therefore not simply to increase the similarity threshold, but to improve answerability detection, grounded generation and abstention.
