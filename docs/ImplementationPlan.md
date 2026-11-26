# FileDownloadManager implementation plan

## Scope and decisions

The goal is functional parity with Internet Download Manager on Windows and Linux. This roadmap covers the documented workflows. Individual browser versions, media formats, legacy protocols, and platform integrations need explicit compatibility entries as their milestones begin.

| Topic | Decision |
|---|---|
| Runtime and UI | .NET 10 LTS, shared Avalonia desktop UI. Plain JavaScript is approved for browser extensions. |
| Architecture | TransferModel, DownloadContracts, Engine, and the Avalonia UI. Add CommandLine and BrowserHost projects when their milestones need them. Use the solution prefix for every project. |
| Application lifetime | Closing the main window leaves the program running. Reopening reconnects to the current transfers. Explicit Exit saves progress and ends transfers and scheduling. User launch starts the program. |
| Languages | English first and as fallback. Keep translations, themes, icons, and images separate. |
| Linux validation | Ubuntu LTS is a proposed first test distribution. Record the release, display session, and desktop environment for each desktop integration test. |
| Concurrency | Proposed initial ceiling of eight parallel segments per file, with a shared per-host limit. Evaluate advanced ceilings of sixteen and thirty-two using measured throughput and error rates. HTTP/2 can carry multiple segments over one connection. |

## Milestones and acceptance checks

The first review increment adds the TransferModel, DownloadContracts, and Engine projects to the [solution](../FileDownloadManager.slnx). The `IFileDownloader.DownloadAsync` entry point accepts an HTTP(S) source, a destination in an existing directory, and a cancellation token. The single-stream implementation publishes completed files without overwriting an existing destination.

Run `dotnet build` from the repository root, then `dotnet run --no-build --project tests/FileDownloadManager.TransferChecks`. The Windows integration run exercised a local HTTP server, redirects, unknown response length, byte equality, destination collisions, missing resources, unsolicited partial responses, truncation, and cancellation. Linux execution and TLS-specific tests are pending. Pause/resume, automatic retries, and recovery after process interruption remain later increments.

Launch the Avalonia download library with `dotnet run --project src/FileDownloadManager.UI`. Choose **Add URL**, enter an HTTP(S) URL and destination, and start the transfer. The table shows filenames, completed file sizes, statuses, and source hosts for the current session. The sidebar filters all, downloading, finished, and unfinished entries. **Stop** cancels the selected active download. **Remove** clears a selected inactive entry and preserves its file. Closing the window on Windows or macOS hides it to the notification area. Use the tray menu to show downloads or exit. On Linux the window also remains available in the taskbar because tray support varies by desktop. **Tasks → Exit** ends the process.

