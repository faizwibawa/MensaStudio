# Implementation Plan: MensaStudio Photo Sorter

## Overview

Implementation in C# / .NET 8, following the design document. The plan starts with the solution scaffold. Next comes `MensaStudio.Core`, built bottom-up from pure components, with each component's FsCheck property tests placed right after it. Then the thin WPF shell (`MensaStudio.App`) is wired onto the finished Core, and the work ends with the single-file publish profile.

All decision logic lives in Core and is verified by automated tests. The WPF UI is verified by building the solution and by the manual smoke checks listed in the design's Testing Strategy. App tasks therefore have no automated test sub-tasks.

Commands (non-watch):
- Build: `dotnet build MensaStudio.sln -c Release`
- Tests: `dotnet test tests/MensaStudio.Core.Tests -c Release`

## Tasks

- [ ] 1. Scaffold solution, projects and test infrastructure
  - [x] 1.1 Create solution and project files
    - Create `MensaStudio.sln` with `src/MensaStudio.Core` (net8.0, Nullable enable, no package references), `src/MensaStudio.App` (net8.0-windows, `UseWPF`, project reference to Core, `CommunityToolkit.Mvvm` 8.3.2) and `tests/MensaStudio.Core.Tests` (net8.0, reference to Core, `xunit` 2.9.3, `xunit.runner.visualstudio` 2.8.2, `Microsoft.NET.Test.Sdk` 17.14.1, `FsCheck.Xunit` 3.6.0, `Microsoft.Extensions.TimeProvider.Testing` 8.10.0), all versions pinned exactly
    - Add `app.manifest` to the App with PerMonitorV2 DPI awareness and `requestedExecutionLevel level="asInvoker"`
    - Add a minimal `App.xaml`/`MainWindow.xaml` placeholder so the solution builds; verify with `dotnet build MensaStudio.sln -c Release`
    - _Requirements: 20.1, 20.3_
  - [x] 1.2 Implement the file system abstraction and typed exceptions
    - `IFileSystem` and `PhysicalFileSystem` exactly as specified (durable write with `Flush(true)`, `ReplaceFile` via `File.Replace` or `File.Move` when the destination is absent, `MoveFile` with `overwrite: false`, `OpenAppend` UTF-8 without BOM, `FileShare.Read`)
    - `FolderReadException`, `SessionFileReadException`, `ApplyLogOpenException`, `SettingsWriteException`, each carrying path and a human-readable reason
    - _Requirements: 1.7, 11.3, 14.7, 15.10, 15.12, 19.11_
  - [-] 1.3 Create the in-memory file system fake and shared generators
    - `tests/.../Fakes/InMemoryFileSystem.cs`: OrdinalIgnoreCase paths, no-overwrite move semantics, recorded operation log for ordering assertions, fault injection (throwing moves, writes, directory creation, enumeration; files pre-created on demand)
    - `tests/.../Generators/FileNameGen.cs`: Windows-valid file names per the design (mixed case, digits with leading zeros, `#`, `=`, ` (n)` suffixes, non-ASCII letters, rating-prefix bias, photo/RAW extensions in random case)
    - `tests/.../Generators/SessionGen.cs`: photo entries and valid `SessionRecordSet` values (unique names OrdinalIgnoreCase, optional folders/ratings, current chosen from entries), with types added as the Core types appear
    - _Requirements: 2.1, 10.7, 13.8_
  - [ ]* 1.4 Write integration tests for `PhysicalFileSystem`
    - Temp directory under `Path.GetTempPath()/MensaStudio.Tests/<guid>`, deleted on dispose; `MoveFile` refuses to overwrite; `ReplaceFile` with and without an existing destination; `OpenAppend` preserves existing lines
    - _Requirements: 11.3, 15.3, 15.10_

- [x] 2. Implement natural sort ordering
  - [x] 2.1 Implement `NaturalSortComparer`
    - Token-based comparison (Number < Char, numbers by length without leading zeros then ordinal digits, chars by `ToUpperInvariant` code unit), `CompareNatural` plus ordinal tie-break in `Compare`, static `Instance`
    - _Requirements: 2.4, 2.5, 2.6, 2.7_
  - [ ]* 2.2 Write property test for natural sort total order
    - **Property 2: Natural sort is a deterministic total order**
    - **Validates: Requirements 2.4, 2.5, 2.7**
  - [ ]* 2.3 Write property test for numeric runs of any length
    - **Property 3: Numeric runs compare by value at any length**
    - **Validates: Requirements 2.6**
  - [ ]* 2.4 Write unit tests for `NaturalSortComparer`
    - `IMG_2` < `IMG_10`, `IMG_9` < `IMG_10`, 20-digit vs 21-digit runs, `IMG_7` vs `IMG_007`, `img_1` vs `IMG_1`
    - _Requirements: 2.6, 2.7_

