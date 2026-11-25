# FileDownloadManager contributor guide

Read the [coding standards](docs/CodingStandards.md) before writing code. Use the [implementation plan](docs/ImplementationPlan.md) to find the current milestone, its acceptance criteria, and the decisions already made.

- Work on the agreed milestone. Add projects when they first have a responsibility to implement.
- Keep domain and application code independent of Avalonia and operating-system APIs. Keep dependencies within the boundaries in the coding standards.
- Preserve saved downloads and queues when changing persistence or recovery behavior.
- Once the solution exists, run `dotnet build` and the applicable tests from the repository root. Exercise changes through the affected entry point. For platform-specific behavior, record the OS and desktop environment actually tested.
- Update the plan when a feature passes its acceptance checks, or when a product decision changes its scope. Identify any verification still pending.
