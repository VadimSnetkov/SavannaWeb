# PR #1 — mentor review fixes

Reviewed the 12 inline comments posted by `sreeharihm` on 26 November 2026. The review summary, “Added review comments”, does not request another code change. These fixes build on PR commit `d46573e` and keep the same feature branch; they do not merge the PR.

## Each comment and its solution

| Comment | Meaning | What changed |
| --- | --- | --- |
| [1. Use normal API results, exception middleware and Serilog](https://github.com/VadimSnetkov/SavannaWeb/pull/1#discussion_r4706719492) | A controller should describe the response; shared middleware should handle unexpected failures. | Removed the controller's exception-catching `Respond` delegate. Added `ExceptionHandlingMiddleware`, Serilog configuration and HTTP request logging. Controllers return `Ok`, `NotFound`, `Conflict` and `UnprocessableEntity`. |
| [2. 404 and 409 are not exceptions](https://github.com/VadimSnetkov/SavannaWeb/pull/1#discussion_r4706752140) | Missing games, full fields, session limits and stepping while running are expected outcomes. | Added `GameResult` and `GameResultStatus`. The game manager returns ordinary results for these cases. Actual unexpected exceptions are not converted to 409; the middleware returns 500. The frontend preserves a game after a conflict and clears an expired game after 404. |
| [3. Put registration helpers in Extensions](https://github.com/VadimSnetkov/SavannaWeb/pull/1#discussion_r4706786999) | Folder names should match a file's responsibility. | Moved `ServiceCollectionExtensions.cs` from `Service` to `Savanna.Simulation.Api/Extensions` and updated its namespace and imports. |
| [4. Use the validation result and return 422](https://github.com/VadimSnetkov/SavannaWeb/pull/1#discussion_r4707042844) | The JSON can be readable but contain invalid values. | Converted FluentValidation's errors to `ValidationProblemDetails` and returned `UnprocessableEntity`. Empty, unknown, missing or null species receive 422. Broken JSON still receives 400. |
| [5. Log worker cancellation at Information](https://github.com/VadimSnetkov/SavannaWeb/pull/1#discussion_r4707124928) | Normal shutdown should be visible without being recorded as an error. | Injected `ILogger<SimulationWorker>` and log once from `StopAsync`, including shutdown before the first tick. `LogInformation` is forwarded to Serilog by the registered provider. |
| [6. Missing extension method in Program](https://github.com/VadimSnetkov/SavannaWeb/pull/1#discussion_r4707162213) | The earlier `UseExceptionHandler` call was a framework extension; there was no project-specific handler to inspect. | Added `ApplicationBuilderExtensions.UseSavannaExceptionHandling`, which installs our custom middleware. `Program` now calls this explicit extension and the API registration extension. |
| [7. Add Swagger/API documentation](https://github.com/VadimSnetkov/SavannaWeb/pull/1#discussion_r4707170539) | Endpoints should be easy to inspect and exercise without the frontend. | Added Swashbuckle, Swagger UI at `/swagger` in Development, and documented response types/codes on controller actions. |
| [8. Derive Zebra Type from the class name](https://github.com/VadimSnetkov/SavannaWeb/pull/1#discussion_r4707208781) | Practise extension methods using runtime type information. | Added `AnimalBehaviorExtensions.GetSpeciesName()`. Zebra uses `this.GetSpeciesName()`; the extension turns `ZebraBehavior` into `Zebra` by removing only the trailing `Behavior` suffix. Renaming this plugin class would also require matching configuration changes. |
| [9. Let AnimalCatalog exceptions reach middleware](https://github.com/VadimSnetkov/SavannaWeb/pull/1#discussion_r4707233814) | A failed plugin should not silently become a successful turn. | Replaced `LastBehaviorError` and the stay-in-place fallback with `AnimalBehaviorException`, preserving the original inner exception. HTTP failures reach the middleware and are logged there. Background rounds have no HTTP middleware, so their failures are logged by the manager and isolated to the affected game. The console catches them at its own UI boundary. |
| [10. Reduce simulation coupling through interfaces](https://github.com/VadimSnetkov/SavannaWeb/pull/1#discussion_r4707273015) | The engine should depend on replaceable service contracts. | Extracted the existing movement algorithm to `AnimalMovementService`. `SimulationService` now receives `IAnimalMovementService`, `IHealthService` and `IBirthService`. Built-in behaviors receive `IMovementService`. Registered implementations in both API and console DI. Movement rules are unchanged. |
| [11. Show messages based on HTTP status](https://github.com/VadimSnetkov/SavannaWeb/pull/1#discussion_r4707374129) | An expired game, invalid input, conflict and unavailable server need different feedback. | Added named `HttpStatus` values and `apiErrorMessage`. 422 displays validation messages; 409 explains the conflict; 404 offers a new game; 5xx errors use safe retry messages. Responses without JSON are handled too. |
| [12. Do not track the npm lock file](https://github.com/VadimSnetkov/SavannaWeb/pull/1#discussion_r4707395503) | Apply the mentor's repository convention. | Added `package-lock.json` to `.gitignore` and removed it from Git tracking. npm can still generate a local copy. Updated setup to `npm install`, because `npm ci` requires an existing lock file. |

The interface refactor most directly applies Dependency Inversion: the engine depends on contracts rather than concrete implementations. The contracts are small, and a test replaces movement, health and birth implementations through DI to verify the engine works with substitutes.

The lock-file choice follows this review's convention. Exact direct dependency versions remain in `package.json`, but without a committed lock file, transitive versions are not frozen across fresh installations.

## Verification on 27 November 2026

- .NET solution builds with zero warnings/errors, including the console.
- **59 xUnit cases passed**: existing simulation coverage plus real HTTP tests for 404/409/422/400, safe 500 responses, original exception logging, background game isolation, worker shutdown, Swagger and interface replacement.
- React/TypeScript production build passed.
- **14 frontend error-handling tests passed** without a browser.
- **1 live API test passed** through the Vite proxy.
- Six browser UI scenarios could not start: the environment's Chromium launcher points to a missing Chromium binary. They are retained for local execution; no claim is made that this browser suite passed.

The faulty-plugin regression cases now expect propagation to the application boundary. They still verify that forbidden positions and health changes are not applied to the failing animal. This is the intentional error-handling change requested in comment 9.

## Run after pulling

From the repository root:

```powershell
dotnet test SavannaWeb.sln
dotnet run --project Savanna.Simulation.Api
```

In another terminal:

```powershell
cd Savanna.Web
npm install
npm run dev
```

Open `http://localhost:5173` for the game or `http://localhost:5050/swagger` for API testing. The frontend's focused error tests can be run with `npm run test:e2e -- api-errors.spec.ts`. Keep the backend running for the full browser/API suite: `npm run test:e2e`.
