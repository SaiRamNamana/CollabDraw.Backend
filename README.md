# CollabDraw — Backend

ASP.NET Core + SignalR backend powering [CollabDraw](https://collab-draw-frontend-lake.vercel.app/), a real-time collaborative whiteboard with CRDT-based conflict-free sync.

**Live demo:** https://collab-draw-frontend-lake.vercel.app/
**Frontend repo:** [CollabDraw.Frontend](https://github.com/your-username/CollabDraw.Frontend) <!-- update with your actual link -->

## What this service does

- Hosts a SignalR hub that relays binary Yjs CRDT updates between all clients in a drawing room, over WebSockets
- Persists every update to Postgres, so rooms survive restarts and late joiners get full history replayed on join
- Tracks live presence (who's in each room) and broadcasts join/leave events
- Supports room reset, clearing persisted state for all connected clients

## Why CRDTs instead of just broadcasting events

Naively relaying raw draw events breaks under real conditions: reconnects, out-of-order delivery, concurrent edits. This service doesn't try to resolve conflicts itself — it's a dumb relay + persistence layer. Each client's Yjs document merges updates independently and always converges to the same final state regardless of arrival order, which is what makes the sync provably conflict-free rather than "usually fine."

## Tech stack

| Component | Technology |
|---|---|
| Framework | ASP.NET Core (.NET 10) |
| Real-time transport | SignalR (WebSockets) |
| ORM | Entity Framework Core |
| Database | PostgreSQL |
| Containerization | Docker |
| Hosting | Render |

## Architecture

```
Client A (Yjs doc) → SignalR hub → persist to Postgres → broadcast → Client B, C, ... (Yjs doc)
```

New client joins → hub replays all persisted updates for that room → client's local Yjs doc merges them into the correct current state, no matter the original order.

## API surface (SignalR hub methods)

| Method | Purpose |
|---|---|
| `JoinRoom(roomId, userName)` | Joins a room, replays persisted history, broadcasts updated presence |
| `SendUpdate(roomId, update)` | Persists and broadcasts a Yjs binary update to the room |
| `SendCursor(roomId, x, y, isDrawing)` | Broadcasts live cursor position for presence indicators |
| `ResetRoom(roomId)` | Clears persisted state and notifies all clients to reset their canvas |
| `LeaveRoom(roomId)` | Removes the connection from the room and updates presence |

## Running locally

```bash
dotnet restore
dotnet ef database update    # applies schema to your local Postgres
dotnet run
```

Requires a local Postgres instance — see `appsettings.json` for the expected connection string format, or run one via Docker:
```bash
docker run --name collabdraw-pg -e POSTGRES_PASSWORD=devpass -e POSTGRES_DB=collabdraw -p 5432:5432 -d postgres
```

## Deployment

Deployed on Render via the included `Dockerfile` — builds and runs as a container, with connection details supplied through environment variables (`ConnectionStrings__DefaultConnection`, `ASPNETCORE_ENVIRONMENT`).
