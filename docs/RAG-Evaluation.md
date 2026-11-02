# RAG Retrieval Evaluation

## 1. Objective

Evaluate the retrieval component independently of the LLM.

The objective is to verify whether the system retrieves the expected documents and relevant chunks based on user questions.

## 2. Test Dataset

| ID | Question | Expected Source |
|---|---|---|
| Q1 | How can I reset my password? | support-faq.md |
| Q2 | What authentication methods are supported? | support-faq.md |
| Q3 | What are the main product features? | product-guide.md |
| Q4 | How can I solve connection error 500? | troubleshooting.md |
| Q5 | What is the recommended configuration? | product-guide.md |

Add questions based on the actual content of the test corpus.

## 3. Retrieval Results

| ID | Expected Source | Retrieved Source | Found in Top-K | Result |
|---|---|---|---|---|
| Q1 | support-faq.md | To complete | To complete | Pending |
| Q2 | support-faq.md | To complete | To complete | Pending |
| Q3 | product-guide.md | To complete | To complete | Pending |
| Q4 | troubleshooting.md | To complete | To complete | Pending |
| Q5 | product-guide.md | To complete | To complete | Pending |

## 4. Query Reformulation

Test different formulations of the same question.

| Original Question | Alternative Formulation | Expected Source | Found |
|---|---|---|---|
| How can I reset my password? | I forgot my password. | support-faq.md | Pending |
| How can I reset my password? | I cannot access my account. | support-faq.md | Pending |

## 5. Negative Test

Question:

"What is the company's annual revenue?"

Expected behavior:

The retrieval system should not return an unrelated document as if it contained the answer.

Record the retrieved results and determine whether a relevance threshold or additional filtering is needed.

## 6. Evaluation Metric

Retrieval Success Rate:

Successful Queries / Total Queries × 100

A query is successful when its expected source appears in the Top-K retrieved results.

## 7. Observations

- Best retrieval results:
- Failed retrieval queries:
- Impact of query reformulation:
- Impact of chunking:
- Potential improvements:

## 8. Conclusion

Summarize the retrieval performance, known limitations, and improvements to investigate in the next iteration.