- [ ] 3. Implement file classification, sidecar matching and folder scanning
  - [x] 3.1 Implement `FileKinds`
    - Photo and RAW extension sets (OrdinalIgnoreCase, final extension only), app file names (session, session temp, apply log), `IsPhoto`, `IsRaw`, `IsAppFile`
    - _Requirements: 2.1, 2.3_
  - [ ]* 3.2 Write property test for file classification
    - **Property 1: Photo and RAW classification uses only the final extension**
    - **Validates: Requirements 2.1, 3.6**
  - [x] 3.3 Implement `ScannedPhoto`, `ScanResult` and `SidecarMatcher.Match`
    - Sort inputs naturally, build Base_Name dictionary with png pass then jpg/jpeg pass overriding, first in Natural_Sort_Order within a group, RAWs per photo in Natural_Sort_Order, discard unmatched RAWs
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.7_
  - [ ]* 3.4 Write property test for sidecar matching
    - **Property 4: Sidecar matching agrees with a reference model and ignores enumeration order**
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.7**
  - [ ] 3.5 Implement `FolderScanner` and `UnsureFolderResolver`
    - Scan: `EnumerateFileNames` only (subfolders excluded by construction), drop app files, split photos/RAWs, `SidecarMatcher.Match`, set `HasSessionFile`; wrap enumeration failures in `FolderReadException`
    - Resolver: direct subfolder named `Unsure` compared OrdinalIgnoreCase, or null
    - _Requirements: 1.3, 1.4, 1.7, 2.1, 2.2, 2.3, 3.1, 16.7_
  - [ ]* 3.6 Write unit and integration tests for matcher, scanner and resolver
    - `IMG_1.jpg.cr2` does not match `IMG_1.jpg`; jpg preferred over png; two RAWs on one photo
    - Temp folder: subfolder named `x.jpg`, nested photos, session/log/`.tmp` files excluded; unreadable folder → `FolderReadException`
    - Resolver: `Unsure`, `unsure`, missing folder
    - _Requirements: 1.3, 1.4, 1.7, 2.2, 2.3, 3.1, 3.2, 3.3, 16.7_

- [x] 4. Implement the rating prefix codec
  - [x] 4.1 Implement `RatingExtraction` and `RatingPrefixCodec`
    - `Extract` (`^[1-5]_`), `Apply` (rating or null), `WithConflictSuffix` inserting ` (n)` before `Path.GetExtension`
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 13.7_
  - [ ]* 4.2 Write property test for prefix extraction
    - **Property 15: Rating prefix extraction**
    - **Validates: Requirements 9.1, 9.2**
  - [ ]* 4.3 Write property test for prefix round trip
    - **Property 16: Rating prefix round trip**
    - **Validates: Requirements 9.3, 9.4, 9.5, 9.8**
  - [ ]* 4.4 Write property test for prefix idempotence
    - **Property 17: Rating prefix application is idempotent**
    - **Validates: Requirements 9.6**
  - [ ]* 4.5 Write unit tests for `RatingPrefixCodec`
    - All examples of Req 9.1–9.4 and the `WithConflictSuffix` examples of Req 13.7 (`3_IMG_0001 (1).jpg`, `IMG_0001 (1) (1).jpg`)
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 13.7_

- [ ] 5. Checkpoint - Ensure all tests pass
  - Run `dotnet build MensaStudio.sln -c Release` and `dotnet test tests/MensaStudio.Core.Tests -c Release`. Ensure all tests pass, ask the user if questions arise.

