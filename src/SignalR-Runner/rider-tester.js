import { createConnection } from "./main.js";

const JWT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJkNjZjNTI3Ny04MzI0LTQ1MTYtYTZkNy04MTQ0MWJmOTg2NDkiLCJzdWIiOiIzIiwibmFtZSI6IlJpZGVyIiwiZW1haWwiOiJyaWRlckBnbWFpbC5jb20iLCJwaG9uZV9udW1iZXIiOiIrMjAxMjM0NTY3ODkzIiwiZW1haWxfdmVyaWZpZWQiOiJmYWxzZSIsInBob25lX3ZlcmlmaWVkIjoidHJ1ZSIsImFwcCI6InJpZGVyLWFwcCIsIm5iZiI6MTc3MzE1NDQzMCwiZXhwIjoxODMzMTU0NDMwLCJpYXQiOjE3NzMxNTQ0MzB9.kTj2imkccj6ewZ2ip-I1uDTVLipzwOOGaSUjhG8KaEY";
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
