import { createConnection } from "./main.js";

const JWT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJjOGZhY2E3ZS05NWU2LTQ3MGUtYTRkYy0yODU4YWJjNDU2ZmUiLCJzdWIiOiIyIiwibmFtZSI6IkRyaXZlciIsImVtYWlsIjoiZHJpdmVyQGdtYWlsLmNvbSIsInBob25lX251bWJlciI6IjAxMjM0NTY3ODkxIiwic3RhdHVzIjoiQWN0aXZlIiwiZW1haWxfdmVyaWZpZWQiOiJGYWxzZSIsInBob25lX3ZlcmlmaWVkIjoiVHJ1ZSIsInJvbGVzIjpbIkRyaXZlciJdLCJkcml2ZXJfb25ib2FyZGluZ19zdGF0dXMiOiJBcHByb3ZlZCIsIm5iZiI6MTc3MDQxNDIwNywiZXhwIjoxODMwNDE0MjA3LCJpYXQiOjE3NzA0MTQyMDd9.AgGn3qDTFOn1uHyxYFpsZxl-jaBBeQpzwxYxYA1Sxq8";
const HUB_URL = "http://localhost:5000/hubs/driver";

const connection = createConnection(HUB_URL, JWT_TOKEN);

// ---- handlers ----
connection.on("NotifyDriver", (driverId, message) => {
  console.log("📢 Driver notification");
  console.log({ driverId, message });
});

connection.on("NotifyRideRequestOffer", (driverId, offerId) => {
  console.log("🆕 Ride offer");
  console.log({ driverId, offerId });
});

connection.on("NotifyRideRequestOfferExpired", (driverId, offerId) => {
  console.log("⏰ Offer expired");
  console.log({ driverId, offerId });
});

// ---- start ----
async function start() {
  try {
    await connection.start();
    console.log("✅ Connected to Driver hub");
  } catch (err) {
    console.error("❌ Connection failed", err);
    setTimeout(start, 3000);
  }
}

start();
