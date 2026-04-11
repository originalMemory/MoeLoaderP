## ADDED Requirements

### Requirement: Unfinished-task bundle format (`.mlpub`)

The system SHALL define a single machine-readable format for **unfinished download tasks** export and re-import. Files SHALL use the extension **`.mlpub`** (UTF-8, no BOM required). The payload SHALL be JSON with a root object that includes:

- `"schema"`: literal string **`MoeLoaderP.UnfinishedDownloadTasks`**
- `"version"`: integer **`1`** (bump only when breaking the contract)
- `"exportedAt"`: optional ISO-8601 UTC string for audit
- `"tasks"`: array of task objects (see next requirement)

Any file offered for **drag-import** into the download panel SHALL be rejected unless it is a **`.mlpub`** file whose JSON parses and satisfies the `schema` / `version` keys above. **New writes** and **imports** SHALL use this contract only; **no** backward compatibility with other extensions or legacy `schema` literals is required for v1 planning.

#### Scenario: Valid file is recognized

- **WHEN** a file ends with `.mlpub` and its UTF-8 JSON root has `schema == "MoeLoaderP.UnfinishedDownloadTasks"` and `version == 1`
- **THEN** the application SHALL treat it as a valid bundle for import (subject to task-level validation)

#### Scenario: Wrong extension rejected

- **WHEN** the user drags a file whose extension is not `.mlpub` onto the download list area
- **THEN** the drag operation SHALL NOT enqueue tasks (no `Copy` drop effect for invalid extensions)

#### Scenario: Wrong schema rejected

- **WHEN** a `.mlpub` file parses as JSON but `schema` or `version` does not match the supported contract
- **THEN** the application SHALL reject import and SHALL NOT partially enqueue malformed bundles (show a clear error message)

### Requirement: Task record fields (minimum for round-trip)

Each element of `tasks` SHALL be a JSON object containing at minimum the data needed to enqueue a retry download on a later session, including:

- **`siteShortName`** (string): `MoeSite.ShortName` the item belonged to
- **`downloadUrl`** (string): URL used as the primary download target (the resolved URL at export time when available; if only `ResolveUrlFunc` existed, export SHALL persist the best-known direct URL or document in design that `downloadUrl` may be empty and require detail resolution in a later version)
- **`referer`** (string, optional): HTTP referer for the download request
- **`detailUrl`** (string, optional): detail page URL for sites that need re-resolution
- **`id`** (number or string): site item id when applicable; use `0` or empty convention per design when absent
- **`title`** (string, optional): display title
- **`localFileShortNameWithoutExt`** (string, optional): preferred local base name without extension
- **`dlStatusAtExport`** (string, optional): snapshot of `DownloadStatus` enum name at export time (e.g. `Downloading`, `Failed`) for diagnostics; import SHALL still enqueue as **`WaitForDownload`** unless design specifies otherwise

The export pipeline MAY add more fields for forward compatibility; import SHALL ignore unknown fields.

#### Scenario: Export produces parseable bundle

- **WHEN** the user exports unfinished tasks and at least one exportable item exists
- **THEN** the written `.mlpub` file SHALL conform to this requirement’s root and `tasks` shape and SHALL be UTF-8 decodable JSON

### Requirement: Download list context menu — export unfinished tasks

The download list’s context menu (`DownloaderControl` / equivalent) SHALL expose a command (e.g. Chinese **「导出未成功任务」**) that writes a **`.mlpub`** file. The export set SHALL include **every** `MoeItem` in the current download list whose `DlStatus` is **not a terminal success path**, specifically SHALL include **`Failed`**, **`Downloading`**, **`WaitForDownload`**, **`Stop`**, and **`Cancel`**, and SHALL **exclude** **`Success`** and **`Skip`**. Items that have one or more `ChildrenItems` (multi-image parents) SHALL be **omitted** from export in v1 (no nested `children[]` in the bundle). If there are **zero exportable** items after this filter, the command SHALL show a non-destructive message and SHALL NOT write a file.

#### Scenario: Export includes in-progress downloads

- **WHEN** the queue contains at least one `Downloading` or `WaitForDownload` item that is exportable (no `ChildrenItems`) and the user activates the export command
- **THEN** those items SHALL appear in the exported `tasks` array along with any exportable `Failed` / `Stop` / `Cancel` items

#### Scenario: Export with exportable unfinished items

