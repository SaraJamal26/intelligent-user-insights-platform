import request from "supertest";
import app from "../server.js";

describe("AI Service API", () => {
  test("GET /health should return ok", async () => {
    const res = await request(app).get("/health");
    expect(res.statusCode).toBe(200);
    expect(res.body.status).toBe("ok");
  });

  test("POST /api/ai/tags should return tags array", async () => {
    const res = await request(app)
      .post("/api/ai/tags")
      .send({ text: "The app is fast but crashes sometimes." });

    expect(res.statusCode).toBe(200);
    expect(Array.isArray(res.body.tags)).toBe(true);
  });

  test("POST /api/ai/sentiment should return sentimentScore and label", async () => {
    const res = await request(app)
      .post("/api/ai/sentiment")
      .send({ text: "I love it." });

    expect(res.statusCode).toBe(200);
    expect(typeof res.body.sentimentScore).toBe("number");
    expect(typeof res.body.label).toBe("string");
  });
});
