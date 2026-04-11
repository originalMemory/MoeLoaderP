## ADDED Requirements

### Requirement: Retry failed thumbnails (context menu, concise label, Ctrl+R)

The system SHALL provide a single **retry-failed-thumbnails** behavior invoked from (a) a context-menu command on the thumbnail grid background menu and (b) **Ctrl+R** in the same thumbnail browsing scope as **Ctrl+A** / **Ctrl+D**. The context-menu command SHALL sit at the **same structural level** as the existing “select all” entry (same `SpPanel` / sibling row as “全选”, not nested under per-item menus). The visible menu label SHALL be **concise** (for Chinese UI, prefer roughly **4 characters** such as “重试失败”; avoid long phrases like “重试失败缩略图” unless a longer string is required for a11y in a specific locale).

The bulk-retry orchestration SHALL enumerate **only** thumbnails on the **currently displayed** page that are in a **failed, error, or empty/placeholder** load state. For those items it MAY call the existing load path (including `TryLoadThumbnailStreamAsync` where appropriate). The system SHALL **NOT** invoke that load path for items whose thumbnail has **already loaded successfully** (decoded image already shown); calling `TryLoadThumbnailStreamAsync` always performs a network fetch, so skipping successful items avoids redundant re-downloads.

#### Scenario: Command appears with select all

- **WHEN** the user opens the right-click context menu on the thumbnail page area where “全选” is shown
- **THEN** a retry command SHALL appear as a sibling to “全选” in that menu strip with a concise label

#### Scenario: Ctrl+R matches menu retry

- **WHEN** the user presses **Ctrl+R** while focus is in the thumbnail browsing scope
- **THEN** the system SHALL run the **same** retry-failed-thumbnails logic as when the user chooses the context-menu retry command (same scope: current visible page, failed items only)

#### Scenario: Retry reloads failures on current page

- **WHEN** the user activates retry (menu or Ctrl+R) and one or more visible thumbnails are in a failed or error placeholder state from a prior load attempt
- **THEN** the system SHALL re-issue thumbnail fetch for those items on the **currently displayed** thumbnail page without requiring the user to change search or page

#### Scenario: Successful thumbnails are not re-fetched

- **WHEN** the user activates retry (menu or Ctrl+R) and a visible thumbnail has **already** completed a successful load and displays the image
- **THEN** the system SHALL NOT call `TryLoadThumbnailStreamAsync` (or equivalent full re-fetch) for that item as part of this bulk-retry action

#### Scenario: No selection required

- **WHEN** the user activates retry (menu or Ctrl+R)
- **THEN** the operation SHALL NOT depend on checkbox selection; it SHALL target failed loads by item/control state, not by `SelectedImageControls`

### Requirement: Ctrl+D downloads the current selection

The system SHALL register **Ctrl+D** in the thumbnail browsing surface (the same interaction scope where **Ctrl+A** already triggers “select all”) so that pressing Ctrl+D enqueues a download for every **currently selected** thumbnail whose `DownloadUrlInfo` is valid, using the **same** enqueue path as the existing “add selected to downloader” action (including opening/showing the downloader panel when applicable).

#### Scenario: Ctrl+D with selection

- **WHEN** the user has one or more thumbnails selected in the explorer and presses Ctrl+D while focus is in the thumbnail browsing scope
- **THEN** each selected item with a usable download URL SHALL be added to the download queue exactly as the existing mouse-driven download-selected flow does

#### Scenario: Ctrl+D with no selection

- **WHEN** no thumbnails are selected and the user presses Ctrl+D in the thumbnail browsing scope
- **THEN** the system SHALL NOT enqueue downloads (no-op or same behavior as existing download-selected with zero selection)

### Requirement: Download URL prefers original image over preview or sample

When a `MoeItem` has multiple candidate URLs for the file that will be written by the downloader (e.g. sample, web-sized, and original), the system SHALL set or resolve `DownloadUrlInfo` so that the **original / highest-fidelity** URL is chosen **by default** before download starts, unless a higher-priority user or site-specific setting already fixes the format (document any exception in design). Sites that only expose a single URL remain unchanged.

#### Scenario: Multi-candidate item uses original

- **WHEN** site parsing populates more than one resolution or format link for the same logical post and the user has not chosen a conflicting explicit format override
- **THEN** `DownloadUrlInfo.Url` (or the resolved URL after `ResolveUrlFunc` if used) SHALL refer to the original-quality asset rather than a thumbnail or inline preview URL

#### Scenario: Single URL unchanged

- **WHEN** the site provides only one download URL
- **THEN** behavior SHALL match the previous single-URL download path with no user-visible regression
