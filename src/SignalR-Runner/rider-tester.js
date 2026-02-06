import { createConnection } from "./main.js";

const JWT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiI5NDAxOTQ3Mi01YzE5LTRjNGQtYjNkYS00MTVlZjZjNTlmNDAiLCJzdWIiOiIzIiwibmFtZSI6IlJpZGVyIiwiZW1haWwiOiJyaWRlckBnbWFpbC5jb20iLCJwaG9uZV9udW1iZXIiOiIwMTIzNDU2Nzg5MyIsInN0YXR1cyI6IkFjdGl2ZSIsImVtYWlsX3ZlcmlmaWVkIjoiRmFsc2UiLCJwaG9uZV92ZXJpZmllZCI6IlRydWUiLCJyb2xlcyI6WyJSaWRlciJdLCJuYmYiOjE3NzA0MTQyMjIsImV4cCI6MTgzMDQxNDIyMiwiaWF0IjoxNzcwNDE0MjIyfQ.PpJlJquAYbQBC7Ks6nVViAXUcyP8lSLEl71OHCQgFjY";
const HUB_URL = "http://localhost:5000/hubs/rider";

const connection = createConnection(HUB_URL, JWT_TOKEN);

// ---- handlers ----
connection.on("NotifyTripAssigned", (riderId, notification) => {
  console.log("📦 Trip assigned");
  console.log({ riderId, notification });
});

connection.on("NotifyDriverArrived", (riderId, notification) => {
  console.log("🚗 Driver arrived");
  console.log({ riderId, notification });
});

connection.on("NotifyTripStarted", (riderId, notification) => {
  console.log("▶️ Trip started");
  console.log({ riderId, notification });
});

connection.on("NotifyTripCompleted", (riderId, notification) => {
  console.log("🏁 Trip completed");
  console.log({ riderId, notification });
});

connection.on("NotifyDriverLocationUpdate", (riderId, location) => {
  console.log("📍 Location update");
  console.log({ riderId, location });
});

// ---- start ----
async function start() {
  try {
    await connection.start();
    console.log("✅ Connected to Rider hub");
  } catch (err) {
    console.error("❌ Connection failed", err);
    setTimeout(start, 3000);
  }
}

start();