- [ ] 6. Implement the session model, navigation, commands and formatters
  - [x] 6.1 Implement `PhotoEntry`, `Session`, `SessionSnapshot` and `SessionStatistics`
    - Lock-guarded entry array, `CurrentIndex` (-1 when empty), incremental `AssignedCount`/`RatedCount`, `Version`, `Changed` event, `SetFolder`/`ClearFolder`/`SetRating`/`MoveTo` returning whether anything changed, thread-safe `Snapshot()`
    - _Requirements: 7.1, 7.3, 8.1, 8.2, 11.1, 17.3_
  - [ ]* 6.2 Write property test for session counters
    - **Property 34: Session counters stay consistent**
    - **Validates: Requirements 17.3**
  - [x] 6.3 Implement `Navigation`
    - `Next`, `Previous`, `First`, `Last` with clamping; `NextUnassigned` cyclic search returning `Moved`, `AllAssigned` or `OnlyCurrentUnassigned`
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8_
  - [ ]* 6.4 Write property test for basic navigation
    - **Property 9: Basic navigation moves by one and clamps at the ends**
    - **Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.7**
  - [ ]* 6.5 Write property test for next-unassigned search
    - **Property 10: Next-unassigned search is cyclic and reports the right outcome**
    - **Validates: Requirements 6.5, 6.6, 6.8**
  - [x] 6.6 Implement `FolderMapping`
    - Nine slots (index 0 = numpad 1, null = unmapped), key indexer 1–9, `Default` = Good, Okay, Bad, –, –, –, –, –, Unsure
    - _Requirements: 16.1, 19.1_
  - [x] 6.7 Implement `StatusFormatter`, `OverlayFormatter` and `LegendFormatter`
    - Position text `"{i+1} / {n}"` with InvariantCulture; folder text verbatim or null; `★` × rating or null; RAW badge of distinct upper-case extensions, `+` prefixed, ordinal sorted, space separated, or null; nine legend rows with `unmapped`
    - _Requirements: 17.1, 17.2, 17.4, 17.5, 17.6, 17.7, 17.9, 17.10, 18.4_
  - [ ]* 6.8 Write property test for status position text
    - **Property 33: Status position text has no digit grouping**
    - **Validates: Requirements 17.1**
  - [ ]* 6.9 Write property test for overlay formatting
    - **Property 35: Overlay formatting**
    - **Validates: Requirements 17.4, 17.5, 17.6, 17.9, 17.10**
  - [ ]* 6.10 Write property test for legend rows
    - **Property 36: Legend lists nine keys with names or "unmapped"**
    - **Validates: Requirements 17.7, 18.4**
  - [ ] 6.11 Implement `SessionCommands` (thin Core helper used by `MainViewModel`)
    - `AssignSlot(session, mapping, slot)` → sets `mapping[slot]` and advances with `Navigation.Next`, or returns an `UnmappedKey` outcome with no change; `ClearFolder(session)`; `Rate(session, rating)`; empty sessions are no-ops
    - Keeps the command semantics in Core so Properties 11 and 12 can test them without WPF
    - _Requirements: 6.9, 7.1, 7.2, 7.3, 7.4, 7.6, 7.7, 7.10, 7.12, 8.1, 8.2, 8.3, 8.4, 8.7_
  - [ ]* 6.12 Write property test for folder commands
    - **Property 11: Folder commands change only the current photo's assignment**
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.7, 7.10**
  - [ ]* 6.13 Write property test for rating commands
    - **Property 12: Rating commands change only the current photo's rating**
    - **Validates: Requirements 8.1, 8.2, 8.3, 8.4**
  - [ ]* 6.14 Write unit tests for `SessionCommands`
    - Replacing the `FolderMapping` leaves existing assignments unchanged, including names no longer mapped; commands on an empty session change nothing
    - _Requirements: 6.9, 7.6, 7.12, 8.7, 19.5_

- [x] 7. Implement key mapping and the shortcut catalog
  - [x] 7.1 Implement `InputKey`, `Modifiers`, `UiMode`, `Command` and `KeyCommandMapper.Map`
    - Implement every row of the design's key mapping table, including Normal, HelpVisible, ApplyPreview, Applying and Busy modes, Alt+NumPad left unhandled, Ctrl+, → `OpenSettings` in Normal and Busy only
    - _Requirements: 1.1, 1.3, 1.10, 6.1, 6.2, 6.3, 6.4, 6.5, 7.1, 7.3, 7.5, 7.11, 8.1, 8.2, 8.5, 8.6, 14.1, 14.6, 15.9, 18.2, 18.5, 18.6, 19.12_
  - [x] 7.2 Implement `ShortcutInfo` and `ShortcutCatalog.All`
    - One entry per handled Normal-mode key or key combination with display text and description
    - _Requirements: 18.3_
  - [ ]* 7.3 Write property test for key mapping channels and modes
    - **Property 13: Key mapping separates channels and respects modal states**
    - **Validates: Requirements 1.10, 7.5, 7.11, 8.5, 8.6, 14.6, 15.9, 18.6, 19.12**
  - [ ]* 7.4 Write property test for the help catalog
    - **Property 14: Help catalog lists exactly the handled shortcuts**
    - **Validates: Requirements 18.3**
  - [ ]* 7.5 Write unit tests for `KeyCommandMapper`
    - One example per mapping table row, including numpad 7/1 with NumLock off arriving as Home/End and Ctrl+, in each mode
    - _Requirements: 6.3, 6.4, 7.11, 19.12_

- [ ] 8. Implement the session file printer and parser
  - [x] 8.1 Implement `SessionRecord`, `SessionRecordSet` and `SessionFilePrinter`
    - Header line, `#current=` line (omitted when `CurrentFileName` is null), one `name TAB folder TAB rating` line per entry, CR LF on every line, `PrintBytes` as UTF-8 without BOM
    - _Requirements: 10.1, 10.2, 10.3_
  - [ ] 8.2 Implement `SkipReason`, `SkippedLine`, `SessionParseResult` and `SessionFileParser`
    - Byte overload with strict UTF-8 and optional BOM (`SessionFileReadException` on invalid UTF-8); LF or CR LF; rules evaluated in the design's table order with the TAB test first; first TAB-free `#current=` line wins; skipped lines reported with one-based line numbers and reasons
    - _Requirements: 10.4, 10.5, 10.6, 10.8, 11.11_
  - [ ]* 8.3 Write property test for session file round trip
    - **Property 18: Session file round trip**
    - **Validates: Requirements 10.1, 10.4, 10.7**
  - [ ]* 8.4 Write property test for encoding and parser tolerance
    - **Property 19: Session file encoding and parser tolerance**
    - **Validates: Requirements 10.2, 10.3, 10.4, 10.5**
  - [ ]* 8.5 Write property test for skipped and duplicate lines
    - **Property 20: Malformed and duplicate session lines are skipped and reported**
    - **Validates: Requirements 10.6, 10.8**
  - [ ]* 8.6 Write unit tests for `SessionFileParser`
    - Header-only file, missing `#current=`, BOM + LF, photo named `#1.jpg`, entry named `#current=x.jpg` before and after the real `#current=` line, two TAB-free `#current=` lines, invalid UTF-8
    - _Requirements: 10.4, 10.5, 11.11_

