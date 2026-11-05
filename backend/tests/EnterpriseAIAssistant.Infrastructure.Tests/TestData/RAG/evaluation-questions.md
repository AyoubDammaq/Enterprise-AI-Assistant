# RAG Evaluation Questions

## Questions with answers in the corpus

### Q1
**Question:** How can a user reset their Demo Portal password?

- Expected source: `support-faq.md`
- Expected facts: Select Forgot Password; a reset link is sent to the registered email; the link expires after 15 minutes.

### Q2
**Question:** How long is a Demo Portal account locked after failed login attempts?

- Expected source: `support-faq.md`
- Expected facts: Five consecutive failed attempts trigger a temporary lockout lasting 10 minutes.

### Q3
**Question:** What is the default low-stock threshold in Atlas Inventory?

- Expected source: `product-guide.md`
- Expected facts: The default threshold is 10 units.

### Q4
**Question:** Which user role can modify inventory settings and manage users?

- Expected source: `product-guide.md`
- Expected facts: The Administrator role manages users, product categories, and application settings.

### Q5
**Question:** How should a user resolve error INV-001?

- Expected source: `troubleshooting.md`
- Expected facts: Enter a stock quantity greater than or equal to zero and submit the update again.

## Questions whose answers are absent from the corpus

### Q6
**Question:** What is the monthly subscription price of Atlas Inventory?

- Expected result: No answer supported by the corpus.
- Relevant source: None.

### Q7
**Question:** Which database engine does Atlas Inventory use in production?

- Expected result: No answer supported by the corpus.
- Relevant source: None.

## Evaluation procedure

For each question:

1. Execute retrieval without calling the chat model.
2. Record the retrieved chunk IDs, source documents, and similarity scores if available.
3. Check whether the expected source appears in the Top-K results.
4. For Q6 and Q7, verify that the application does not treat unrelated passages as evidence for an answer.
5. Record the observed result and mark the test PASS or FAIL.

Do not mark a test as PASS merely because the generated answer sounds plausible.