Windows desktop automation verified downloading, cancellation, category counts, completed size, removal, and saved bytes. The desktop currently supports one active transfer. Linux desktop testing, persistent history, and scheduling are pending. Windows uses the installed Segoe Fluent Icons on Windows 11 or Segoe MDL2 Assets on Windows 10. The View menu switches icon styles for the current session. Windows 11 build 26300 verification covered switching icons, closing to the tray, restoring the window from Explorer, and explicit Exit. Windows 10, Linux, and macOS appearance and tray verification are pending. The cross-platform fallback uses [Bootstrap Icons v1.11.3](https://github.com/twbs/icons/tree/v1.11.3) with the [MIT license](../src/FileDownloadManager.UI/Resources/Icons/LICENSE-Bootstrap-Icons.txt) included in build and publish output.

The main window uses a compact icon toolbar, an expandable category tree, and a transfer grid. The tree filters by transfer state and filename extension. The grid displays the last attempt, description, live percentage when size is known, average transfer rate, and estimated time left when available. **Retry** starts a fresh attempt. **Clear finished** removes completed entries from the session list and preserves their files. Scheduler and queue controls are planned with their respective features.

Windows desktop automation verified category selection, live percentage on a local transfer, Stop, Retry, Clear finished, and saved file contents. Linux desktop verification is pending.

**Add URL** opens a separate dialog for the source, destination, and description. **Options** opens a separate appearance dialog that applies the icon choice on confirmation. The category tree uses bundled Bootstrap folder and file-type icons. Windows desktop automation verified both dialogs, URL validation, applying the icon choice, and a download through the new dialog.

The desktop defaults to a dark palette with subdued grid lines. Empty categories leave the grid unobstructed and show a zero count in the status bar. Windows desktop automation verified the empty view and a subsequent download.

Drag column dividers to resize and click a header to sort. **File type** uses the filename extension. **Date added** records when the entry first entered the list, and retry updates **Last try** on the same entry. **Group by** groups the current category by file type, status, or date added. Windows desktop automation checked resizing, both sort directions, and file-type grouping.

| Step | Deliverable | Acceptance check |
|---|---|---|
| 0. Foundation | Confirm repository state, scaffold the solution and the first required projects, establish build and test entry points. | Build a clean checkout and verify project names and reference direction against the [coding standards](CodingStandards.md). |
| 1. Durable HTTP transfers | Add a URL, choose a filename and destination, handle redirects and collisions, stream progress, pause, cancel, retry, and recover partial files. Support unknown content length. | Compare the final bytes with a known source after interruption. Test an ignored range, a changed remote file, a full disk, and cancellation. |
| 2. Desktop lifetime | English download list and progress dialog, pause/resume, stop all, reopen existing instance, explicit Exit, and recovery on next launch. | Close the window during a transfer and reopen it. Exit, launch again, and verify active and manually paused jobs recover into the appropriate states. |
| 3. Acceleration and limits | Dynamic range splitting, connection reuse, segment progress, speed and ETA, bandwidth limits, per-host exceptions, and single-stream fallback. | Compare single-stream and parallel transfers on the same file. Verify identical bytes, correct handling of throttling, and a host cap shared across concurrent files. |
| 4. Scheduler and queues | Named queues, reordering and moving jobs, per-queue concurrency, retry policy, start/stop windows, recurring schedules, synchronization queues, transfer quotas, and completion actions. | Start a queue with the window closed. Verify scheduled stop, quota recovery, and refresh of changed files. Cover sleep, clock changes, and missed runs using the chosen policy. |
| 5. Managing and adding downloads | Categories by extension and site, folder rules, editable properties and source URLs, refreshed expired links, history and bulk actions, batch URLs, clipboard input, import/export, drag-and-drop basket, and CLI. | Import and review a batch, route files to folders, start the queue from the CLI, refresh an expired address, and distinguish removing history from deleting a file. |
| 6. Browser capture | Chromium-family and Firefox extensions, native messaging, link context menus, selected/all links, takeover and override keys, file and site exclusions, authenticated handoff. Investigate advanced capture from other applications per OS. | Verify each supported browser on both operating systems, including logged-in downloads, exclusions, and cancellation without duplicate transfers. |
| 7. Media | In-player panel, format and quality selection, direct media, supported segmented streams, audio/video assembly, subtitles, and expiring addresses. Document format and site coverage. | Use controlled media fixtures to verify duration, selected quality, audio synchronization, and subtitles in the completed file. |
| 8. Website Grabber | Saved projects and templates, crawl scope, traversal and download filters, discovery preview, selective transfer, offline link rewriting, scheduled refresh. | Mirror a fixture site with relative links and cycles. Browse offline and refresh a changed asset without fetching excluded paths. |
| 9. Network and OS parity | FTP, SFTP over SSH, legacy MMS, system/manual proxy settings and proxy auto-configuration, site and proxy authentication, external scanner hooks, open/run actions, network disconnect and shutdown where supported. | Verify SFTP against a controlled SSH server, including host-key verification, authentication, cancellation, and byte equality. Record results per protocol and OS. Verify completion actions after natural completion and manual stop. |
| 10. UI parity and distribution | Customizable columns and toolbars, skins, themes, notifications, tray integration, language selection, installation, updater, and product decisions on trial and registration. | Verify packaging and upgrades on Windows and the chosen Linux distribution. Preserve partial files and queues across upgrade. Exercise localization fallback and each supported desktop workflow. |

## Cancellation acceptance checks

For milestones 1 through 4, verify the [cooperative cancellation rules](CodingStandards.md) through the normal desktop or application entry points.

- Cancel one segmented download during a stream read. All its segments stop, other downloads continue, and the cancelled job stays out of the retry queue.
- Pause during a retry delay, then resume. The delay ends promptly and the finished file matches the source.
- Exit while transfers and a scheduled wait are active. The scheduler stops dispatching jobs, transfers release their files, and the next launch restores valid checkpoints and preserves manually paused jobs.

## Decisions to make during implementation

- Before scheduler implementation, choose the missed-run policy when the app was closed or the computer slept.
- Before browser and CLI integration, decide what a request should do when the desktop app has exited.
- Before increasing segment defaults, benchmark representative hosts and file sizes. Capture elapsed time, retries, and the effect on simultaneous jobs.
- For each parity milestone, expand its features into individual checks, link the relevant IDM manual entry, and record Windows and Linux results. Document platform limitations and product decisions explicitly.

## Reference documentation

- Transfer behavior and limits. [Dynamic segmentation](https://www.internetdownloadmanager.com/support/segmentation.html) and [connection options](https://www.internetdownloadmanager.com/support/using_idm/options.html)
- User workflows. [Main window](https://www.internetdownloadmanager.com/support/using_idm/using_idm.html), [starting downloads](https://www.internetdownloadmanager.com/support/using_idm/starting.html), and [list import](https://www.internetdownloadmanager.com/support/import_downloads.html)
- Scheduled work and mirroring. [Scheduler](https://www.internetdownloadmanager.com/support/idm-scheduler/idm_scheduler.html) and [Grabber](https://www.internetdownloadmanager.com/support/idm-grabber/idm_grabber.html)
- Integration and product coverage. [Media FAQ](https://www.internetdownloadmanager.com/register/new_faq/video_main.cgi), [feature catalogue](https://www.internetdownloadmanager.com/features2.html), and [release history](https://www.internetdownloadmanager.com/news.html)
- Platform behavior. [.NET lifecycle](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [Avalonia platforms](https://docs.avaloniaui.net/docs/supported-platforms), [.NET resources](https://learn.microsoft.com/en-us/dotnet/core/extensions/resources), and [HTTP range semantics](https://www.rfc-editor.org/rfc/rfc9110.html#name-range-requests)
