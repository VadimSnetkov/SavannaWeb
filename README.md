# Savanna Web

A React + TypeScript frontend for the existing C# Savanna simulation. This first web increment runs locally without Docker or a database.

## Run on Windows

Prerequisites: .NET SDK 10 and Node.js 24 with npm. Check with `dotnet --version` and `node --version`.

Open `SavannaWeb.sln` in Visual Studio. Open two terminals in the repository folder.

**Terminal 1 — simulation backend:**

```powershell
dotnet run --project Savanna.Simulation.Api
```

Keep it running. The backend listens at `http://localhost:5050`.

**Terminal 2 — frontend:**

```powershell
cd Savanna.Web
npm install
npm run dev
```

Open **http://localhost:5173**. Both processes must be running. If PowerShell blocks `npm.ps1`, use `npm.cmd install` and `npm.cmd run dev` instead.

Click **Create game**, add animals, then click **Play**. Games start paused so you can populate the field first.

## Features

- Add antelopes, lions and the Zebra plugin with buttons or **A**, **L**, **Z**.
- Server-side movement, hunting, health, death and reproduction reuse the console engine.
- Play/pause, advance one round, or discard the game and start another.
- Toggle icons and configured letters. Unknown plugin species fall back to their letter.
- Select and highlight an animal to inspect species, age in rounds, health, offspring, speed, vision and position.
- Each browser tab creates its own independent game.
- Standard browser warning when leaving a page with progress. Browsers control its wording and may suppress it in some circumstances; it is not a saving mechanism.
- Original console frontend remains available.

Space toggles play/pause unless a button has focus, where Space activates that button normally. Species shortcuts use the keys configured by the backend.

## Structure

| Location | Responsibility |
| --- | --- |
| `Savanna.Core/` | Shared models, movement, health, birth rules and plugin loader |
| `Savanna.Console/` | Existing letter-based console frontend |
| `Savanna.Animals.Zebra/` | Animal behavior compiled as a separate DLL |
| `Savanna.Simulation.Api/Controllers/` | HTTP game endpoints |
| `Savanna.Simulation.Api/Service/` | Per-game engine scopes, commands and server clock |
| `Savanna.Simulation.Api/Models/` | Request and response data |
| `Savanna.Simulation.Api/Validator/` | FluentValidation request and configuration rules |
| `Savanna.Simulation.Api/Extensions/` | API/DI registrations and exception middleware registration |
| `Savanna.Simulation.Api/Middleware/` | Unexpected HTTP failure logging and safe error responses |
| `Savanna.Web/src/UI/` | React field, animal symbols and statistics panel |
| `Savanna.Web/src/hooks/` | Commands, polling and unsaved-progress warning |
| `Savanna.Web/src/service/` | HTTP client |
| `Savanna.Tests/` | Existing engine tests plus web-session tests |

This is the simulation service from the planned microservice architecture. Authentication and saved-game services are future increments; this version does not contain empty service projects.

## How it works

The browser sends commands to the .NET API. A background service advances running games at the configured interval; React polls for snapshots at that same interval. The browser never calculates animal movement. SignalR can replace polling later if needed.

Each game owns a dependency-injection scope with separate simulation, birth counters, catalog and behavior instances. A registry lock serializes commands and timer updates. Responses contain copied data. This first version runs in one backend process; distributed sessions and multi-instance scaling are not implemented.

Games exist only in server memory. Their IDs are held only in the current page, so refreshing or closing the page does not restore progress. New game deletes the old session. Abandoned sessions expire after the configured idle timeout, and stopping the backend removes all sessions. **There is no save/load or login yet.** This is a local development build, not a deployment with access control.

Core additions are small: stable animal IDs, a birth round and an offspring count. Spawning records the current round; successful births increment both parents' counts. Age is the current round minus the birth round. Movement and health rules are unchanged.

The console baseline is `VadimSnetkov/Savanna`, commit `43e5e17` (`feature/iteration-3`), including the Zebra DLL loader. This project does not modify the original console repository.

## Configuration

Swagger is available at **http://localhost:5050/swagger** when the backend runs in Development. Use **Try it out** to create a game, copy its returned ID, and exercise the game endpoints.

Controllers return 404 for a missing game, 409 for a conflicting command, and 422 for invalid input. Malformed JSON remains a 400 binding error. Unexpected errors are logged through Serilog and return a safe 500 response with a trace ID. Serilog's level and console output are configured in `appsettings.json`.

Plugin errors now propagate out of the shared Core library. For HTTP commands, the API stops the affected game and the middleware logs the exception. For automatic rounds, the game manager stops and logs only that game; other games keep running. Create a new game after such a failure. The console handles plugin errors at its own UI boundary.

Edit `Savanna.Simulation.Api/appsettings.json` for field dimensions, round delay, animal settings, plugins, idle timeout and maximum game count. Restart the backend after changes.

The console keeps its separate `Savanna.Console/appsettings.json`.

The local API URL is in `Savanna.Simulation.Api/Properties/launchSettings.json`. If you change port 5050, also change the proxy target in `Savanna.Web/vite.config.ts`. The frontend calls `/api` through Vite, so no separate CORS setup is needed for development.

Zebra builds automatically and is copied to the API's `bin/Debug/net10.0/Plugins/Zebra/` folder (or equivalent Release folder). Publishing also copies the plugin. Additional trusted plugins need matching `Simulation:AdditionalAnimals` settings and a compatible DLL in the plugin folder. Restart the API to discover them.

## Verify

From the repository root:

```powershell
dotnet build SavannaWeb.sln
dotnet test SavannaWeb.sln
dotnet run --project Savanna.Console
```

Press Escape to leave the console.

Frontend compilation:

```powershell
cd Savanna.Web
npm run build
```

Browser tests (keep the backend running in another terminal):

```powershell
npx playwright install chromium
npm run test:e2e
```

The test runner starts the frontend if needed. Tests cover controls, statistics, Zebra, display toggling, shortcuts, separate tabs, reset confirmation, reload warning, connection failure, API validation and mobile overflow. `PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH` optionally selects an installed Chromium executable.

See `REVIEW-FIXES.md` for every mentor comment, its solution and the checks completed for this revision.

## Continue on your personal laptop

Commit the source, then push the feature branch. `package-lock.json` stays local and is ignored, following the mentor's repository convention. Clone the repository on your personal laptop, switch to that feature branch, and repeat the startup commands. Later updates use `git pull`.

`bin`, `obj`, `node_modules` and build outputs are ignored and recreated locally. There are no database contents to transfer yet. Keep work on a feature branch and merge only after mentor approval.
