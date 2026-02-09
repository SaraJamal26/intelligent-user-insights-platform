## Overview
A **.NET 8 Windows Service–capable** application that hosts a self-hosted Web API for user management with file-based JSON persistence (no DB).  
It integrates with a separate Node.js AI microservice via REST to populate user data (sentiment, tags, engagement, insights).


## Services

### 1) .NET User Service (Web API)

**Key features**
- CRUD APIs: `GET / POST / PUT / DELETE  /api/users`
- File persistence: loads/saves users to JSON on disk
- AI enrichment endpoint: `POST /api/users/{id}/analyze`
- Aggregation endpoint: `GET /api/users/insights`
- Uses `HttpClientFactory`, DI, and `async/await`


### 2) Node AI Service

**Endpoints**
- `POST /api/ai/sentiment`
- `POST /api/ai/tags`
- `POST /api/ai/insights`
- `GET /health`


## How to run locally

### 1) Start AI service (Ollama)

Install Ollama and pull model:
```bash
ollama pull llama3
```

Create ai-service/.env:
```bash
PORT=3001
AI_PROVIDER=ollama
OLLAMA_URL=http://localhost:11434
OLLAMA_MODEL=llama3
MOCK_AI=false
```


Start the AI service:
```bash
node server.js
```


Verify:
```bash
http://localhost:3001/health
```

Step 2: Start the .NET User Service

Set AI service base URL:
```bash
set AiService__BaseUrl=http://localhost:3001
```


Run the service:
```bash
dotnet run
```

Or run from Visual Studio (F5).

## Run with Docker

### From the repository root:
```bash

docker compose up --build
```


### Services will be available at:

.NET API: http://localhost:8080

Node AI: http://localhost:3001
