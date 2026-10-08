# Benha Coordinates & Sample Trip Requests

Sample coordinate pairs for testing and demoing trip dispatch in Benha, Egypt.

---

### Route 1: Benha Train Station → Benha University
```json
{
  "pickupLatitude": 30.46629,
  "pickupLongitude": 31.18463,
  "dropoffLatitude": 30.46983,
  "dropoffLongitude": 31.17891,
  "pickupAddress": "Benha Train Station, Qalyubia",
  "dropoffAddress": "Faculty of Engineering, Benha University",
  "estimatedDistanceKm": 0.8
}
```

---

### Route 2: Benha University Hospital → Al-Shohada Square
```json
{
  "pickupLatitude": 30.46255,
  "pickupLongitude": 31.18742,
  "dropoffLatitude": 30.46500,
  "dropoffLongitude": 31.18200,
  "pickupAddress": "Benha University Hospital, Farid Nada St",
  "dropoffAddress": "Al-Shohada Square, Benha",
  "estimatedDistanceKm": 0.6
}
```

---

### Route 3: Kafr El-Gazzar → Al-Vilal District
```json
{
  "pickupLatitude": 30.47200,
  "pickupLongitude": 31.17400,
  "dropoffLatitude": 30.46800,
  "dropoffLongitude": 31.19200,
  "pickupAddress": "Kafr El-Gazzar Entrance, Benha",
  "dropoffAddress": "Al-Vilal District, Corniche El-Nil",
  "estimatedDistanceKm": 2.1
}
```

---

### Route 4: Out-of-Service Area (Negative Geofence Test)
```json
{
  "pickupLatitude": 30.61000,
  "pickupLongitude": 31.35000,
  "dropoffLatitude": 30.46629,
  "dropoffLongitude": 31.18463,
  "pickupAddress": "Outside Service Area",
  "dropoffAddress": "Benha Train Station",
  "expectedOutcome": "Validation Error (Outside Service Area)"
}
```