- **WHEN** the queue contains one or more exportable unfinished items and the user activates the export command
- **THEN** the system SHALL prompt for a save path (or use a documented default naming pattern) and SHALL write one `.mlpub` whose `tasks` array contains exactly that export set

#### Scenario: Only multi-child unfinished rows

- **WHEN** every unfinished row in the queue is a multi-child parent (v1 non-exportable) and the user activates the export command
- **THEN** the system SHALL not write a `.mlpub` file and SHALL inform the user that there is nothing to export under the current rules

#### Scenario: Save to existing valid `.mlpub` merges tasks

- **WHEN** the user chooses a save path where a file already exists, that file is valid UTF-8 JSON with supported `schema` and `version`, and the user confirms save
- **THEN** the system SHALL read the existing `tasks` array, append each new task record that is **not a duplicate** of an existing task (per design: same `siteShortName` and same trimmed `downloadUrl`, or fallback duplicate rule when `downloadUrl` is empty), and SHALL write back a single document with the **merged** `tasks` without discarding prior unrelated tasks

#### Scenario: Save path exists but is not a valid bundle

- **WHEN** the user chooses an existing path that is not a valid `.mlpub` document (wrong schema, corrupt JSON, etc.)
- **THEN** the system SHALL NOT silently overwrite the file with export data; it SHALL show an error and SHALL abort or prompt for a different path (implementation choice documented in design)

#### Scenario: Export with nothing to export

- **WHEN** every download row is `Success` or `Skip`, or there are no rows, and the user activates the export command
- **THEN** the system SHALL not write a `.mlpub` file and SHALL inform the user that there is nothing to export

### Requirement: Main window close — three-way prompt when unfinished work remains

When the user attempts to close the main application window and the download queue contains **at least one** item whose `DlStatus` is **`Failed`**, **`Downloading`**, **`WaitForDownload`**, **`Stop`**, or **`Cancel`**, the system SHALL show a **three-button** confirmation (e.g. WPF `MessageBox` with **`Yes` / `No` / `Cancel`**). **Multi-image parents** (`ChildrenItems` non-empty) with one of those statuses **SHALL** count as unfinished for this prompt **even though** they are **not** exportable into the bundle in v1. The message body SHALL explain the mapping: **Yes** = open save flow to export the **exportable** unfinished bundle then close; **No** = close **without** exporting; **Cancel** = **abort** close. If the user chooses **Yes** but cancels the save dialog without saving, the application SHALL **not** exit (treat as incomplete export / user changed mind). If the user chooses **No**, the window SHALL close without writing a bundle.

#### Scenario: Close while downloading shows prompt

- **WHEN** at least one item is `Downloading` or `WaitForDownload` and the user closes the window
- **THEN** the three-button prompt SHALL appear before exit (unless overridden by tests)

#### Scenario: Cancel close

- **WHEN** the user activates **Cancel** on the close prompt
- **THEN** the close operation SHALL be aborted

### Requirement: Download list drag-drop — import only `.mlpub`

The download panel (`DownloaderControl` or its `ListBox` host) SHALL handle drag-and-drop **only** for **`.mlpub`** files as defined above. Dropping valid files SHALL append each valid task to the download queue in **`WaitForDownload`** (or equivalent) so the existing timer/downloader pipeline can process them. Non-`.mlpub` files SHALL be ignored or explicitly rejected with no side effects.

#### Scenario: Drop valid bundle

- **WHEN** the user drops one or more `.mlpub` files that pass schema validation onto the download list
- **THEN** each contained task SHALL be added to the queue without executing arbitrary code from the file (no script, JSON only)

#### Scenario: Drop non-bundle file

- **WHEN** the user drops `.jpg`, `.txt`, `.json` without `.mlpub` extension, or executable types
- **THEN** the application SHALL not enqueue downloads from those drops

### Requirement: Security and robustness

Import SHALL NOT execute embedded scripts; JSON only. Import SHALL validate URLs with conservative rules (e.g. `http`/`https` only) before network use; invalid URLs SHALL be skipped with a logged or user-visible summary. Path fields in JSON MUST NOT be interpreted as local file read sources for arbitrary path traversal (no silent `File.Read` from attacker-controlled paths beyond documented behavior).

#### Scenario: Non-http URL skipped

- **WHEN** a task record contains a `downloadUrl` that is not `http` or `https`
- **THEN** that task SHALL be skipped during import and SHALL not crash the app