- [ ] 9. Checkpoint - Ensure all tests pass
  - Run `dotnet test tests/MensaStudio.Core.Tests -c Release`. Ensure all tests pass, ask the user if questions arise.

- [ ] 10. Implement session merging
  - [ ] 10.1 Implement `MergeResult` and `SessionMerger.Merge`
    - First matching record per name (OrdinalIgnoreCase), verbatim Folder/Rating for matches, prefix-seeded Rating for new photos, `parse == null` seeds every photo, `DroppedCount` = unmatched + duplicate-skipped, current from `#current=` or 0, `Changed` flag
    - _Requirements: 9.7, 9.9, 9.10, 11.7, 11.8, 11.9, 11.11, 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8, 16.4, 16.5_
  - [ ]* 10.2 Write property test for merge contents
    - **Property 22: Merge keeps exactly the scanned photos with correct values**
    - **Validates: Requirements 9.7, 9.9, 11.7, 12.1, 12.2, 12.4, 12.5, 12.6, 12.7, 16.4, 16.5**
  - [ ]* 10.3 Write property test for merge current position
    - **Property 23: Merge restores the current position or falls back to the first photo**
    - **Validates: Requirements 11.8, 11.9**
  - [ ]* 10.4 Write property test for dropped count and changed flag
    - **Property 24: Merge reports dropped entries and changes accurately**
    - **Validates: Requirements 12.3, 12.8**

- [ ] 11. Implement continuous session saving
  - [ ] 11.1 Implement `SessionStore`, `SaveOutcome` and `SaveStatus`
    - `Attach`/`MarkDirty` with `ITimer` from `TimeProvider` firing at `min(now + 1 s, firstDirty + 5 s)`; save on the thread pool via `Snapshot()` → print → `WriteAllBytesDurable(temp)` → `ReplaceFile`; `SemaphoreSlim(1)` serialization; failure restores dirty, raises `Failed`, does not re-arm; later success raises `Saved`; `FlushAsync`, `SaveNowAsync`, `WhenIdleAsync`, `DisposeAsync`
    - _Requirements: 1.5, 1.9, 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 12.8, 15.7_
  - [ ]* 11.2 Write property test for debounce timing
    - **Property 21: Debounced saving meets the 1 s / 5 s timing bounds**
    - **Validates: Requirements 11.1, 11.2**
  - [ ]* 11.3 Write integration tests for `SessionStore`
    - Temp-write failure leaves previous Session_File content (fault-injecting wrapper over `PhysicalFileSystem`); `FlushAsync` writes pending changes and waits for an in-flight save; failure sets `LastSaveFailed`, next success clears it; retry on next change
    - _Requirements: 1.5, 11.3, 11.4, 11.5, 11.6_
  - [ ]* 11.4 Write integration tests for the load pipeline
    - Real temp folder: scan + parse + merge with an existing Session_File (restored values, skipped-line reporting, dropped entries); an `Unsure` subfolder loaded as Source_Folder uses only its own Session_File and leaves the parent's untouched
    - _Requirements: 1.2, 11.7, 11.12, 12.3, 16.3, 16.4, 16.5_

- [ ] 12. Implement preload window, LRU cache and preload scheduling
  - [x] 12.1 Implement `PreloadWindow.Compute`
    - Order `p, p+1, p+2, p+3, p−1, p−2` filtered to `[0, n−1]`; empty for `n == 0`
    - _Requirements: 5.2, 5.6_
  - [ ]* 12.2 Write property test for the preload window
    - **Property 6: Preload window contents and decode order**
    - **Validates: Requirements 5.2, 5.6**
  - [x] 12.3 Implement `LruCache<TKey, TValue>`
    - Linked list + dictionary with optional comparer; `TryGet`/`Touch` mark MRU; `Add` inserts or replaces as MRU and evicts LRU at capacity; `Contains` does not touch; `Clear`
    - _Requirements: 5.3, 5.7_
  - [ ]* 12.4 Write property test for the LRU cache
    - **Property 8: LRU cache matches a list model**
    - **Validates: Requirements 5.3, 5.7**
  - [ ] 12.5 Implement `PreloadPlan` and `PreloadScheduler.Plan`
    - `NewQueue` = window positions not held, in flight or failed (window order); `ToTouch` = held window positions; `ToCancel` = queued positions outside the window
    - _Requirements: 5.1, 5.4, 5.8_
  - [ ]* 12.6 Write property test for the preload plan
    - **Property 7: Preload plan schedules, touches and cancels correctly**
    - **Validates: Requirements 5.1, 5.4, 5.8**

