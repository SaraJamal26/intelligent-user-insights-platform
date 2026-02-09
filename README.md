**Overview:**
A .NET 8 Windows Service–capable application that hosts a self-hosted Web API for user management with file-based JSON persistence (no DB).
It integrates with a separate Node.js AI microservice via REST to populate user data (sentiment, tags, engagement, insights).

Services
1) .NET User Service (Web API)

Key features

CRUD APIs: GET/POST/PUT/DELETE /api/users

File persistence: loads/saves users to JSON on disk

AI enrichment endpoint: POST /api/users/{id}/analyze

Aggregation endpoint: GET /api/users/insights

Uses HttpClientFactory, DI, async/await

2) Node AI Service

Endpoints

POST /api/ai/sentiment

POST /api/ai/tags

POST /api/ai/insights

GET /health


How to run locally
Start AI service

Install Ollama and pull model:

ollama pull llama3


ai-service/.env

PORT=3001
AI_PROVIDER=ollama
OLLAMA_URL=http://localhost:11434
OLLAMA_MODEL=llama3
MOCK_AI=false


Run:

node server.js

Start .NET service

Set AI base URL:

AiService__BaseUrl=http://localhost:3001


Run from Visual Studio (F5) or:

dotnet run
