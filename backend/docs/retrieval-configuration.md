# Retrieval Configuration

## Overview

The retrieval configuration controls how many document chunks can be
returned for a search and the minimum cosine similarity required for
a chunk to be included.

Configuration is managed through `RetrievalOptions` in the
Infrastructure project.

## Default values

When no configuration overrides are provided, the defaults are:

| Setting | Default | Description |
|---|---:|---|
| `Retrieval:TopK` | `5` | Number of chunks requested by default |
| `Retrieval:MinimumSimilarityScore` | `0.50` | Minimum cosine similarity score |

The shared maximum allowed value for `TopK` is defined once in
`RetrievalLimits.MaximumTopK` and is currently `20`.

Valid ranges:

- `TopK`: integer between `1` and `20`, inclusive.
- `MinimumSimilarityScore`: finite number between `0.0` and `1.0`,
  inclusive.

The options registration validates these values and uses
`ValidateOnStart()` so invalid configuration is detected at application
startup.

## API configuration overrides

The API application overrides the defaults in
`src/EnterpriseAIAssistant.API/appsettings.json`:

```json
{
  "Retrieval": {
    "TopK": 3,
    "MinimumSimilarityScore": 0.60
  }
}
```

Therefore, the API uses:

- `TopK = 3`
- `MinimumSimilarityScore = 0.60`

These are API-specific configuration values. They do not change the
default values declared by `RetrievalOptions`.

## Per-request TopK

A caller may provide a `TopK` value for an individual retrieval request.
The value must remain within the shared range from `1` to `20`.
Invalid values are rejected before generating the query embedding.

## Centralized limit

Do not define another `MaximumTopK` constant in a retrieval implementation.
Use `RetrievalLimits.MaximumTopK` to keep validation consistent across
the retriever, vector store, and dependency-injection options validation.
