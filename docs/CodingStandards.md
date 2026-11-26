# Coding standards

## Solution and dependencies

Use `FileDownloadManager` for the solution and `FileDownloadManager.[ProjectName]` for every project. Match namespaces to project and folder names. Target .NET 10 LTS and use a supported servicing patch.

Use .NET and Microsoft packages for application code. Avalonia and its required dependencies are approved for the desktop interface. Browser extensions may use plain JavaScript, browser APIs, and manifests. Review additional third-party libraries or external runtime tools with the owner before adoption.

## Architecture

| Layer | Owns | May reference |
|---|---|---|
| TransferModel | Download requests, states, and rules for ranges and completion | .NET base types |
| DownloadContracts | Operations that callers can request from the engine | TransferModel |
| Engine | Transfer execution, protocol handling, scheduling, partial files, and checkpoints | DownloadContracts and TransferModel |
| UI (planned) | Avalonia windows, localization, and desktop lifetime | Uses DownloadContracts and TransferModel. Startup wires in Engine. |

Name each library for its role in the download manager. Keep its folder, project filename, and root namespace aligned.

Host the engine and scheduler in the desktop process. Browser and command-line entry points should submit operations to the running application through a local boundary, with a single owner of queue and file state.

Apply SOLID to concrete responsibilities. For example, changing a toolbar label should affect presentation resources, and changing a checkpoint format should affect persistence. Inject narrow contracts for boundaries such as clocks, storage, and transfer I/O when a use case needs them. Apply YAGNI by introducing abstractions, projects, and settings alongside their first actual use.

## Types and names

- Put every authored C# type in its own matching file, including classes, records, structs, interfaces, enums, and delegates. Keep members with their owning type. Use the framework convention for an Avalonia view and its generated partial class.
- Use PascalCase for types, methods, properties, events, enum members, and constants. Prefix interface names with `I`. Use camelCase for parameters and local variables, and underscore-prefixed camelCase for private instance fields.
- Give asynchronous methods the `Async` suffix when the framework contract permits it.
- Choose names that identify units and scope, such as `MaximumParallelSegmentsPerFile`. Use `TimeSpan` for durations and a sufficiently wide integer type for byte positions.

## Comments

- For complex logic, comment on the reason for the implementation choice, the invariant it preserves, or the failure it prevents. Place the comment beside the relevant code and update it when the reasoning changes. Omit comments that merely narrate an assignment, condition, or method call.

## Values and resources

- Use framework enums and typed APIs for protocol values. Give meaningful fixed literals named constants near the component that owns them. Put configurable limits and defaults in named, validated settings.
- Keep persisted identifiers and message keys stable across releases. Display names and translated text may change independently.
- Store desktop text under `Resources/Localization/` in .resx files. English is the first and fallback language. Use typed resource access where available. Localize complete messages and format dates, numbers, and quantities using the selected culture.
- Keep icons, images, and Avalonia theme definitions under separate `Resources/Icons/`, `Resources/Images/`, and `Resources/Themes/` folders. Keep browser extension messages in the extension's own `_locales/` directory.

## Cooperative cancellation

- Give each cancellable operation a final parameter named `CancellationToken cancellationToken`. Require it on internal APIs so callers must supply the token. Forward it through the download contracts and engine, including HTTP requests, stream reads and writes, retry delays, and scheduler waits.
- The desktop host owns the application lifetime token. Each running transfer owns a `CancellationTokenSource` linked to that lifetime and passes its token to every segment. Check `ThrowIfCancellationRequested()` in long-running loops that have no cancellable await. Await the segment tasks before disposing their source or closing shared files.
- Record the reason for stopping at the application boundary. Pause preserves resumable data, a user cancellation ends the job, and Exit checkpoints eligible jobs for recovery. Treat an `OperationCanceledException` caused by the requested token as an expected stop and skip retries. Handle network timeouts separately.
- Finish any required checkpoint using a separate, bounded cleanup token so an already-cancelled transfer token cannot interrupt the save. A checkpoint must describe only durable bytes.

## Transfer correctness

Persist enough information to recover a transfer after interruption. A checkpoint may claim only byte ranges safely written to the partial file. Validate remote identity and range responses before combining data. Publish the completed file only after all expected bytes are accounted for. Preserve manually paused jobs through a restart.

Use deterministic tests for state transitions, scheduling, and range arithmetic. Exercise transport behavior against a controlled HTTP server and recovery against real temporary files. Include a regression test when a change fixes a failure that existing checks would miss.
