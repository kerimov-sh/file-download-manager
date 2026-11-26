# FileDownloadManager contributor guide

Read the [coding standards](docs/CodingStandards.md) before writing code. Use the [implementation plan](docs/ImplementationPlan.md) to find the current milestone, its acceptance criteria, and the decisions already made.

- Work on the agreed milestone. Add projects when they first have a responsibility to implement.
- Keep TransferModel and DownloadContracts independent of Avalonia and operating-system APIs. Follow the project boundaries in the coding standards.
- Preserve saved downloads and queues when changing persistence or recovery behavior.
- Once the solution exists, run `dotnet build` and the applicable tests from the repository root. Exercise changes through the affected entry point. For platform-specific behavior, record the OS and desktop environment actually tested.
- Update the plan when a feature passes its acceptance checks, or when a product decision changes its scope. Identify any verification still pending.

## Branch workflow

1. Start each feature, bug fix, hotfix, or other substantive change on its own descriptively named branch from an up-to-date `main`. Keep the UI, backend, and tests for the same feature together on that branch through review and merge. Simple documentation and configuration updates may share the active feature, bug-fix, or hotfix branch.
2. Commit the change on its branch. Run the build, applicable tests, and checks for the affected behavior. Resolve failures and repeat the checks until they pass.
3. Merge the verified change into `main`. If `main` has advanced, integrate it into the change branch and verify the combined result before merging.
4. Push the merged `main` and confirm it contains the change. Then delete the merged branch locally and, if it was published, on the remote.
