# ✈️ Automated Air Traffic Control (ATC) Simulation & Testing Framework

> A safe, sprint-realistic simulation platform for testing automated ATC conflict-resolution algorithms — without risking real aircraft.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?logo=dotnet)](https://dotnet.microsoft.com/apps/aspnet)
[![EF Core](https://img.shields.io/badge/EF_Core-Database--First-68217A?logo=dotnet)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-Database-CC2927?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![xUnit](https://img.shields.io/badge/xUnit-Testing-2C4F76)](https://xunit.net/)
[![Status](https://img.shields.io/badge/Status-In_Development-yellow)]()

---

## 📖 Overview

Testing automated, algorithm-driven ATC vectoring in live airspace is **dangerous and non-viable**. Existing flight simulators are typically disconnected from the automated resolution engines researchers actually want to validate.

This project builds a **two-layer software framework** — a Simulation Engine and a Control Layer — that:

- Ingests simulated flight telemetry (ADS-B-format JSON)
- Calculates real-time flight kinematics on a fixed tick
- Detects horizontal/vertical separation conflicts between aircraft
- Dispatches conflicts to an automated controller agent and tests its tactical vectoring decisions
- Visualizes live aircraft data on a radar/status view

> 📝 **Note:** This implementation is intentionally scoped for a small team delivering under a compressed timeline. See [Scope Realignment](#-scope-realignment) below for why the numbers differ from the original spec — every requirement is preserved, just resized to be honestly buildable.

---

## 👥 Team — Group 26

| Roll No. | Name |
|---|---|
| IN26011871 | Shriyam Rastogi |
| IN26011732 | Harsh Kumar Nimesh |
| IN26012122 | Arnav Majithia |
| IN26009582 | Harsh Raj Singh |
| IN26009579 | Aman Kumar Singh |
| IN26009732 | Aditya Atreya |
| IN26011664 | Aryaman Singh |

---

## 🧱 Tech Stack

| Layer | Technology |
|---|---|
| **Backend** | ASP.NET Core Web API (.NET 8) |
| **Database** | SQL Server (LocalDB / Express) |
| **ORM** | EF Core — **Database-First** (`Scaffold-DbContext`) |
| **Frontend** | Lightweight HTML/JS, polling-based updates |
| **Background processing** | `IHostedService` / `BackgroundService` tick loop |
| **Testing** | xUnit (conflict-detection logic) |
| **Distance calculation** | Haversine formula (spherical geodesy) |
| **API docs / testing** | Swagger (OpenAPI, auto-generated) |

---

## 🎯 Scope Realignment

The original specification was written at aviation-industry production scale. We've scaled the **constants**, not the **ambition** — every functional requirement, user story, and acceptance criterion is preserved.

| Original Spec | Sprint-Realistic Build | Why |
|---|---|---|
| 500 simultaneous aircraft, <100ms tick | 15–30 aircraft, ~1s tick | Still demonstrably real-time; achievable without dedicated load-testing infra |
| WGS84 ellipsoidal precision to 0.01 NM | Haversine (spherical) distance | Sufficient precision for a simulation environment |
| Live ADS-B telemetry ingestion | Simulated JSON generator (identical schema) | No API keys/rate limits needed; a real feed can be swapped in later with zero changes downstream |
| External "Automated Controller Agent" service | Internal C# service via `IControllerAgent` | Swappable for a real external agent later without touching the rest of the system |
| Real-time WebSocket push | Polling-based REST updates | Simpler to build and debug reliably within the timeline |
| 99.9% uptime, multi-hour runs | Stable through demo-length runs (10–20 min) | No infra to prove production SLAs in a course sprint |

---

## 🏗️ System Architecture

```
┌─────────────────────────────────────────┐
│         PRESENTATION LAYER              │
│  Polling-based frontend (HTML/JS)       │
│  (Status view, aircraft table, controls)│
└───────────────────┬─────────────────────┘
                    │ REST API (poll)
┌───────────────────▼─────────────────────┐
│     APPLICATION LAYER (ASP.NET Core)    │
│                                         │
│  Simulation TickService ──▶ ConflictDetector
│  (BackgroundService)       (Haversine calc)
│              │                          │
│              ▼                          │
│    AutomatedControllerAgent             │
│    implements IControllerAgent          │
│    rule: conflict → turn 30° / ±1000ft  │
└───────────────────┬─────────────────────┘
                    │ EF Core (Database-First)
┌───────────────────▼─────────────────────┐
│             SQL SERVER                  │
│  SimulationRun · Aircraft · PositionLog │
│  ConflictEvent · VectorCommand · AppUser│
└─────────────────────────────────────────┘
```


### Data flow (per tick)

1. Telemetry generator advances aircraft state → kinematics applied
2. `ConflictDetector` runs Haversine checks across all active aircraft pairs
3. On conflict → payload dispatched to `AutomatedControllerAgent` → returns a vector command
4. Command applied to the aircraft's trajectory for the next tick
5. Frontend polls the API and refreshes the aircraft/conflict view
6. EF Core persists position logs, conflict events, and commands to SQL Server

---

## ✅ Functional Requirements

<details>
<summary><strong>A. Ingestion & Kinematics Layer</strong></summary>

- Ingest simulated JSON flight-state vectors (ICAO24, callsign, lat/lon, altitude, heading, speed)
- Update coordinates and altitude per aircraft using kinematic formulas each tick

</details>

<details>
<summary><strong>B. Conflict Detection & Management</strong></summary>

- Continuously calculate horizontal (NM) and vertical (ft) separation between all active aircraft pairs
- Trigger a **Loss of Separation Alert** when < 5 NM horizontal **and** < 1,000 ft vertical

</details>

<details>
<summary><strong>C. Automated Vectoring & Resolution</strong></summary>

- Dispatch real-time aircraft state payloads to the Automated Controller Agent
- Execute tactical commands (heading ±30°, climb/descend 1,000 ft)

</details>

<details>
<summary><strong>D. Analytics & Supervision</strong></summary>

- Render an aircraft status view with conflict highlighting
- Record all telemetry, conflict events, and reaction latencies to SQL Server

</details>

---

## 🗄️ Database Schema (EF Core Database-First)

Schema is authored directly in SQL Server, then scaffolded into C# models via `Scaffold-DbContext` — **no code-first migrations**.

| Table | Purpose |
|---|---|
| `SimulationRun` | Tracks each simulation session (start/end time, status) |
| `Aircraft` | Aircraft participating in a run (callsign, ICAO24) |
| `PositionLog` | Per-tick position history (lat, lon, altitude, heading, speed) |
| `ConflictEvent` | Detected separation violations and their resolution status |
| `VectorCommand` | Commands issued by the Controller Agent per conflict |
| `AppUser` | Authenticated users (Supervisor / Engineer / Analyst roles) |

```bash
Scaffold-DbContext "Server=.;Database=ATC_DB;Trusted_Connection=True;TrustServerCertificate=True;" `
  Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models
```

---

## 📡 API Layer

REST endpoints exposed via ASP.NET Core Web API controllers, documented and testable live via **Swagger** (`/swagger`):

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/simulationruns` | Start a new simulation run |
| `PATCH` | `/api/simulationruns/{id}/stop` | Stop an active run |
| `GET` | `/api/simulationruns/{id}/aircraft` | Current aircraft positions (polled by frontend) |
| `GET` | `/api/simulationruns/{id}/conflicts` | Fetch conflict history for a run |
| `GET` | `/api/simulationruns/{id}/commands` | Vector commands issued for a run |

---

## 🧩 Project Structure

```
AtcSimulationFramework/
├── Controllers/       REST API endpoints
├── Models/            EF Core scaffolded entities
├── Services/          Tick loop, conflict detector, controller agent
├── Data/              DbContext
├── wwwroot/           Frontend (HTML/JS)
└── Program.cs         Dependency injection & app configuration
```

---

## 🚀 Getting Started

1. Clone the repo and restore dependencies (`dotnet restore`)
2. Ensure the SQL Server schema in `ATC_Database_Schema.sql` (repo root) has been applied
3. Update the connection string in `appsettings.json`
4. Run `Scaffold-DbContext` if `Models/` isn't already populated
5. `dotnet run` — Swagger UI available at `/swagger`

---

## 📜 License

No License
