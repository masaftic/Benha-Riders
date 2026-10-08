import { createConnection } from "./main.js";

const JWT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiIwNWZkZDNhOC05YTdhLTRlYzItYmY3MC1mYTRkNDE0MGVhYmYiLCJzdWIiOiIyIiwibmFtZSI6IkRyaXZlciIsImVtYWlsIjoiZHJpdmVyQGdtYWlsLmNvbSIsInBob25lX251bWJlciI6IisyMDEyMzQ1Njc4OTEiLCJlbWFpbF92ZXJpZmllZCI6ImZhbHNlIiwicGhvbmVfdmVyaWZpZWQiOiJ0cnVlIiwiYXBwIjoiZHJpdmVyLWFwcCIsImRyaXZlcl9vbmJvYXJkaW5nX3N0YXR1cyI6IkFwcHJvdmVkIiwibmJmIjoxNzczMDEyMTc0LCJleHAiOjE4MzMwMTIxNzQsImlhdCI6MTc3MzAxMjE3NH0.PGYBMMNgMjE7KZ9LuCBIkZWGILTRyYzu6-JAIMl0Q1w";
const HUB_URL = "http://localhost:5000/hubs/driver";

const connection = createConnection(HUB_URL, JWT_TOKEN);

// ---- handlers ----

connection.on("NotifyRideRequestOffer", (driverId, notification) => {
  console.log("🆕 Ride offer");
  console.log({ driverId, notification });
});

connection.on("NotifyRideRequestOfferExpired", (driverId, notification) => {
  console.log("⏰ Offer expired");
  console.log({ driverId, notification });
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
