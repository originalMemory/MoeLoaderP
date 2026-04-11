# viewed-image-tracking Specification

## Purpose
TBD - created by archiving change track-viewed-images. Update Purpose after archive.
## Requirements
### Requirement: Per-site viewed ID storage

The system SHALL maintain, for each image site, a persistent set of viewed image numeric IDs using the same compressed encoding model as the reference `ViewedID` implementation (range runs, `IsViewed`, session `AddViewingId` merged into stored ranges on save). The system SHALL NOT import or migrate viewed data from MoeLoader-Delta or any external legacy config.

#### Scenario: Persist after session

- **WHEN** the user has browsed one or more images for a site and the application saves settings or exits in a normal shutdown path
- **THEN** the viewed ID data for that site SHALL be written to MoeLoaderP persistent storage so that a subsequent launch can restore `IsViewed` for those IDs

#### Scenario: Load on startup

- **WHEN** the application starts and stored viewed ID data exists for a site short name
- **THEN** the in-memory viewed ID structure for that site SHALL be populated from storage without losing valid compressed ranges

### Requirement: Mark items when items enter the result collection

The system SHALL, when a `MoeItem` is added to a `MoeItems` collection for a site fetch, set `IsViewed` from the site’s `ViewedID` and, for IDs not yet persisted as viewed, SHALL record them via `AddViewingId` for later serialization. This marking SHALL occur before `LocalFilter()` and SHALL NOT remove items from the collection based on the global “mask viewed” setting.

#### Scenario: Previously viewed item

- **WHEN** a fetched item’s ID is already in the stored viewed set for that site
- **THEN** `IsViewed` on that `MoeItem` SHALL be true before `LocalFilter()` runs

#### Scenario: First-time item on a fetch

- **WHEN** a fetched item’s ID is not yet in the stored viewed set
- **THEN** the system SHALL add that ID through the same session mechanism as the reference `AddViewingId` path so it can be merged into persisted ranges on save

### Requirement: Global “mask viewed” search setting (display only)

The system SHALL provide a global boolean setting in **Settings → Search** (same settings group as other search options), default **off**, bound in the settings UI. When **on**, the system SHALL hide or omit viewed items **only** when populating the **current** thumbnail strip (`ImageItemsWrapPanel` or equivalent) for the page being shown. When **off**, all non–locally-filtered items for that page SHALL be eligible for display regardless of viewed state. The mask SHALL NOT be implemented by marking viewed items as `IsLocalFilter` solely for masking, because that would change `FilterCount` and the `SearchNextVisualPage` loop that uses `Count - FilterCount`.

#### Scenario: Mask does not use LocalFilter for viewed-only reason

- **WHEN** the global mask setting is on and an item is viewed and passes all existing `LocalFilter` rules (resolution, rating, etc.)
- **THEN** `IsLocalFilter` on that item SHALL still be false (the mask hides it only when creating thumbnail controls, not via `LocalFilter`)

#### Scenario: Default does not hide viewed items

- **WHEN** the global mask setting is at its default (off)
- **THEN** viewed items SHALL still appear in the current page thumbnail area if not removed by other filters (e.g. `LocalFilter`)

#### Scenario: Mask on does not change fetch or paging

- **WHEN** the user turns the global mask setting on and loads or flips pages
- **THEN** `SearchSession` and site paging (`TryGetRealPage`, `HasNextPage`, page indices) SHALL behave the same as with the setting off; only the final step that adds thumbnail controls for the **currently displayed** page MAY omit or hide viewed items

### Requirement: Viewed state border on thumbnails

The system SHALL render a distinct border color on the thumbnail chrome for viewed items versus non-viewed items. Non-viewed items SHALL keep the existing default border appearance. Viewed items SHALL use a rose/mauve border derived from the reference tint RGB (233, 147, 170), as an opaque brush (for example `#FFE993AA` or a documented contrast-adjusted sibling), defined as a reusable WPF resource key.

#### Scenario: Viewed thumbnail

- **WHEN** a `MoeItem` has `IsViewed` true and its thumbnail control is displayed
- **THEN** the outer thumbnail border brush SHALL use the viewed-image border resource, not the default non-viewed border brush

#### Scenario: Not viewed thumbnail

- **WHEN** a `MoeItem` has `IsViewed` false
- **THEN** the thumbnail border SHALL match the existing default styling for non-viewed items

#### Scenario: Selected and viewed both visible

- **WHEN** the user checks the item (selection on) and the item is viewed
- **THEN** the control SHALL show the existing selection chrome (e.g. blue highlight from the checkbox template) **in addition to** a visible viewed-state treatment (e.g. outer viewed-colored ring with inner/overlaid selection), so viewed is not lost when selected

### Requirement: No conflicting reuse of existing accent colors

The viewed border resource SHALL NOT reuse the primary selection/highlight blue (`#FF00B9FF` family) so that viewed state remains visually distinct from selection and site-mode accents.

#### Scenario: Distinct from selection

- **WHEN** an item is both selected and viewed
- **THEN** the UI SHALL keep viewed border and selection affordance distinguishable (e.g., thickness, layering, or corner treatment) as defined in design

### Requirement: Status bar shows current search page totals and viewed count

The system SHALL append to the main window bottom status area used for the current search summary (the same `TextBlock` as `GetCurrentSearchStateText()` in `MainWindow.xaml`, `SiteTextBlock`) a short phrase whose meaning is **only the currently displayed search page** (`SearchedVisualPage` that is active in the explorer paging UI), **not** the whole `SearchSession` aggregate. **N** SHALL be the count of `MoeItem` instances enumerated as direct elements of every `SearchedPage` in **that** visual page’s `RealPages` only (`foreach (var rp in visualPage.RealPages) foreach (item in rp)`). **M** SHALL be how many of those items have `IsViewed` true. The wording SHALL make the “this page” scope explicit (e.g. Chinese “本页”) so it is not confused with other UI or logs that already show a session-wide or fetch-wide total count. The system SHALL NOT double-count nested `ChildrenItems` for **N** and **M**. When there is no current visual page or counts are zero, the appended phrase MAY be omitted or show zeros as defined in design.

#### Scenario: Switch paging tab updates counts

- **WHEN** the user switches the paging control to another `SearchedVisualPage` and that page is shown in the explorer
- **THEN** **N** and **M** in the status line SHALL be recomputed from that page’s `RealPages` only

#### Scenario: Same session, different page different numbers

- **WHEN** two `SearchedVisualPage` instances in the same `SearchSession` have different numbers of items or different viewed counts
- **THEN** the appended **N** and **M** SHALL change when the user switches between those pages to match the active page only

#### Scenario: New search resets page stats

- **WHEN** the user starts a new search session that replaces `Settings.CurrentSession`
- **THEN** the appended phrase SHALL reflect the new session’s current visual page (or be hidden until a page exists), not previous sessions’ pages

### Requirement: Children items follow parent for viewed display

The system SHALL NOT maintain separate viewed-ID tracking or separate viewed-border rules for `ChildrenItems`. Child thumbnails SHALL derive viewed presentation from their parent `MoeItem` (or equivalent parent binding).

#### Scenario: Child thumbnail

- **WHEN** a child `MoeItem` is shown under a parent and the parent is viewed
- **THEN** the child thumbnail viewed styling SHALL match the parent’s viewed state without writing a separate ID for the child into `ViewedID`

