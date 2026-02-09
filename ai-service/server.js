// server.js
import express from "express";
import cors from "cors";
import dotenv from "dotenv";
import crypto from "crypto";
import { GoogleGenerativeAI } from "@google/generative-ai";

dotenv.config();

const app = express();
app.use(cors());
app.use(express.json());

const port = process.env.PORT || 3001;

const PROVIDER = (process.env.AI_PROVIDER || "ollama").toLowerCase(); // ollama | gemini | mock
const MOCK_AI = (process.env.MOCK_AI || "false").toLowerCase() === "true";

// Ollama
const OLLAMA_URL = process.env.OLLAMA_URL || "http://localhost:11434";
const OLLAMA_MODEL = process.env.OLLAMA_MODEL || "llama3";

// Gemini (Google AI Studio)
const GEMINI_API_KEY = process.env.GEMINI_API_KEY || "";
const GEMINI_MODEL = process.env.GEMINI_MODEL || "gemini-2.0-flash-lite"; // change if needed
const genAI = GEMINI_API_KEY ? new GoogleGenerativeAI(GEMINI_API_KEY) : null;

// ---------------- Correlation ID ----------------
app.use((req, res, next) => {
  const header = "x-correlation-id";
  const cid = req.headers[header] || crypto.randomUUID();
  req.correlationId = cid;
  res.setHeader("X-Correlation-ID", cid);
  next();
});

// ---------------- Health ----------------
app.get("/health", async (req, res) => {
  let dep = "unknown";

  try {
    if (PROVIDER === "ollama") {
      const r = await fetch(`${OLLAMA_URL}/api/tags`);
      dep = r.ok ? "reachable" : "unreachable";
    } else if (PROVIDER === "gemini") {
      dep = genAI ? "configured" : "missing_key";
    } else {
      dep = "n/a";
    }
  } catch {
    dep = "unreachable";
  }

  res.json({ status: "ok", provider: PROVIDER, dependency: dep, mock: MOCK_AI });
});

// ---------- Helpers ----------
function safeJsonParse(text, fallback) {
  try {
    const trimmed = (text || "").trim();
    const start = trimmed.indexOf("{");
    const end = trimmed.lastIndexOf("}");
    if (start >= 0 && end > start) return JSON.parse(trimmed.slice(start, end + 1));
    return JSON.parse(trimmed);
  } catch {
    return { ...fallback, fallback: true };
  }
}

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function ollamaJson(prompt, fallbackObj) {
  try {
    const r = await fetch(`${OLLAMA_URL}/api/generate`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        model: OLLAMA_MODEL,
        prompt: `Return ONLY valid JSON. No markdown. No backticks.\n${prompt}`,
        stream: false,
      }),
    });

    if (!r.ok) {
      const errText = await r.text();
      console.error("Ollama HTTP error:", r.status, errText);
      return { ...fallbackObj, fallback: true };
    }

    const data = await r.json();
    return safeJsonParse(data.response, fallbackObj);
  } catch (e) {
    console.error("Ollama call failed:", e?.message || e);
    return { ...fallbackObj, fallback: true };
  }
}

async function geminiJson(prompt, fallbackObj) {
  if (!genAI) return { ...fallbackObj, fallback: true, error: "GEMINI_API_KEY missing" };

  const model = genAI.getGenerativeModel({ model: GEMINI_MODEL });

  const fullPrompt = `Return ONLY valid JSON. No markdown. No backticks.\n${prompt}`;

  // Simple retry on 429/rate limit (one retry after a short delay)
  for (let attempt = 1; attempt <= 2; attempt++) {
    try {
      const result = await model.generateContent(fullPrompt);
      const text = result.response.text();
      return safeJsonParse(text, fallbackObj);
    } catch (e) {
      const msg = String(e?.message || e);
      const is429 = msg.includes("429") || msg.toLowerCase().includes("too many requests");
      console.error(`Gemini call failed (attempt ${attempt}):`, msg);

      if (is429 && attempt === 1) {
        await sleep(2000); 
        continue;
      }

      return { ...fallbackObj, fallback: true, error: msg };
    }
  }

  return { ...fallbackObj, fallback: true };
}

async function aiJson(prompt, fallbackObj) {
  if (MOCK_AI || PROVIDER === "mock") return { ...fallbackObj, fallback: true };

  if (PROVIDER === "gemini") return await geminiJson(prompt, fallbackObj);

  // default to Ollama
  return await ollamaJson(prompt, fallbackObj);
}

// ---------- Endpoints ----------

// POST /api/ai/sentiment  { text }
app.post("/api/ai/sentiment", async (req, res) => {
  const { text } = req.body || {};
  if (!text) return res.status(400).json({ error: "text is required" });

  const prompt = `
Analyze sentiment of this text and return JSON only:
{"sentimentScore": -1..1, "label":"Positive|Neutral|Negative"}
Text: ${text}
`;

  const parsed = await aiJson(prompt, { sentimentScore: 0, label: "Neutral" });

  res.json({
    sentimentScore: Number(parsed.sentimentScore ?? 0),
    label: String(parsed.label ?? "Neutral"),
    fallback: parsed.fallback ?? false,
    provider: PROVIDER,
    correlationId: req.correlationId,
    error: parsed.error, 
  });
});

// POST /api/ai/tags { text }
app.post("/api/ai/tags", async (req, res) => {
  const { text } = req.body || {};
  if (!text) return res.status(400).json({ error: "text is required" });

  const prompt = `
Extract 3-8 short themes/tags. Return JSON only:
{"tags":["tag1","tag2",...]}
Rules: tags are 1-3 words each.
Text: ${text}
`;

  const parsed = await aiJson(prompt, { tags: [] });
  const tags = Array.isArray(parsed.tags) ? parsed.tags.map(String) : [];

  res.json({
    tags,
    fallback: parsed.fallback ?? false,
    provider: PROVIDER,
    correlationId: req.correlationId,
    error: parsed.error,
  });
});

// POST /api/ai/insights { firstName,lastName,email,notes }
app.post("/api/ai/insights", async (req, res) => {
  const { firstName, lastName, email, notes } = req.body || {};

  const prompt = `
Given the user profile, generate JSON only:
{
  "summary": "1-2 lines",
  "engagementLevel": "Low|Medium|High",
  "recommendedActions": ["action1","action2"]
}
User: ${JSON.stringify({ firstName, lastName, email, notes })}
`;

  const parsed = await aiJson(prompt, {
    summary: "",
    engagementLevel: "Medium",
    recommendedActions: [],
  });

  res.json({
    summary: String(parsed.summary ?? ""),
    engagementLevel: String(parsed.engagementLevel ?? "Medium"),
    recommendedActions: Array.isArray(parsed.recommendedActions)
      ? parsed.recommendedActions.map(String)
      : [],
    fallback: parsed.fallback ?? false,
    provider: PROVIDER,
    correlationId: req.correlationId,
    error: parsed.error,
  });
});

export default app;

if (process.env.NODE_ENV !== "test") {
  app.listen(port, () => {
    console.log(`AI service running on http://localhost:${port} (provider=${PROVIDER})`);
  });
}

