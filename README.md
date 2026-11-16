# Enterprise AI Assistant

This repository contains the backend service for the Enterprise AI Assistant. It is built using .NET, following Clean Architecture principles to ensure a modular, scalable, and maintainable codebase.

## Architecture

The backend is structured using a Clean Architecture approach, separating concerns into distinct layers:

*   **Domain**: Contains the core business logic, entities, and value objects of the application. It has no dependencies on other layers.
*   **Application**: Orchestrates the data flow and use cases. It depends on the Domain layer but not on the presentation or infrastructure layers.
*   **Infrastructure**: Handles external concerns such as database access, file systems, and interactions with other external services. It depends on the Application layer.
*   **API**: The presentation layer, which exposes the application's functionality via a Web API. It depends on the Application and Infrastructure layers.

## Features

*   **ASP.NET Core Web API**: A robust and scalable API built on the latest .NET framework.
*   **Clean Architecture**: A well-organized solution structure that promotes separation of concerns.
*   **Docker Support**: Includes a Dockerfile for easy containerization and deployment.
*   **Health Monitoring**: Comprehensive health checks to monitor the application's status and its dependencies.
*   **Health Checks UI**: A user-friendly dashboard to visualize the health status of the application.

## Project Structure

```
.
├── backend/
│   ├── src/
│   │   ├── EnterpriseAIAssistant.API/          # Presentation Layer (Web API)
│   │   ├── EnterpriseAIAssistant.Application/  # Application Layer
│   │   ├── EnterpriseAIAssistant.Domain/       # Domain Layer
│   │   └── EnterpriseAIAssistant.Infrastructure/ # Infrastructure Layer
│   └── tests/                                  # xUnit tests for each layer
├── docs/                                       # Documentation files
├── frontend/                                   # Placeholder for frontend application
└── scripts/                                    # Utility and build scripts
```

## Getting Started

### Prerequisites

*   .NET 10.0 SDK (or the version specified in `global.json` / `.csproj` files)
*   Docker (Optional, for containerized execution)

### Running Locally

1.  Clone the repository:
    ```sh
    git clone https://github.com/ayoubdammaq/enterprise-ai-assistant.git
    cd enterprise-ai-assistant/backend
    ```

2.  Restore dependencies:
    ```sh
    dotnet restore EnterpriseAIAssistant.slnx
    ```

3.  Run the API project:
    ```sh
    dotnet run --project src/EnterpriseAIAssistant.API/EnterpriseAIAssistant.API.csproj
    ```

The application will be available at `http://localhost:5035` and `https://localhost:7046`.

### Running with Docker

1.  Navigate to the API project directory:
    ```sh
    cd enterprise-ai-assistant/backend/src/EnterpriseAIAssistant.API
    ```

2.  Build the Docker image:
    ```sh
    docker build -t enterprise-ai-assistant .
    ```

3.  Run the Docker container:
    ```sh
    docker run -p 8080:8080 -p 8081:8081 enterprise-ai-assistant
    ```

The application will be available at `http://localhost:8080` and `https://localhost:8081`.

## Health Checks

The application includes a comprehensive health monitoring system.

*   **Health Check API**: Access the raw health status data by navigating to `/api/health`.
*   **Health Check UI Dashboard**: View a user-friendly dashboard of the application's health by navigating to `/healthcheck-ui`.

The following checks are configured:
*   **SQL Server**: Checks the connectivity to the feedback database.
*   **Memory Check**: Monitors the application's memory usage and reports a degraded status if it exceeds a configured threshold.
*   **Remote Endpoint**: Verifies connectivity to essential external services.
*   **URL Group**: Pings the application's own base URL to ensure it is responsive.

## Testing

The solution includes dedicated test projects for each backend layer,
using xUnit.

### Prerequisites

- .NET 10 SDK
- Ollama installed and running
- `llama3.2` chat model
- `nomic-embed-text` embedding model

Pull the required models:

```powershell
ollama pull llama3.2
ollama pull nomic-embed-text
```

### Run the complete test suite

From the `backend` directory, configure the RAG corpus path:

```powershell
$env:RAG_DOCUMENTS_PATH = (Resolve-Path ".\tests\EnterpriseAIAssistant.Infrastructure.Tests\TestData\RAG\Corpus").Path
```

Then execute:

```powershell
dotnet test .\EnterpriseAIAssistant.slnx
```

The integration and RAG diagnostic tests require a running Ollama service.

For detailed RAG evaluation results and experimental configurations,
see:

- `docs/RAG-Evaluation.md`
- `docs/rag/evaluation-baseline.md`
- `docs/rag/evaluation-results-week-10.md`
- `docs/rag/retrieval-parameter-experiments.md`