- [x] 13. Implement decode sizing and EXIF orientation
  - [x] 13.1 Implement `ExifOrientation` and `DecodeSizeCalculator`
    - Tag 1–8 → (rotation, flipX), other or null → identity; `SwapsDimensions` for 5–8; scale = `min(1, sw/w, sh/h, sw/ow, sh/oh)`, rounded with minimum 1, original size returned exactly at scale 1
    - _Requirements: 4.2, 4.3, 4.4_
  - [ ]* 13.2 Write property test for decode size
    - **Property 5: Decode size fits the display and preserves aspect ratio**
    - **Validates: Requirements 4.2**
  - [ ]* 13.3 Write unit tests for `ExifOrientation`
    - All 8 tag mappings; 0, 9 and null → identity
    - _Requirements: 4.3, 4.4_

- [ ] 14. Implement settings validation and persistence
  - [ ] 14.1 Implement `FolderNameRule` and `FolderNameValidator`
    - Invalid file-name characters, `.`/`..`, reserved device names (including superscript COM/LPT, checked on the part before the first `.` with trailing spaces trimmed), trailing space or period, length > 64, `ValidateAll` with OrdinalIgnoreCase duplicates flagged on every involved slot
    - _Requirements: 19.4_
  - [ ]* 14.2 Write property test for folder name validation
    - **Property 37: Folder name validation detects each rule**
    - **Validates: Requirements 19.4**
  - [ ] 14.3 Implement `SettingsLoadResult` and `SettingsStore`
    - `DefaultPath` under `%AppData%\MensaStudio\settings.json`; `Serialize`/`TryDeserialize` with `System.Text.Json` (`version`, exactly 9 `folders`, `""` = unmapped, validation via `ValidateAll`); `Load` never throws (absent → defaults without notice, invalid → defaults with reset reason, file untouched); `Save` via `settings.json.tmp` + replace, creating the directory, throwing `SettingsWriteException`
    - _Requirements: 16.1, 19.1, 19.3, 19.6, 19.7, 19.8, 19.9, 19.11_
  - [ ]* 14.4 Write property test for settings round trip
    - **Property 38: Settings round trip**
    - **Validates: Requirements 19.9**
  - [ ]* 14.5 Write unit and integration tests for settings
    - `FolderMapping.Default` values; `Load` with absent file (defaults, no reset); corrupt or invalid file (defaults, reset reason, file bytes unchanged); save → load on disk; write failure → `SettingsWriteException` with path
    - _Requirements: 16.1, 19.1, 19.3, 19.7, 19.8, 19.11_

- [ ] 15. Checkpoint - Ensure all tests pass
  - Run `dotnet test tests/MensaStudio.Core.Tests -c Release`. Ensure all tests pass, ask the user if questions arise.

- [ ] 16. Implement apply planning and the preview summary
  - [ ] 16.1 Implement plan types, `FolderSnapshot` and `ApplySnapshotReader`
    - `ApplyKind`, `FileMove`, `ApplyOperation`, `ApplyPlan` with `FoldersToEnsure`; snapshot of names directly in the Source_Folder and in each existing subfolder named by a Folder_Assignment; `FolderReadException` with path and reason
    - _Requirements: 13.10, 13.11, 14.1, 14.7_
  - [ ] 16.2 Implement `ApplyPlanner.Plan`
    - The design's algorithm: snapshot paths all taken, Session order, Move vs Rename vs no-op, sidecar targets from their own names, smallest common conflict suffix across photo and sidecars, no I/O
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7, 13.8, 13.9, 13.10, 13.11, 13.12, 13.13, 16.6_
  - [ ]* 16.3 Write property test for operation kinds and target names
    - **Property 25: Planned operations have the right kind, folder and target names**
    - **Validates: Requirements 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.9, 16.6**
  - [ ]* 16.4 Write property test for target uniqueness
    - **Property 26: Planned target paths are unique and never collide with existing files**
    - **Validates: Requirements 13.8**
  - [ ]* 16.5 Write property test for minimal conflict suffixes
    - **Property 27: Conflict suffixes are minimal and assigned in Session order**
    - **Validates: Requirements 13.7, 13.12**
  - [ ]* 16.6 Write property test for planning determinism
    - **Property 28: Planning is deterministic**
    - **Validates: Requirements 13.13**
  - [ ] 16.7 Implement `ApplyPlanSummary` and `ApplyPlanSummarizer`
    - Moves per folder (OrdinalIgnoreCase grouping, first spelling, plan order), sidecar count, unassigned photo count, rename count, conflict count
    - _Requirements: 14.2, 14.5_
  - [ ]* 16.8 Write property test for summary counts
    - **Property 29: Preview summary counts equal plan counts**
    - **Validates: Requirements 14.2**
  - [ ]* 16.9 Write unit tests for planning
    - Concrete examples of Req 13.7 conflict naming, a photo moving into `Unsure` inside an Unsure_Folder session, empty plan when nothing changes, snapshot read failure → `FolderReadException`, performance smoke of 10,000 photos with sidecars and a 10,000-file snapshot planned in well under 2 s
    - _Requirements: 13.5, 13.7, 14.1, 14.5, 14.7, 16.6_

