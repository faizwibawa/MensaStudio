# Requirements Document

## Introduction

MensaStudio is a personal Windows desktop application (C# / .NET 8 / WPF) for manually sorting large photo sets (thousands of photos per session) using the keyboard as the primary controller. The user opens a source folder, steps through the photos, assigns each photo to a destination subfolder with the numpad, and optionally rates it with the top-row number keys. Decisions are staged in a plain-text session file inside the source folder and are only applied (files moved and renamed) when the user explicitly confirms an Apply operation after reviewing a summary preview. RAW files that share a base name with a displayed image travel with that image. The session file is saved continuously so that work can be resumed after closing the application.

## Glossary

- **MensaStudio**: The desktop application as a whole.
- **Source_Folder**: The folder the user opens for sorting. Destination folders are created as direct subfolders of the Source_Folder.
- **Photo**: A displayable image file directly inside the Source_Folder with extension `.jpg`, `.jpeg` or `.png` (case-insensitive).
- **RAW_Extension**: One of `.cr2`, `.cr3`, `.nef`, `.arw`, `.raf`, `.orf`, `.rw2`, `.dng`, `.raw`, `.pef`, `.srw` (case-insensitive).
- **RAW_Sidecar**: A file directly inside the Source_Folder with a RAW_Extension whose base name (file name without extension) equals the base name of a Photo, compared case-insensitively.
- **Base_Name**: A file name without its final extension.
- **Folder_Scanner**: The component that enumerates Photos and RAW files in the Source_Folder and orders them.
- **Natural_Sort_Order**: An ordering of file names in which runs of digits are compared by numeric value and other characters are compared case-insensitively (for example `IMG_2.jpg` precedes `IMG_10.jpg`).
- **Sidecar_Matcher**: The component that pairs RAW_Sidecars with Photos.
- **Session**: The in-memory ordered list of Photos in the Source_Folder together with each Photo's Folder_Assignment, Rating and the Current_Position.
- **Folder_Assignment**: The destination folder name staged for a Photo, or none.
- **Rating**: An integer from 1 to 5 staged for a Photo, or none.
- **Current_Position**: The Photo currently displayed by the Viewer.
- **Session_File**: The plain-text file `MensaStudio.session.txt` in the Source_Folder that persists the Session.
- **Session_File_Parser**: The component that reads Session_File text into a Session record set.
- **Session_File_Printer**: The component that formats a Session record set into Session_File text.
- **Session_Store**: The component that schedules and performs saving of the Session_File.
- **Rating_Prefix**: A file name prefix of the form `<d>_` where `<d>` is a single digit from 1 to 5, matching the pattern `^[1-5]_`.
- **Rating_Prefix_Codec**: The component that extracts a Rating from a file name and produces a file name with a given Rating_Prefix.
- **Folder_Mapping**: The configurable association between numpad keys 1–9 and folder names.
- **Unmapped_Key**: A numpad key from 1 to 9 whose Folder_Mapping folder name is empty.
- **Settings_Store**: The component that loads and saves the Folder_Mapping in `%AppData%\MensaStudio\settings.json`.
- **Settings_Dialog**: The dialog in which the user edits the nine Folder_Mapping folder names.
- **Key_Router**: The component that translates keyboard input in the main window into application commands.
- **Viewer**: The main-window area that displays the Photo at the Current_Position.
- **Image_Loader**: The component that decodes Photos in the background.
- **Preload_Cache**: The least-recently-used cache of decoded Photos and the associated preload scheduling.
- **Preload_Window**: The set of Photos from 2 positions before to 3 positions after the Current_Position.
- **Overlay**: The information layer drawn over the Viewer showing the Folder_Assignment, Rating stars and RAW badge of the current Photo.
- **Legend**: The on-screen list of the nine Folder_Mapping entries.
- **Status_Bar**: The bar showing position, file name and progress counts.
- **Help_Overlay**: The on-screen list of all keyboard shortcuts.
- **NumLock_Indicator**: The on-screen indicator of the current NumLock state.
- **Apply_Operation**: A planned file system action for one Photo: a move into a destination subfolder or a rename in place, including the associated RAW_Sidecar when present.
- **Apply_Plan**: The complete list of Apply_Operations computed from the Session and the current file system state.
- **Apply_Planner**: The pure component that computes the Apply_Plan.
- **Apply_Preview**: The dialog that summarizes the Apply_Plan and asks the user to confirm or cancel.
- **Apply_Executor**: The component that carries out the Apply_Plan on the file system.
- **Apply_Log**: The plain-text file `MensaStudio.apply.log.txt` in the Source_Folder recording every file system action of an Apply.
- **Conflict_Suffix**: A suffix of the form ` (n)`, with n a positive integer, inserted before the extension to make a target file name unique.
- **Unsure_Folder**: The subfolder named `Unsure` inside the Source_Folder.

## Requirements

### Requirement 1: Open a Source Folder

**User Story:** As a photographer, I want to open a folder of photos with the keyboard, so that I can start or resume sorting it.

#### Acceptance Criteria

1. WHEN the user presses Ctrl+O, THE MensaStudio SHALL display a folder selection dialog.
2. WHEN the user selects a folder in the folder selection dialog, THE MensaStudio SHALL load that folder as the Source_Folder by scanning it per Requirements 2 and 3, restoring and merging its Session_File per Requirements 11 and 12 when one exists, and displaying the Photo at the resulting Current_Position in the Viewer.
3. WHEN the user presses Ctrl+U while a Source_Folder is open and the Unsure_Folder of that Source_Folder exists, THE MensaStudio SHALL load the Unsure_Folder as the Source_Folder in the same way as a folder selected in the folder selection dialog.
4. IF the user presses Ctrl+U while no Source_Folder is open or while the Unsure_Folder of the open Source_Folder does not exist, THEN THE MensaStudio SHALL display a notice stating that no Unsure_Folder is available and keep the current Source_Folder, Session and Current_Position unchanged.
5. WHEN a Source_Folder whose full path differs, compared case-insensitively, from the full path of the open Source_Folder is about to be loaded while a Session is open, THE Session_Store SHALL save the open Session to its Session_File, including pending changes, before the new Source_Folder is loaded.
6. IF the folder being loaded as the Source_Folder contains no Photos, THEN THE MensaStudio SHALL display a message stating that the folder contains no supported photos, create no Session_File in that folder, and keep the previously open Source_Folder and Session (if any) open and unchanged.
7. IF the folder being loaded as the Source_Folder cannot be read (for example because it no longer exists, access is denied or the drive is unavailable), THEN THE MensaStudio SHALL display an error message containing the folder path and the failure reason, and keep the previously open Source_Folder and Session (if any) open and unchanged.
8. WHEN the user cancels the folder selection dialog, THE MensaStudio SHALL close the dialog and keep the current Source_Folder, Session and Current_Position unchanged.
9. IF saving the open Session to its Session_File fails before a different Source_Folder is loaded, THEN THE MensaStudio SHALL display an error message stating that the Session could not be saved, cancel loading the new Source_Folder, and keep the open Source_Folder and Session in memory unchanged.
10. WHILE the Apply_Executor is running, THE Key_Router SHALL ignore Ctrl+O and Ctrl+U.

### Requirement 2: Scan and Order Photos

**User Story:** As a photographer, I want photos listed in the order my camera numbered them, so that I review them in shooting sequence.

#### Acceptance Criteria

1. WHEN a Source_Folder is loaded, THE Folder_Scanner SHALL list as a Photo every file directly inside the Source_Folder whose final extension is `.jpg`, `.jpeg` or `.png`, compared case-insensitively (for example `IMG_1.JPG` is listed and `IMG_1.jpg.txt` is not), and SHALL list no other file as a Photo.
2. THE Folder_Scanner SHALL exclude from the Photo list every file located in a subfolder of the Source_Folder at any depth, and every subfolder itself, including subfolders whose names end in `.jpg`, `.jpeg` or `.png`.
3. THE Folder_Scanner SHALL exclude from the Photo list the Session_File, the Apply_Log and every temporary file written by the Session_Store during the save procedure of Requirement 11.3, regardless of whether a save is in progress when the scan runs.
4. THE Folder_Scanner SHALL order the Photos in ascending Natural_Sort_Order of their full file names, including any Rating_Prefix and the extension.
5. FOR ALL lists of file names, ordering by Natural_Sort_Order SHALL produce the identical sequence for every permutation of the input list, and for any two distinct file names exactly one SHALL precede the other (deterministic total order).
6. FOR ALL file names that differ only in the value of one numeric run, THE Folder_Scanner SHALL order the file with the smaller numeric value first, for numeric runs of any length up to the full file name length (for example `IMG_9.jpg` precedes `IMG_10.jpg`, and `IMG_99999999999999999999.jpg` precedes `IMG_100000000000000000000.jpg`).
7. IF two distinct file names compare as equal under Natural_Sort_Order (for example `IMG_7.jpg` and `IMG_007.jpg`, or `img_1.jpg` and `IMG_1.jpg`), THEN THE Folder_Scanner SHALL order them by ordinal character-by-character comparison of the full file names.

### Requirement 3: Match RAW Sidecars

**User Story:** As a photographer who shoots RAW+JPG, I want each RAW file to follow its JPG, so that I sort pairs together.

#### Acceptance Criteria

1. WHEN a Source_Folder is scanned, including the rescan after an Apply per Requirement 15.8, THE Sidecar_Matcher SHALL associate each file located directly inside the Source_Folder with a RAW_Extension to the Photo whose Base_Name equals the RAW file's Base_Name, comparing the full Base_Names (including any Rating_Prefix) case-insensitively, where the Base_Name is the file name with only its final extension removed (for example `IMG_1.jpg.cr2` has Base_Name `IMG_1.jpg` and does not match `IMG_1.jpg`).
2. IF both a `.jpg`/`.jpeg` Photo and a `.png` Photo share the Base_Name of a RAW file, THEN THE Sidecar_Matcher SHALL associate the RAW file with the `.jpg`/`.jpeg` Photo and SHALL associate the RAW file with no `.png` Photo.
3. IF multiple RAW files share the Base_Name of one Photo, THEN THE Sidecar_Matcher SHALL associate every one of those RAW files with that Photo, ordered by Natural_Sort_Order of the RAW file names.
4. THE Sidecar_Matcher SHALL associate each RAW file with at most one Photo, and IF more than one Photo of the same preference group (`.jpg`/`.jpeg`, or `.png`) shares the Base_Name of a RAW file, THEN THE Sidecar_Matcher SHALL associate the RAW file with the Photo whose file name comes first in Natural_Sort_Order.
5. IF a RAW file matches no Photo, THEN THE MensaStudio SHALL leave that RAW file out of the Photo list, out of the Photo count, out of the RAW_Sidecar count in the Apply_Preview and out of every Apply_Operation, and SHALL leave that RAW file at its original path with its original file name.
6. THE Viewer SHALL display only Photos and SHALL display no RAW file as a separate entry.
7. FOR ALL sets of file names in a Source_Folder, THE Sidecar_Matcher SHALL produce the same RAW-to-Photo associations regardless of the order in which the files are enumerated (deterministic matching).

### Requirement 4: Display Photos

**User Story:** As a photographer, I want each photo shown correctly oriented and sharp at screen size, so that I can judge it.

#### Acceptance Criteria

1. WHEN the Current_Position changes or the Viewer area is resized, THE Viewer SHALL display the Photo at the Current_Position centered in the Viewer area. The whole image SHALL be visible without cropping, the width-to-height ratio of the oriented image SHALL be preserved, and either the displayed width SHALL equal the Viewer area width or the displayed height SHALL equal the Viewer area height.
2. THE Image_Loader SHALL decode each Photo so that the decoded width and height do not exceed the physical pixel width and height of the display showing the main window at the time the decode starts. The decoded image SHALL preserve the Photo's width-to-height ratio, and a Photo whose original dimensions already fit within those bounds SHALL be decoded at its original dimensions.
3. WHEN a Photo contains an EXIF orientation tag with a value from 1 to 8, THE Image_Loader SHALL rotate and/or flip the decoded image according to that value, so that the displayed image is the upright view that the tag defines.
4. IF a Photo has no EXIF orientation tag or has an orientation value outside 1 to 8, THEN THE Image_Loader SHALL present the decoded image in its stored pixel orientation without rotating or flipping it.
5. THE Image_Loader SHALL decode Photos on a background thread, and while one or more decodes are in progress THE Key_Router SHALL process each key press in the main window within 100 ms of the key press.
6. WHILE the Photo at the Current_Position is still being decoded, THE Viewer SHALL display a loading indicator together with the Photo's file name in place of any previously displayed Photo.
7. IF a decode finishes for a Photo that is no longer at the Current_Position, THEN THE Viewer SHALL leave its displayed content unchanged and SHALL NOT display that decoded Photo.
8. IF a Photo cannot be decoded because its file is missing, locked or unreadable, or because its content is not a valid image of its file format, THEN THE Viewer SHALL display an error placeholder containing the Photo's file name and an indication that the Photo could not be decoded.
9. WHILE the Viewer displays the error placeholder for the Photo at the Current_Position, THE Key_Router SHALL accept assignment, rating and navigation commands for that Photo and apply them exactly as it does for a successfully decoded Photo.

### Requirement 5: Preload and Cache Photos

**User Story:** As a photographer sorting thousands of photos, I want the next photos ready before I reach them, so that stepping through feels instant.

#### Acceptance Criteria

1. WHEN the Current_Position changes, THE Preload_Cache SHALL schedule decoding of every Photo in the Preload_Window that is not already held decoded in the Preload_Cache, and SHALL mark every already-held Photo in the Preload_Window as most recently used.
2. WHEN the Preload_Cache schedules decoding for a Preload_Window, THE Preload_Cache SHALL start decoding the Photo at the Current_Position before any other Photo, followed by the Photos at positions +1, +2, +3, −1 and −2 relative to the Current_Position, in that order, skipping positions outside the Preload_Window.
3. THE Preload_Cache SHALL hold at most 15 decoded Photos at any time.
4. WHEN the Current_Position changes, THE Preload_Cache SHALL cancel every decode request that has not yet started for a Photo outside the new Preload_Window.
5. WHEN the Current_Position changes to a Photo that is held decoded in the Preload_Cache, THE Viewer SHALL display that Photo within 100 ms after the key press that changed the Current_Position, without displaying the loading indicator.
6. FOR ALL Current_Position values p in a Session of N Photos with N ≥ 1, using zero-based positions, THE Preload_Window SHALL contain exactly the positions from max(0, p−2) to min(N−1, p+3), without wrapping around either end of the Session.
7. WHEN a decoded Photo is added to the Preload_Cache while 15 decoded Photos are held, THE Preload_Cache SHALL evict the decoded Photo that was least recently added, displayed by the Viewer or marked as most recently used, so that the count stays at 15.
8. IF decoding a Photo scheduled by the Preload_Cache fails, THEN THE Preload_Cache SHALL hold no decoded image for that Photo, SHALL continue decoding the remaining Photos in the Preload_Window, and SHALL not schedule that Photo for decoding again until a Source_Folder is next loaded, with THE Viewer displaying the error placeholder per Requirement 4.8 whenever that Photo is at the Current_Position.
9. WHEN a Source_Folder is loaded or rescanned, THE Preload_Cache SHALL discard every held decoded Photo and cancel every pending decode request of the previous Photo list before scheduling decoding for the new Preload_Window.

### Requirement 6: Navigate Photos

**User Story:** As a photographer, I want to move back and forth with the keyboard, so that I can revise earlier decisions.

#### Acceptance Criteria

1. WHEN the user presses Right Arrow and the Current_Position is not the last Photo, THE MensaStudio SHALL move the Current_Position forward by exactly one Photo for each key-down event, including key-down events generated by holding the key down.
2. WHEN the user presses Left Arrow and the Current_Position is not the first Photo, THE MensaStudio SHALL move the Current_Position back by exactly one Photo for each key-down event, including key-down events generated by holding the key down.
3. WHEN the user presses Home, including numpad 7 while NumLock is off, THE MensaStudio SHALL move the Current_Position to the first Photo in the Session.
4. WHEN the user presses End, including numpad 1 while NumLock is off, THE MensaStudio SHALL move the Current_Position to the last Photo in the Session.
5. WHEN the user presses N and at least one Photo other than the current Photo has no Folder_Assignment, THE MensaStudio SHALL move the Current_Position to the first Photo without a Folder_Assignment found by searching from the Photo after the Current_Position to the last Photo, then continuing from the first Photo up to the Photo before the Current_Position.
6. IF the user presses N and every Photo in the Session has a Folder_Assignment, THEN THE MensaStudio SHALL keep the Current_Position unchanged and display a notice stating that all photos are assigned.
7. IF the user presses Right Arrow or End at the last Photo, or Left Arrow or Home at the first Photo, THEN THE MensaStudio SHALL keep the Current_Position unchanged and display no error or notice.
8. IF the user presses N and the current Photo is the only Photo in the Session without a Folder_Assignment, THEN THE MensaStudio SHALL keep the Current_Position unchanged and display no notice.
9. IF the user presses Right Arrow, Left Arrow, Home, End or N while no Session is open or while the open Session contains no Photos, THEN THE MensaStudio SHALL ignore the key press without displaying an error and without changing any Session state.

### Requirement 7: Assign Destination Folders with the Numpad

**User Story:** As a photographer, I want to press a numpad key to send a photo to a folder and move on, so that I can sort at speed.

#### Acceptance Criteria

1. WHEN the user presses a numpad key from 1 to 9 that is not an Unmapped_Key while NumLock is on and the Session contains at least one Photo, THE MensaStudio SHALL set the Folder_Assignment of the Photo at the Current_Position to the folder name mapped to that key in the Folder_Mapping at the time of the key press.
2. WHEN the MensaStudio sets a Folder_Assignment through a numpad key and the Current_Position is not the last Photo, THE MensaStudio SHALL move the Current_Position to the next Photo.
3. WHEN the user presses numpad 0 while NumLock is on and the Session contains at least one Photo, THE MensaStudio SHALL clear the Folder_Assignment of the Photo at the Current_Position (leaving a Photo without a Folder_Assignment unchanged) and keep the Current_Position unchanged.
4. IF the user presses an Unmapped_Key while NumLock is on, THEN THE MensaStudio SHALL keep the Folder_Assignment, Rating and Current_Position unchanged and display, for at least 2 seconds, a notice that identifies the pressed numpad key and states that the key has no folder mapped.
5. THE Key_Router SHALL route numpad digit keys only to the folder assignment commands of this requirement and top-row digit keys only to the rating commands of Requirement 8, so that a numpad key press never changes a Rating and a top-row digit key press never changes a Folder_Assignment.
6. THE MensaStudio SHALL record the Folder_Assignment as the folder name, independent of the numpad key used, so that a later change of the Folder_Mapping leaves the recorded folder name unchanged.
7. WHEN the user presses a numpad key from 1 to 9 that is not an Unmapped_Key for a Photo that already has a Folder_Assignment, THE MensaStudio SHALL replace the existing Folder_Assignment with the newly mapped folder name.
8. THE NumLock_Indicator SHALL display whether NumLock is on or off, SHALL show the actual NumLock state within 200 ms after the main window is shown, and SHALL update within 200 ms after the NumLock state changes or after the main window regains keyboard focus.
9. WHILE NumLock is off, THE NumLock_Indicator SHALL display a warning that numpad folder keys are inactive, and WHILE NumLock is on, THE NumLock_Indicator SHALL display no such warning.
10. WHEN the MensaStudio sets a Folder_Assignment through a numpad key and the Current_Position is the last Photo, THE MensaStudio SHALL keep the Current_Position on the last Photo.
11. WHILE NumLock is off, THE Key_Router SHALL leave every Folder_Assignment unchanged in response to numpad key presses.
12. IF the user presses a numpad key from 0 to 9 while no Source_Folder is open or the Session contains no Photos, THEN THE MensaStudio SHALL leave the Session and Current_Position unchanged and continue accepting keyboard input.

### Requirement 8: Rate Photos with the Top-Row Keys

**User Story:** As a photographer, I want to give photos a 1–5 rating, so that my best shots are marked in their file names.

#### Acceptance Criteria

1. WHEN the user presses a top-row digit key from 1 to 5 without Ctrl, Alt or Shift held while a Session is open, THE MensaStudio SHALL set the Rating of the Photo at the Current_Position to that digit, regardless of the NumLock state and regardless of whether that Photo is decoded, still decoding or failed to decode.
2. WHEN the user presses the top-row digit key 0 without Ctrl, Alt or Shift held while a Session is open, THE MensaStudio SHALL set the Rating of the Photo at the Current_Position to none, and SHALL leave the Rating as none without displaying an error if the Photo had no Rating.
3. WHEN the MensaStudio sets or clears a Rating, THE MensaStudio SHALL keep the Current_Position and the Folder_Assignment of the current Photo unchanged and SHALL leave the file names of the Photo and its RAW_Sidecars on disk unchanged until an Apply_Plan is executed.
4. WHEN the user presses a top-row digit key from 1 to 5 for a Photo that already has a Rating, THE MensaStudio SHALL replace the existing Rating with the new digit, and SHALL keep the Rating at that digit if the new digit equals the existing Rating.
5. IF the user presses a top-row digit key from 6 to 9, THEN THE MensaStudio SHALL keep the Rating, Folder_Assignment and Current_Position of the current Photo unchanged.
6. IF the user presses a top-row digit key from 0 to 9 while Ctrl, Alt or Shift is held, THEN THE MensaStudio SHALL keep the Rating of the current Photo unchanged.
7. IF the user presses a top-row digit key from 0 to 9 while no Source_Folder is loaded or the loaded Source_Folder contains no Photos, THEN THE MensaStudio SHALL ignore the key press and leave every Session and Session_File unchanged.

### Requirement 9: Rating Prefix Encoding

**User Story:** As a photographer, I want ratings stored in file names in a consistent way, so that ratings survive outside the application and carry over between sessions.

#### Acceptance Criteria

1. WHEN the Rating_Prefix_Codec extracts a Rating from a file name that starts with a Rating_Prefix, THE Rating_Prefix_Codec SHALL return the prefix digit as the Rating and all characters after the first underscore as the unprefixed name, with letter case and extension unchanged (for example `3_IMG_01.JPG` yields Rating 3 and `IMG_01.JPG`, and `3_3_x.jpg` yields Rating 3 and `3_x.jpg`).
2. WHEN the Rating_Prefix_Codec extracts a Rating from a file name that does not match `^[1-5]_`, THE Rating_Prefix_Codec SHALL return no Rating and the full file name, unchanged, as the unprefixed name (for example `10_x.jpg`, `0_x.jpg`, `6_x.jpg`, `3-x.jpg`, `3x.jpg`, `_3_x.jpg` and `IMG_3_x.jpg` yield no Rating).
3. WHEN the Rating_Prefix_Codec applies a Rating from 1 to 5 to a file name, THE Rating_Prefix_Codec SHALL produce the Rating digit, followed by one underscore, followed by the unprefixed name extracted from that file name per criteria 1 and 2 (for example Rating 4 applied to `2_x.jpg` or to `x.jpg` produces `4_x.jpg`).
4. WHEN the Rating_Prefix_Codec applies no Rating to a file name, THE Rating_Prefix_Codec SHALL produce the unprefixed name extracted from that file name per criteria 1 and 2 (for example `2_x.jpg` produces `x.jpg` and `x.jpg` produces `x.jpg`).
5. FOR ALL file names and all Ratings from 1 to 5, applying the Rating and then extracting SHALL return the same Rating and the same unprefixed name as extracting from the original file name (round-trip property).
6. FOR ALL file names and all Ratings from 1 to 5, and FOR ALL file names whose unprefixed name does not start with a Rating_Prefix when no Rating is applied, applying the Rating twice SHALL produce the same file name as applying the Rating once (idempotence property).
7. WHEN a Photo without a Session_File entry is added to a Session, THE MensaStudio SHALL set the Photo's Rating to the Rating that the Rating_Prefix_Codec extracts from the Photo's file name, or to no Rating if the file name does not start with a Rating_Prefix.
8. FOR ALL file names whose unprefixed name does not start with a Rating_Prefix, applying no Rating and then extracting SHALL return no Rating and the same unprefixed name as extracting from the original file name (round-trip property for no Rating).
9. IF a Photo has a Session_File entry whose Rating differs from the Rating extracted from the Photo's file name, THEN THE MensaStudio SHALL use the Session_File entry's Rating for that Photo and ignore the Rating_Prefix.
10. WHEN the MensaStudio sets a Photo's Rating from its Rating_Prefix, THE MensaStudio SHALL leave the Photo's file name on disk unchanged until an Apply is executed.

### Requirement 10: Session File Format

**User Story:** As a photographer, I want my staged decisions stored in a readable text file, so that I can inspect or recover them outside the application.

#### Acceptance Criteria

1. THE Session_File_Printer SHALL write, after the header lines, one entry line per Photo in Session order, each containing exactly three fields separated by single tab characters: the Photo file name, the Folder_Assignment folder name and the Rating digit, with an empty field for an absent Folder_Assignment or absent Rating (for example `IMG_0102.jpg<TAB>Unsure<TAB>` and `IMG_0103.jpg<TAB><TAB>3`).
2. THE Session_File_Printer SHALL write `# MensaStudio session v1` as the first line and `#current=<filename>` as the second line, where `<filename>` is the file name of the Photo at the Current_Position.
3. THE Session_File_Printer SHALL write the Session_File as UTF-8 text without a byte order mark, terminating every line, including the last, with CR LF.
4. WHEN valid Session_File text is provided, THE Session_File_Parser SHALL produce the entry records in the order of their lines and the Current_Position file name taken from the text after `#current=` up to the end of the first line that starts with `#current=` and contains no tab character, or no Current_Position file name if the text contains no such line, accepting lines terminated by CR LF or LF and text with or without a UTF-8 byte order mark.
5. WHEN the Session_File_Parser encounters an empty line, or a line that starts with `#`, contains no tab character and is not a `#current=` line, THE Session_File_Parser SHALL ignore that line.
6. IF a Session_File entry line does not contain exactly three tab-separated fields, has an empty file name field, or has a Rating field other than empty or a single digit from 1 to 5, THEN THE Session_File_Parser SHALL skip that line, report its one-based line number to the caller, and continue parsing the remaining lines.
7. FOR ALL valid Session record sets, printing then parsing then printing SHALL produce identical Session_File text, and printing then parsing SHALL produce an equivalent record set (round-trip property), where a valid Session record set has unique non-empty file names, file names and folder names containing no tab, CR or LF characters, Ratings from 1 to 5 or none, and a Current_Position file name equal to the file name of one of its entries.
8. IF a Session_File entry line has a file name equal, compared case-insensitively, to the file name of an earlier entry line, THEN THE Session_File_Parser SHALL keep the earlier entry, skip the later line and report the later line's one-based line number to the caller.

### Requirement 11: Continuous Session Saving and Resume

**User Story:** As a photographer, I want my progress saved automatically, so that I never lose decisions and can resume where I stopped.

#### Acceptance Criteria

1. WHEN a Folder_Assignment, Rating or Current_Position changes, THE Session_Store SHALL save the full Session, including every change made up to the start of the save, to the Session_File within 1 second after the most recent change.
2. WHEN multiple changes occur with less than 1 second between consecutive changes, THE Session_Store SHALL combine the changes into a single save, and SHALL start a save no later than 5 seconds after the first unsaved change, even if changes are still occurring.
3. THE Session_Store SHALL save by writing the full Session_File content to a temporary file in the Source_Folder and then replacing the Session_File with the temporary file, so that if the write fails the previous Session_File content remains unchanged.
4. WHEN the user closes the MensaStudio main window, THE Session_Store SHALL write every change not yet saved to the Session_File, and wait for any save already in progress to finish, before the application exits.
5. IF saving the Session_File fails, THEN THE Session_Store SHALL display an error indicator in the Status_Bar, keep the Session in memory unchanged, and retry saving the full Session on the next Folder_Assignment, Rating or Current_Position change.
6. WHEN a save succeeds after a failed save, THE Session_Store SHALL remove the error indicator from the Status_Bar.
7. WHEN a Source_Folder containing a Session_File is loaded, THE MensaStudio SHALL parse the Session_File per Requirement 10, merge it with the folder scan per Requirement 12, and restore the Folder_Assignment and Rating of every Session_File entry kept by the merge.
8. WHEN a Session_File is restored and the `#current=` file name matches a Photo in the merged Session, THE MensaStudio SHALL set the Current_Position to that Photo.
9. IF the `#current=` line is absent or names no Photo in the merged Session, THEN THE MensaStudio SHALL set the Current_Position to the first Photo.
10. IF saving the Session_File fails while the user is closing the main window, THEN THE MensaStudio SHALL display an error message containing the Session_File path and the failure reason, and offer the choice to retry the save or exit without saving.
11. IF the Session_File cannot be read, THEN THE MensaStudio SHALL display an error message containing the failure reason and load the Session from the folder scan, with each Rating seeded per Requirement 9.7 and no Folder_Assignments.
12. IF the Session_File_Parser skips one or more Session_File lines during restore, THEN THE MensaStudio SHALL display a notice containing the number of skipped lines and their line numbers.

### Requirement 12: Merge Session File with Folder Contents

**User Story:** As a photographer, I want the session to stay in sync with the folder, so that added or removed files are handled without breaking my session.

#### Acceptance Criteria

1. WHEN a Session_File is restored, THE MensaStudio SHALL add every scanned Photo whose file name matches no Session_File entry, comparing full file names including extension case-insensitively, to the Session with its Rating seeded per Requirement 9.7 and no Folder_Assignment.
2. WHEN a Session_File is restored, THE MensaStudio SHALL drop every Session_File entry whose file name, compared case-insensitively including extension, matches no scanned Photo.
3. WHEN one or more Session_File entries are dropped during a merge, THE MensaStudio SHALL display a notice containing the total number of dropped entries, counting entries dropped under criteria 2 and 7; WHEN zero entries are dropped, THE MensaStudio SHALL display no such notice.
4. WHEN a merge completes, THE MensaStudio SHALL order the merged Session by Natural_Sort_Order of the Photo file names, independent of the entry order in the Session_File.
5. FOR ALL Session_Files and folder scans, the merged Session SHALL contain exactly the scanned Photos, each exactly once, and SHALL identify each Photo by its scanned file name as it exists on disk.
6. FOR ALL Session_File entries whose file name matches a scanned Photo, the merged Session SHALL keep the entry's Folder_Assignment and Rating unchanged, including an empty Folder_Assignment or empty Rating, without seeding the Rating from the file name's Rating_Prefix.
7. IF a Session_File contains two or more entries whose file names match the same scanned Photo case-insensitively, THEN THE MensaStudio SHALL keep the first such entry in Session_File line order and drop the remaining duplicate entries.
8. WHEN a merge adds or drops at least one entry, THE Session_Store SHALL save the merged Session to the Session_File within 1 second after the merge completes.

### Requirement 13: Plan the Apply Operation

**User Story:** As a photographer, I want the application to work out exactly which files will move and how they will be named, so that applying is predictable and never overwrites files.

#### Acceptance Criteria

1. WHEN the Apply_Planner computes the Apply_Plan for a Photo that has a Folder_Assignment, THE Apply_Planner SHALL plan a move of the Photo into the direct subfolder of the Source_Folder whose name equals the Folder_Assignment, whether or not that subfolder exists in the snapshot.
2. WHEN the Apply_Planner computes the Apply_Plan for a Photo that has a Rating and no Folder_Assignment, and the Rating_Prefix_Codec output for that Rating differs from the current file name, THE Apply_Planner SHALL plan an in-place rename of the Photo within the Source_Folder to the Rating_Prefix_Codec output, replacing any existing Rating_Prefix.
3. WHEN the Apply_Planner computes the Apply_Plan for a Photo that has no Rating, has a file name starting with a Rating_Prefix, and has no Folder_Assignment, THE Apply_Planner SHALL plan an in-place rename of the Photo within the Source_Folder to the unprefixed name produced by the Rating_Prefix_Codec.
4. WHEN the Apply_Planner computes the Apply_Plan for a Photo that has a Folder_Assignment, THE Apply_Planner SHALL compute the target file name by applying the Photo's Rating, or no Rating when the Photo has none, through the Rating_Prefix_Codec to the current file name, so that a moved Photo without a Rating has any existing Rating_Prefix removed.
5. WHEN the Apply_Planner computes the Apply_Plan for a Photo that has no Folder_Assignment and whose Rating_Prefix_Codec output equals its current file name by exact character match, THE Apply_Planner SHALL plan no Apply_Operation for that Photo or for its RAW_Sidecars.
6. WHEN a Photo with associated RAW_Sidecars has an Apply_Operation, THE Apply_Planner SHALL include every associated RAW_Sidecar in that Apply_Operation with the same destination folder, Rating_Prefix and Conflict_Suffix as the Photo, computing each RAW_Sidecar target name from the RAW_Sidecar's own current file name so that the RAW_Sidecar keeps its own Base_Name letter case and its own extension including letter case.
7. IF a planned target path of a Photo or of any of its RAW_Sidecars matches, compared case-insensitively, a file in the snapshot (regardless of whether that file is itself moved or renamed by the Apply_Plan) or a target path of an Apply_Operation planned earlier in the same Apply_Plan, THEN THE Apply_Planner SHALL insert into the target names of the Photo and of all its RAW_Sidecars the smallest Conflict_Suffix ` (n)`, with n starting at 1, that makes all of those target paths unique, placed between the unprefixed Base_Name and the extension and leaving any existing ` (n)` text in the Base_Name unchanged (for example `3_IMG_0001.jpg` becomes `3_IMG_0001 (1).jpg`, and `IMG_0001 (1).jpg` becomes `IMG_0001 (1) (1).jpg`).
8. FOR ALL Sessions and file system states, every target path in the Apply_Plan SHALL be distinct from every other target path in the Apply_Plan and from every existing file not moved by the Apply_Plan (uniqueness invariant).
9. FOR ALL Apply_Plans, every Photo and RAW_Sidecar target name SHALL start with the Rating_Prefix of the Photo's Rating when the Photo has a Rating, and SHALL have no Rating_Prefix when the Photo has no Rating.
10. THE Apply_Planner SHALL compute the Apply_Plan from the Session and a snapshot of the file names directly inside the Source_Folder and directly inside every existing subfolder named by a Folder_Assignment, without creating, moving, renaming or deleting any file or folder.
11. THE Apply_Planner SHALL record for each Apply_Operation the operation kind (move or in-place rename), the destination folder name for a move, the source and target path of the Photo, the source and target path of each associated RAW_Sidecar, and whether a Conflict_Suffix was applied.
12. THE Apply_Planner SHALL plan Photos and assign Conflict_Suffixes in Session order, so that when two Photos compete for the same target path, the Photo earlier in Session order receives the target path without a Conflict_Suffix or with the smaller n.
13. FOR ALL Sessions and snapshots, computing the Apply_Plan twice from the same Session and the same snapshot SHALL produce identical Apply_Plans (determinism property).

### Requirement 14: Apply Preview

**User Story:** As a photographer, I want to see a summary before files move, so that I can confirm the result or go back and fix decisions.

#### Acceptance Criteria

1. WHEN the user presses Ctrl+Enter while a Session is open and the Apply_Executor is not running, THE MensaStudio SHALL compute the Apply_Plan from the Session and the file names present in the Source_Folder and its destination subfolders at the time of the key press, and, when the Apply_Plan contains at least one Apply_Operation, SHALL display the Apply_Preview within 2 seconds of the key press for a Session of up to 10,000 Photos on a local drive.
2. THE Apply_Preview SHALL display each destination folder name that is the target of at least one Apply_Operation together with the number of Photos planned to move into it, the total number of RAW_Sidecars included in Apply_Operations (moved or renamed), the number of Photos without a Folder_Assignment, the number of in-place renames and the number of Apply_Operations whose target names contain a Conflict_Suffix, with each count equal to the corresponding count in the computed Apply_Plan.
3. WHEN the user confirms the Apply_Preview, THE MensaStudio SHALL close the Apply_Preview and THE Apply_Executor SHALL execute the exact Apply_Plan summarized in the Apply_Preview without recomputing it.
4. WHEN the user cancels the Apply_Preview by pressing Escape or by closing the Apply_Preview window, THE MensaStudio SHALL close the Apply_Preview, keep every Folder_Assignment, Rating and the Current_Position unchanged, and perform no file system action.
5. IF the computed Apply_Plan contains no Apply_Operations, THEN THE MensaStudio SHALL display a notice stating that there is nothing to apply instead of the Apply_Preview, perform no file system action, and keep the Session unchanged.
6. WHILE the Apply_Preview is displayed, THE Key_Router SHALL route keyboard input to the Apply_Preview, with Enter confirming and Escape cancelling, and SHALL ignore all other keys, including assignment, rating, navigation and Ctrl+Enter commands.
7. IF the existing file names of the Source_Folder or of a destination subfolder cannot be read while computing the Apply_Plan, THEN THE MensaStudio SHALL display an error message containing the folder path and the failure reason, display no Apply_Preview, perform no file system action, and keep the Session unchanged.

### Requirement 15: Execute the Apply Operation

**User Story:** As a photographer, I want the planned moves carried out reliably with a record of what happened, so that I can trust and audit the result.

#### Acceptance Criteria

1. WHEN the Apply_Executor starts executing an Apply_Plan, THE Apply_Executor SHALL create every destination subfolder referenced by the Apply_Plan that does not yet exist, before performing the first move or rename.
2. WHEN the Apply_Executor executes an Apply_Operation, THE Apply_Executor SHALL process the Apply_Operations in Apply_Plan order and, within each Apply_Operation, move or rename the Photo to its planned target path first and then move or rename each associated RAW_Sidecar to its planned target path.
3. WHEN the Apply_Executor performs a file system action (subfolder creation, move or rename), THE Apply_Executor SHALL append exactly one line to the Apply_Log, preserving all existing Apply_Log lines, containing the local date and time to the second, the action type (create folder, move, rename or skip), the full source path, the full target path and the outcome (success, failed with reason, or skipped).
4. IF a move or rename fails, THEN THE Apply_Executor SHALL leave the source file at its original path, log the failure reason to the Apply_Log and continue with the remaining Apply_Operations.
5. IF the move or rename of the Photo of an Apply_Operation fails, THEN THE Apply_Executor SHALL leave every associated RAW_Sidecar of that Apply_Operation at its original path, append one Apply_Log line per RAW_Sidecar with the outcome skipped, and count the Apply_Operation as failed.
6. WHEN the Apply_Executor finishes, THE MensaStudio SHALL display a result summary containing the number of successful Apply_Operations, the number of failed Apply_Operations, and the Photo file name and failure reason of each failed Apply_Operation, where an Apply_Operation counts as successful only if the Photo and every associated RAW_Sidecar reached their planned target paths.
7. WHEN the Apply_Executor finishes, THE Session_Store SHALL rewrite the Session_File to contain exactly one entry for each Photo present in the Source_Folder after execution under its post-execution file name, keeping the Folder_Assignment and Rating of every Photo whose own move or rename failed and the Rating of every Photo renamed in place, and writing the `#current=` line with the post-execution file name of the pre-Apply Current_Position Photo if that Photo remains in the Source_Folder.
8. WHEN the Session_File has been rewritten after an Apply, THE MensaStudio SHALL rescan the Source_Folder, merge the result with the Session_File per Requirement 12, and set the Current_Position per Requirement 11.8 and 11.9.
9. WHILE the Apply_Executor is running, THE Key_Router SHALL ignore assignment, rating, navigation, Apply and folder-open (Ctrl+O, Ctrl+U) commands, and THE MensaStudio SHALL display a progress indicator showing the number of completed Apply_Operations and the total number of Apply_Operations, updated after each completed Apply_Operation.
10. IF a planned target path exists on disk at the moment its move or rename is performed, compared case-insensitively, THEN THE Apply_Executor SHALL leave both the existing file and the source file unchanged and treat that action as failed with a reason indicating that the target already exists.
11. IF creating a destination subfolder fails, THEN THE Apply_Executor SHALL log the failure to the Apply_Log and count every Apply_Operation targeting that subfolder as failed with the subfolder creation failure as reason, without attempting their moves.
12. IF the Apply_Log cannot be opened for appending before the first file system action, THEN THE Apply_Executor SHALL perform no file system action, leave the Session and the Session_File unchanged, and THE MensaStudio SHALL display an error message containing the Apply_Log path and the failure reason.

### Requirement 16: Review the Unsure Folder

**User Story:** As a photographer, I want to revisit photos I was unsure about as their own session, so that I can decide on them later with their ratings intact.

#### Acceptance Criteria

1. WHEN the Settings_Store loads and no saved Folder_Mapping exists, THE Settings_Store SHALL map numpad key 9 to the folder name `Unsure`.
2. WHEN the Apply_Executor executes an Apply_Operation for a Photo whose Folder_Assignment is `Unsure`, THE Apply_Executor SHALL move the Photo and each associated RAW_Sidecar into the Unsure_Folder using the target names planned per Requirement 13, including the Photo's Rating_Prefix and any Conflict_Suffix, and SHALL create the Unsure_Folder first if it does not exist per Requirement 15.1.
3. WHEN the Unsure_Folder is loaded as the Source_Folder, THE MensaStudio SHALL read and write only the Session_File and Apply_Log located directly inside the Unsure_Folder, leaving the parent Source_Folder's Session_File and Apply_Log unread and unmodified for as long as the Unsure_Folder remains the Source_Folder.
4. WHEN the Unsure_Folder is loaded as the Source_Folder and a Photo has no entry in the Unsure_Folder's Session_File, THE MensaStudio SHALL add the Photo with no Folder_Assignment and seed its Rating from the Rating_Prefix of its file name per Requirement 9.7, leaving the Rating empty when the file name has no Rating_Prefix.
5. WHEN the Unsure_Folder is loaded as the Source_Folder and a Photo has an entry in the Unsure_Folder's Session_File, THE MensaStudio SHALL keep that entry's Folder_Assignment and Rating unchanged per Requirement 12.6, without re-seeding the Rating from the Rating_Prefix.
6. WHILE the Unsure_Folder is the Source_Folder, THE Apply_Planner SHALL plan each move into the direct subfolder of the Unsure_Folder named by the Photo's Folder_Assignment, so that a Folder_Assignment of `Unsure` targets the subfolder `Unsure` inside the Unsure_Folder.
7. THE MensaStudio SHALL identify the Unsure_Folder by the subfolder name `Unsure`, compared case-insensitively, regardless of which numpad key, if any, the Folder_Mapping assigns to `Unsure`.

### Requirement 17: Status Bar, Overlay and Legend

**User Story:** As a photographer, I want to see where I am and what I decided for the current photo at a glance, so that I stay oriented through long sessions.

#### Acceptance Criteria

1. WHILE a Session containing at least one Photo is open, THE Status_Bar SHALL display the one-based index of the Current_Position and the total number of Photos in the Session as decimal integers without digit grouping, separated by ` / ` (for example `142 / 1284`).
2. WHILE a Session containing at least one Photo is open, THE Status_Bar SHALL display the complete file name, including its extension, of the Photo at the Current_Position.
3. WHILE a Session containing at least one Photo is open, THE Status_Bar SHALL display three counts over all Photos in the Session: the number of Photos whose Folder_Assignment is not none, the number of Photos whose Folder_Assignment is none, and the number of Photos whose Rating is not none, such that the first two counts sum to the total Photo count.
4. WHILE the Photo at the Current_Position has a Folder_Assignment, THE Overlay SHALL display the assigned folder name exactly as stored in the Folder_Assignment.
5. WHILE the Photo at the Current_Position has a Rating, THE Overlay SHALL display a number of star symbols equal to the Rating, from 1 to 5.
6. WHILE the Photo at the Current_Position has one or more associated RAW_Sidecars, THE Overlay SHALL display a badge listing each RAW_Sidecar extension once, without the leading dot, in upper case, prefixed by `+`, in ascending alphabetical order and separated by a single space (for example `+CR2 +DNG`).
7. THE Legend SHALL display the nine numpad keys in ascending order from 1 to 9, each together with its Folder_Mapping folder name, and SHALL display an indicator stating "unmapped" in place of the folder name for each Unmapped_Key.
8. WHEN a Folder_Assignment, Rating, Current_Position or Folder_Mapping changes, THE Status_Bar, Overlay and Legend SHALL display the updated values within 100 ms of the change.
9. IF the Folder_Assignment of the Photo at the Current_Position names a folder that is not in the current Folder_Mapping, THEN THE Overlay SHALL display that folder name unchanged.
10. IF the Photo at the Current_Position has no Folder_Assignment, no Rating or no associated RAW_Sidecar, THEN THE Overlay SHALL omit the folder name, the star symbols or the RAW badge respectively, and SHALL display no placeholder for the omitted element.
11. WHILE no Session containing at least one Photo is open, THE Status_Bar SHALL display no position, file name or counts, and THE Overlay SHALL display no folder name, star symbols or RAW badge.

### Requirement 18: Help Overlay

**User Story:** As a photographer, I want a quick reference of all shortcuts, so that I can learn the controls without leaving the application.

#### Acceptance Criteria

1. WHEN the MensaStudio starts, THE MensaStudio SHALL display the main window with the Help_Overlay hidden.
2. WHEN the user presses F1 in the main window, THE MensaStudio SHALL toggle the visibility of the Help_Overlay within 100 ms and keep the Current_Position, every Folder_Assignment and every Rating unchanged.
3. THE Help_Overlay SHALL list every keyboard shortcut handled by the Key_Router, showing for each shortcut its key or key combination together with its command description, and SHALL list no key that the Key_Router does not handle.
4. THE Help_Overlay SHALL list the numpad keys 1 to 9 together with their folder names from the current Folder_Mapping, and SHALL mark Unmapped_Keys as unmapped.
5. WHEN the user presses Escape while the Help_Overlay is visible, THE MensaStudio SHALL hide the Help_Overlay within 100 ms and execute no other command for that key press.
6. WHILE the Help_Overlay is visible, THE Key_Router SHALL ignore every key except F1 and Escape, keeping the Current_Position, every Folder_Assignment and every Rating unchanged.
7. WHEN the Folder_Mapping changes, THE Help_Overlay SHALL display the updated folder names the next time it is shown.

### Requirement 19: Folder Mapping Settings

**User Story:** As a photographer, I want to rename the nine folder slots, so that the categories fit each shoot.

#### Acceptance Criteria

1. THE Settings_Store SHALL provide the default Folder_Mapping 1=`Good`, 2=`Okay`, 3=`Bad`, 9=`Unsure`, with keys 4 to 8 as Unmapped_Keys.
2. WHEN the user opens the Settings_Dialog, THE Settings_Dialog SHALL display one editable folder name field for each numpad key from 1 to 9, each pre-filled with the folder name currently mapped to that key, and an empty field for each Unmapped_Key.
3. WHEN the user saves the Settings_Dialog and every folder name field is either empty or passes the validation rules of criterion 4, THE Settings_Store SHALL write the Folder_Mapping to `%AppData%\MensaStudio\settings.json` with each empty field stored as an Unmapped_Key, THE Settings_Dialog SHALL close, and THE Legend SHALL display the updated Folder_Mapping by the time the Settings_Dialog has closed.
4. IF a non-empty folder name entered in the Settings_Dialog contains a character invalid in Windows file names, is `.` or `..`, is a reserved Windows device name, ends with a space or period, is longer than 64 characters, or duplicates another non-empty folder name in the Settings_Dialog compared case-insensitively, THEN THE Settings_Dialog SHALL display a validation message naming the violated rule next to each invalid field, keep the dialog open with all entered values retained, and leave the settings file and the active Folder_Mapping unchanged.
5. WHEN the Folder_Mapping changes, THE MensaStudio SHALL keep every existing Folder_Assignment in the Session unchanged, including Folder_Assignments whose folder name no longer appears in the Folder_Mapping.
6. WHEN the MensaStudio starts, THE Settings_Store SHALL load the Folder_Mapping from `%AppData%\MensaStudio\settings.json`.
7. IF the settings file is absent, THEN THE Settings_Store SHALL use the default Folder_Mapping.
8. IF the settings file cannot be parsed or contains a folder name that fails the validation rules of criterion 4, THEN THE Settings_Store SHALL use the default Folder_Mapping, display a notice stating that the settings were reset to defaults, and leave the existing settings file unmodified until the user next saves the Settings_Dialog.
9. FOR ALL valid Folder_Mappings, saving then loading the settings file SHALL produce an equivalent Folder_Mapping, where equivalent means that each numpad key from 1 to 9 has the identical folder name (exact, case-sensitive match) or is an Unmapped_Key in both (round-trip property).
10. WHEN the user closes the Settings_Dialog without saving, THE Settings_Dialog SHALL discard all edits and THE Settings_Store SHALL keep the active Folder_Mapping and the settings file unchanged.
11. IF the Settings_Store cannot write the settings file during a save, THEN THE Settings_Dialog SHALL display an error message containing the file path and the failure reason, keep the dialog open with all entered values retained, and THE MensaStudio SHALL keep the previously active Folder_Mapping in effect.
12. WHEN the user presses Ctrl+Comma in the main window while no dialog is open and the Apply_Executor is not running, THE MensaStudio SHALL open the Settings_Dialog.

### Requirement 20: Packaging

**User Story:** As the sole user, I want a single executable, so that I can run the application without an installer.

#### Acceptance Criteria

1. THE MensaStudio SHALL target .NET 8 on Windows using WPF.
2. THE MensaStudio SHALL include a `dotnet publish` profile that produces a self-contained, single-file executable for win-x64, such that the publish output contains exactly one executable file and no other files required at runtime (debug symbol files excluded).
3. WHEN the published executable is copied by itself to any writable folder on a Windows 10 or Windows 11 x64 machine that has no .NET runtime installed and is launched, THE MensaStudio SHALL start and display the main window without running an installer, without requesting administrator privileges, and without requiring any additional files next to the executable.
4. WHILE the MensaStudio is running, THE MensaStudio SHALL write persistent data only to `%AppData%\MensaStudio\` and to the Source_Folder and its destination subfolders, and SHALL NOT create registry entries, Start-menu entries, or files next to the executable.