- [ ] 17. Implement apply execution and the apply log
  - [ ] 17.1 Implement `ApplyLogEntry`, `ApplyLogFormatter` and the `IApplyLogWriter` implementation
    - Five TAB-separated fields, `yyyy-MM-dd HH:mm:ss` InvariantCulture local time, `CREATE_FOLDER`/`MOVE`/`RENAME`/`SKIP`, `SUCCESS`/`FAILED: reason`/`SKIPPED: reason`, TAB/CR/LF in reasons replaced by spaces, CR LF line ends, flushed per line via `IFileSystem.OpenAppend`
    - _Requirements: 15.3_
  - [ ] 17.2 Implement `ApplyExecutor.ExecuteAsync` and result types
    - Open log first (`ApplyLogOpenException`, no actions); ensure folders (validator, `CreateDirectory`, failure map); operations in plan order, photo then sidecars, `FileExists` pre-check plus no-overwrite move, skip lines for sidecars of failed photos and for failed folders, outcomes and progress after each operation
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.9, 15.10, 15.11, 15.12, 16.2_
  - [ ]* 17.3 Write property test for executor outcomes
    - **Property 30: Executor outcomes on a fault-injecting file system**
    - **Validates: Requirements 15.2, 15.4, 15.5, 15.6, 15.10, 15.11**
  - [ ]* 17.4 Write property test for apply log lines
    - **Property 31: One log line per file system action**
    - **Validates: Requirements 15.3**
  - [ ]* 17.5 Write integration tests for `ApplyExecutor`
    - Temp folder: folders created before moves; photo + RAW with prefix and suffix moved into `Unsure`; file opened with `FileShare.None` fails and its sidecars are skipped; target created after planning is not overwritten; log open failure aborts with no file action
    - _Requirements: 15.1, 15.4, 15.5, 15.10, 15.12, 16.2_

- [ ] 18. Implement the post-apply session rewrite
  - [ ] 18.1 Implement `PostApplySessionBuilder.Build`
    - Per-photo rules from the design (unchanged, kept on failure, omitted when moved, renamed with folder cleared and rating kept); `CurrentFileName` = post-execution name of the pre-Apply current photo or null
    - _Requirements: 15.7_
  - [ ]* 18.2 Write property test for the post-apply session
    - **Property 32: Post-apply session contains exactly the remaining photos**
    - **Validates: Requirements 15.7**
  - [ ]* 18.3 Write integration test for the apply round trip
    - Temp folder: snapshot → plan → execute → build → `SessionStore.SaveNowAsync` → rescan → merge; verify Session_File entries, restored ratings of renamed photos, current position fallback when the current photo moved
    - _Requirements: 15.7, 15.8, 11.8, 11.9_

- [ ] 19. Checkpoint - Core complete, ensure all tests pass
  - Run `dotnet build MensaStudio.sln -c Release` and `dotnet test tests/MensaStudio.Core.Tests -c Release`. Ensure all tests pass, ask the user if questions arise.

- [ ] 20. Build the WPF shell foundation
  - [ ] 20.1 Implement `MainWindow` layout and `KeyRouter`
    - `MainWindow.xaml`: Viewer area (`Image` with `Stretch="Uniform"`, centered, `BitmapScalingMode="HighQuality"`), overlay layer (folder, stars, RAW badge), status bar, legend, NumLock indicator, notice/toast host, placeholders for help overlay and apply progress
    - `KeyRouter`: `Key.System` → `SystemKey`, WPF `Key` → `InputKey`, `Keyboard.Modifiers` → `Modifiers`, every key-down including repeats, `e.Handled = true` for non-`None` commands; hook `PreviewKeyDown`
    - _Requirements: 4.1, 6.1, 6.2, 6.3, 6.4, 7.5, 7.11, 17.4, 17.5, 17.6, 17.7_
  - [ ] 20.2 Implement App services
    - `NoticeService` (non-modal toasts with at least 2.5 s display, modal message boxes for errors needing acknowledgement), `IDisplayInfo`/`DisplayInfo` (`MonitorFromWindow` + `GetMonitorInfo`, physical pixels), `NumLockMonitor` (`Keyboard.IsKeyToggled(Key.NumLock)` on `Loaded`, `Activated`, key events and a 150 ms `DispatcherTimer`)
    - _Requirements: 4.2, 7.4, 7.8, 7.9_
  - [ ] 20.3 Implement `MainViewModel` core and composition root
    - `App.xaml.cs` builds `PhysicalFileSystem`, `TimeProvider.System`, `SettingsStore` (load at startup, reset notice), `SessionStore`, `MainViewModel`; bind `MainWindow`
    - `[ObservableProperty]` state: `Session?`, `FolderMapping`, `UiMode`, status position/file name/counts, overlay texts, legend rows, NumLock warning, save-error indicator (from `SaveStatusChanged`)
    - Dispatch `Command`s: navigation (including `NextUnassigned` notices), `SessionCommands` for assign/clear/rate (unmapped-key notice naming the key), session-dependent commands dropped without a session or with zero photos; refresh status/overlay/legend on every change
    - _Requirements: 4.9, 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8, 6.9, 7.1, 7.2, 7.3, 7.4, 7.7, 7.9, 7.10, 7.12, 8.1, 8.2, 8.3, 8.7, 11.5, 11.6, 17.1, 17.2, 17.3, 17.4, 17.5, 17.6, 17.7, 17.8, 17.9, 17.10, 17.11, 18.1, 19.6, 19.8_

- [ ] 21. Implement folder loading and safe shutdown
  - [ ] 21.1 Implement Open Folder and Open Unsure flows in `MainViewModel`
    - Ctrl+O via `Microsoft.Win32.OpenFolderDialog` (cancel keeps state); Ctrl+U via `UnsureFolderResolver` (notice when unavailable); `Busy` mode; flush before switching to a different folder (case-insensitive path compare, abort with error on failure); background scan; error with path and reason on `FolderReadException`; "no supported photos" notice keeping the previous session; read and parse Session_File (unreadable → error and scan-only load); merge; notices for skipped lines and dropped entries; `SessionStore.Attach` and `MarkDirty` when `Changed`; reset preload and show current photo
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 11.7, 11.8, 11.9, 11.11, 11.12, 12.3, 12.8, 16.3, 16.4, 16.5, 16.7_
  - [ ] 21.2 Implement window close handling and unhandled-exception safety
    - `MainWindow.Closing`: cancel, defer while `Applying`, await `FlushAsync`; on failure show path and reason with Retry / Exit without saving; then close
    - `Dispatcher`/`AppDomain` unhandled-exception handlers flush the session and show the error
    - _Requirements: 11.4, 11.10, 15.9_

- [ ] 22. Implement image decoding and preloading
  - [ ] 22.1 Implement `ImageLoader` and `ImageDecodeException`
    - `FileStream` with `ReadWrite | Delete` share into `MemoryStream`; `BitmapFrame` with `DelayCreation` for size and EXIF tag 274 (query errors → no tag); `DecodeSizeCalculator`; `BitmapImage` with `DecodePixelWidth` only when scaling and `OnLoad`; `TransformedBitmap` from `ExifOrientation`; `Freeze()`; listed exceptions mapped to `ImageDecodeException`
    - _Requirements: 4.2, 4.3, 4.4, 4.5, 4.8_
  - [ ] 22.2 Implement `PreloadCache` and Viewer states
    - `LruCache<string, BitmapSource>(15, OrdinalIgnoreCase)`, pending queue, in-flight set, failed map, generation counter; `Reset` cancels and clears; `OnPositionChanged` applies `PreloadScheduler.Plan`; two background workers; completion marshalled to the UI thread, stale generations dropped, viewer updated only for the current photo
    - Viewer view-model states: image (cached photos shown immediately, no loading indicator), loading indicator with file name, error placeholder with file name and reason; wire into `MainViewModel` load and navigation paths
    - _Requirements: 3.6, 4.1, 4.5, 4.6, 4.7, 4.8, 4.9, 5.1, 5.2, 5.3, 5.4, 5.5, 5.7, 5.8, 5.9_

- [ ] 23. Implement the help overlay
  - [ ] 23.1 Implement `HelpOverlay` view and toggle logic
    - Rendered from `ShortcutCatalog.All` plus current `LegendFormatter` rows, rebuilt each time it is shown; hidden at startup; F1 toggles, Escape hides; `UiMode.HelpVisible` while shown
    - _Requirements: 18.1, 18.2, 18.3, 18.4, 18.5, 18.6, 18.7_

- [ ] 24. Implement the apply flow
  - [ ] 24.1 Implement `ApplyPreviewDialog` and `ApplyResultDialog`
    - Preview shows `ApplyPlanSummary` fields; Enter confirms (`IsDefault`), Escape or window close cancels (`IsCancel`); result dialog shows success/failure counts and the failed photo names with reasons
    - _Requirements: 14.2, 14.3, 14.4, 14.6, 15.6_
  - [ ] 24.2 Wire the apply flow into `MainViewModel`
    - Ctrl+Enter: background snapshot + plan (error on `FolderReadException`, "Nothing to apply" notice on empty plan), `UiMode.ApplyPreview` while the preview is open; on confirm: `UiMode.Applying`, wait for `SessionStore` idle, execute the same plan with progress indicator (completed / total), log-open error handling, `PostApplySessionBuilder` + `SaveNowAsync`, rescan + merge + current position (no-photos state when the folder is empty), result dialog; cancel leaves everything unchanged
    - _Requirements: 14.1, 14.3, 14.4, 14.5, 14.7, 15.6, 15.7, 15.8, 15.9, 15.12_

- [ ] 25. Implement the settings dialog
  - [ ] 25.1 Implement `SettingsDialog` and its view model
    - Nine `TextBox`es pre-filled from the active mapping; inline messages from `FolderNameValidator.ValidateAll` naming the violated rule; Save writes via `SettingsStore.Save` (inline path + reason on `SettingsWriteException`, dialog stays open); Cancel or close discards edits
    - _Requirements: 19.2, 19.3, 19.4, 19.10, 19.11_
  - [ ] 25.2 Wire Ctrl+, to the settings dialog in `MainViewModel`
    - Open modally on `OpenSettings`; on successful save replace `FolderMapping` and refresh legend before the dialog closes; existing Folder_Assignments untouched
    - _Requirements: 7.6, 17.8, 18.7, 19.3, 19.5, 19.12_

- [ ] 26. Checkpoint - App builds and tests pass
  - Run `dotnet build MensaStudio.sln -c Release` and `dotnet test tests/MensaStudio.Core.Tests -c Release`. The WPF UI is verified by this build plus the manual smoke checks in the design's Testing Strategy. Ensure all tests pass, ask the user if questions arise.

- [ ] 27. Add single-file packaging
  - [ ] 27.1 Create the `win-x64` publish profile
    - `src/MensaStudio.App/Properties/PublishProfiles/win-x64.pubxml` with the design's settings (self-contained, single file, native library self-extract, compression, no trimming, no ReadyToRun, `DebugType=embedded`, `PublishDir=bin\publish\win-x64\`)
    - Run `dotnet publish src/MensaStudio.App -p:PublishProfile=win-x64` and confirm the publish folder contains exactly one `.exe`
    - _Requirements: 20.1, 20.2, 20.3, 20.4_

- [ ] 28. Final checkpoint - Ensure all tests pass
  - Run `dotnet build MensaStudio.sln -c Release` and `dotnet test tests/MensaStudio.Core.Tests -c Release`. Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional test tasks and can be skipped for a faster MVP. All 38 design properties are covered by tasks 2.2–18.2.
- Each property test sits in its own file `tests/MensaStudio.Core.Tests/Properties/PropertyNNTests.cs` with a single `[Property(MaxTest = 100)]` (or higher) test and the tag comment `// Feature: mensa-studio-photo-sorter, Property NN: <title>`. Property-specific generators live in that file. Shared generators from task 1.3 are reused, not modified.
- Unit tests go in `tests/MensaStudio.Core.Tests/Unit/`, integration tests in `tests/MensaStudio.Core.Tests/Integration/`, and integration tests use temp directories that are deleted on dispose.
- `SessionCommands` (task 6.11) is a thin Core helper. It makes the `AssignSlot`/`ClearFolder`/`Rate` semantics that the design attributes to the view model testable for Properties 11 and 12. `MainViewModel` delegates to it.
- The WPF App (tasks 20–25) has no automated UI tests. It is verified by building the solution and by the manual smoke checks in the design (viewer fit and EXIF rotation, responsiveness on large folders, NumLock indicator, help overlay, dialogs, packaging on a clean Windows VM).
- Use only non-watch commands: `dotnet build MensaStudio.sln -c Release`, `dotnet test tests/MensaStudio.Core.Tests -c Release`.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "2.1", "3.1", "4.1", "6.1", "6.6", "7.1", "8.1", "12.1", "12.3", "13.1"] },
    { "id": 2, "tasks": ["1.3", "3.3", "6.3", "6.7", "7.2", "8.2", "12.5", "14.1", "16.1"] },
    { "id": 3, "tasks": ["3.5", "6.11", "10.1", "11.1", "14.3", "16.2", "17.1", "1.4", "2.2", "2.3", "2.4", "3.2", "3.4", "4.2", "4.3", "4.4", "4.5", "6.2", "6.4", "6.5", "6.8", "6.9", "6.10", "7.3", "7.4", "7.5", "8.3", "8.4", "8.5", "8.6", "12.2", "12.4", "12.6", "13.2", "13.3", "14.2"] },
    { "id": 4, "tasks": ["16.7", "17.2", "3.6", "6.12", "6.13", "6.14", "10.2", "10.3", "10.4", "11.2", "11.3", "14.4", "14.5", "16.3", "16.4", "16.5", "16.6"] },
    { "id": 5, "tasks": ["18.1", "16.8", "16.9", "17.3", "17.4", "17.5", "11.4"] },
    { "id": 6, "tasks": ["18.2", "18.3"] },
    { "id": 7, "tasks": ["20.1", "20.2", "22.1", "24.1", "25.1"] },
    { "id": 8, "tasks": ["20.3"] },
    { "id": 9, "tasks": ["21.1"] },
    { "id": 10, "tasks": ["21.2"] },
    { "id": 11, "tasks": ["22.2"] },
    { "id": 12, "tasks": ["23.1"] },
    { "id": 13, "tasks": ["24.2"] },
    { "id": 14, "tasks": ["25.2"] },
    { "id": 15, "tasks": ["27.1"] }
  ]
}
```
