# BibleFamilyTreeBuilder

BibleFamilyTreeBuilder is a local C# WPF desktop app for building one large biblical family tree. It lets the user edit people and relationships as data, then automatically lays out the visible tree by generation so every card does not have to be manually moved.

The first version stores everything in readable local JSON. There is no login, cloud storage, collaboration, or database.

## Why It Exists

Biblical family trees often include multiple spouses, adopted or legal parent relationships, unknown people, unnamed descendant chains, and grouped people such as "700 wives". General-purpose diagram tools can show these, but they usually make the user manually arrange every card. This app keeps the genealogy as editable data and creates a simple visual layout from that data.

## Data Model

The project has one `TreeProject` with:

- `People`: all person cards in the large tree.
- `Relationships`: parent-child and marriage connections between people.
- `GenerationLabels`: optional row labels for biblical time periods or notes.

Each `Person` stores:

- `Id`
- `Name`
- `DisplayName`
- `AlsoKnownAs`
- `NameVariants`
- `CardType`
- `Notes`
- `BibleReferences`
- `GenerationOverride`
- `ManualXOffset`
- `ManualYOffset`

Each `Relationship` stores:

- `Id`
- `Type`
- `FromPersonId`
- `ToPersonId`
- `Label`
- `DisplayLabel`
- `ParentKind`
- `Notes`
- `BibleReferences`
- `EvidenceLevel`

Each `GenerationLabel` stores:

- `GenerationNumber`
- `Title`
- `Notes`

Each `PersonNameVariant` stores:

- `Id`
- `NameUsed`
- `Book`
- `Reference`
- `Notes`
- `TranslationOrSource`

Relationship `EvidenceLevel` values are:

- `Direct`: directly stated in a biblical reference.
- `Inferred`: reasonably inferred from the text.
- `Traditional`: based on tradition or common interpretation.
- `Uncertain`: possible but not certain.
- `Unknown`: not enough evidence.

## Card Types

- `Default`: green person card.
- `JesusLine`: yellow/gold card selected manually by the user.
- `Unknown`: muted grey/blue unknown person placeholder.
- `UnknownDescendant`: blue person-style card for an unknown descendant chain.
- `GroupedPeople`: distinct purple grouped card such as "700 wives".

Cards use rounded corners, spacing, subtle shadows, and a stronger outline for the selected person.

## Unknown Descendants

Unknown descendant chains are not a separate relationship type in this MVP. They are represented as blue `Person` cards with `CardType.UnknownDescendant`, then connected using normal `ParentChild` relationships. This keeps the layout code and saved JSON easier to understand.

## Name Variants And Bible Source References

Some biblical people appear under different names, spellings, or forms in different books and references. `NameVariants` let one person record store those source-specific names without creating duplicate people.

For example, one person card can keep the clean display name `Solomon`, while the details can store a variant such as `Jedidiah` with `2 Samuel 12:25`.

The selected-person panel includes `Name Variants / Book References`, where the user can:

- View all variants for the selected person.
- Add a variant with `NameUsed`, `Book`, `Reference`, `Notes`, and optional `TranslationOrSource`.
- Edit the selected variant and save changes.
- Delete only the selected variant.

Name variants are details on the person record. Deleting a variant does not delete the person, and adding a variant should be used instead of creating a second person card for the same biblical person.

The main tree card stays clean and continues to show only the person's `DisplayName` or `Name`. Name variants appear in the selected-person details and are preserved in JSON save/load.

## Merge People

The toolbar includes `Merge People` for cases where two person records were created separately but later turn out to represent the same biblical person. This can happen when alternate spellings, source-specific names, or uncertain identities are entered before the user realizes they should be one person.

Merge People is different from name variants:

- Use `NameVariants` when one existing person needs book-specific names, spellings, references, or notes.
- Use `Merge People` when two separate person cards should become one person record.

The merge dialog lets the user choose:

- The primary person to keep.
- The secondary person to merge into the primary person.

The selected person is preselected as the primary person when possible. Before merging, the dialog previews moved relationships, combined aliases, combined Bible references, combined name variants, notes behavior, card type behavior, generation override behavior, and manual offset behavior.

When merged:

- The primary person's `Id` is kept.
- The secondary person is removed.
- Relationships pointing to the secondary person are moved to the primary person.
- Duplicate relationships and self-relationships caused by the merge are removed.
- `AlsoKnownAs`, `BibleReferences`, and `NameVariants` are combined without obvious duplicates.
- Secondary notes are copied or appended so useful notes are not silently lost.
- `JesusLine` is preserved if either person had it.
- The primary person's generation override and manual offsets are preferred. Secondary values are copied only when the primary person does not already have them.

The app asks for confirmation before merging because the merge cannot be undone unless the project was backed up or has not been saved yet. Save a backup copy before large merges.

## Relationship Evidence And Bible References

Relationships can store their own evidence, separate from the people they connect. This helps distinguish links such as biological parent, legal parent, adopted parent, unknown parent-child, marriage, inferred connection, or uncertain tradition.

For a selected relationship, the left panel lets the user edit:

- `DisplayLabel`: a short label such as `Mother`, `Legal father`, or `Betrothed`.
- `ParentKind`: editable after creation for parent-child relationships.
- `EvidenceLevel`: `Direct`, `Inferred`, `Traditional`, `Uncertain`, or `Unknown`.
- `BibleReferences`: one or more references that support the relationship.
- `Notes`: explanation of why the relationship was added or how strong the evidence is.

Only `DisplayLabel` appears on the canvas and in PNG exports, and only when it is not empty. Longer notes and Bible references stay in the selected-person panel so the visual tree does not become cluttered.

## Manual Generation Override

Parent-child relationships create automatic generation rows. Marriage relationships try to keep spouses on the same row.

`GenerationOverride` is applied after the automatic calculation. This allows the user to place someone on the same row as another biblical figure when the exact ancestry is missing, but the user believes the people lived around the same period.

## Generation Labels And Time Period Notes

Generation labels are optional labels for an entire generation row. For example, generation `12` can be labeled `Time of David`, with notes explaining that people manually placed there are believed to have lived around David's period.

Generation labels are different from `GenerationOverride`:

- `GenerationOverride` belongs to one person and changes which row that person appears on.
- `GenerationLabels` belong to a row and describe that row for everyone shown there.

The selected-person panel shows the selected person's effective generation. It also shows whether that person is using automatic generation placement or has a manual generation override active. The same panel lets the user add, edit, or clear the title and notes for the selected person's current generation row.

Generation notes are kept in the edit panel and saved to JSON. The canvas only shows the short row title as `Generation N - Title`, so longer biblical context does not clutter the tree.

This is useful when exact ancestry or timing is uncertain, or when the user intentionally places people on the same row because they lived around the same biblical period.

## Generation Labels Manager

The toolbar includes `Manage Generations`, which opens a simple manager for all generation/time-period labels in the project.

The manager shows:

- Generation number.
- Title.
- Notes.
- Number of people currently laid out on that generation row.

From the manager, the user can add a new generation label, edit an existing label, or delete a label. Deleting a generation label only removes that row's title and notes. It does not delete any people, relationships, `GenerationOverride` values, or manual nudges.

The manager prevents duplicate generation-label entries for the same generation number. If a label already exists for a generation, the app asks the user to edit the existing label instead.

After labels are added, edited, or deleted, the canvas refreshes so lane labels and exports use the latest generation names.

## Manual Nudging

`ManualXOffset` and `ManualYOffset` are applied after automatic layout. The nudge buttons adjust those offsets for the selected person. `Clear Manual Position` resets both offsets to zero.

This means the same person appears only once in the tree, while the user still has enough control to resolve difficult biblical layout cases.

## Visual Layout

The tree canvas draws subtle horizontal generation lanes behind the cards. Each lane is labeled on the left as `Generation 0`, `Generation 1`, and so on. These labels come from the same auto-layout calculation as the cards, so they update when relationships, generation overrides, or manual layout changes are refreshed.

If a generation row has a custom title, the canvas label changes to `Generation N - Title`.

Parent-child relationships use neutral connector lines. Marriage relationships use red/pink horizontal connectors. Adopted or legal parent relationships use dashed parent-child lines so they remain visually distinct from biological parent links.

The canvas includes a small legend that explains the main colors and connector styles.

## Search And Navigation

The left panel includes a search box for people. Search matches `Name`, `DisplayName`, and `AlsoKnownAs` values. Selecting a search result selects that person in the editor and highlights the matching card on the canvas.

The toolbar includes:

- `Center Selected`: pans the canvas toward the selected person.
- `Fit Tree to View`: adjusts zoom and pan so the current tree fits into the visible canvas area.

Search also checks `BibleReferences`, name variant fields, and relationship evidence fields. Searching for `Jedidiah` can find the person whose card is displayed as `Solomon`, and searching for `Matthew` can find people with Matthew-specific variants, person references, or relationship references.

When a search match comes from a relationship, the result selects one of the connected people. If the currently selected person is connected to that matching relationship, the app keeps that person in the results; otherwise it uses the relationship's `FromPerson`.

Search continues to use the full people list. If the app is currently showing an ancestor or descendant view and the user selects a search result outside that visible lineage, the app automatically returns to `Full Tree` and selects that person.

## Lineage Views

The toolbar includes display-only lineage views:

- `View Ancestors`: shows the selected person and their parent chain, including biological, legal, adopted, and unknown parent-child links.
- `View Descendants`: shows the selected person and their children, grandchildren, and further descendants.
- `View Ancestors + Descendants`: shows the selected person's local lineage in both directions.
- `Return to Full Tree`: clears the lineage filter and shows the full project again.

The current view mode is shown in the toolbar as `Full Tree`, `Ancestors of [Name]`, `Descendants of [Name]`, or `Lineage of [Name]`.

Lineage views only affect what is drawn on the canvas. They do not delete people, delete relationships, change card types, change generation overrides, change manual offsets, or alter the saved JSON data. Auto Layout operates on the currently visible filtered tree, and returning to `Full Tree` restores the normal full-tree drawing.

Spouses are included in lineage views when they are directly connected to people in the visible lineage, so marriage context remains visible without expanding into unrelated branches.

## Image Export

The toolbar includes two PNG export options:

- `Export Current View as PNG`: captures the current tree canvas viewport, including the current pan and zoom. This is useful for sharing exactly what is on screen.
- `Export Full Tree as PNG`: exports the full drawn tree area for the current canvas project, independent of pan and zoom. It adds padding around the exported tree so cards, lane labels, the header, and the legend are not cut off.

Both exports include visible person cards, parent-child lines, marriage connectors, generation lanes, custom generation labels, and short relationship labels when `DisplayLabel` is set. They do not include the left edit panel or the main window toolbar.

Full-tree export also includes a clean background, a small `Bible Family Tree` header, the current view mode such as `Full Tree` or `Ancestors of Jesus`, and a legend explaining card colors and relationship line styles.

In lineage view, export uses the current filtered lineage view. For example, `Export Full Tree as PNG` exports the full ancestor/descendant view that is currently drawn, not the original unfiltered project. Use `Return to Full Tree` first if you want to export the full unfiltered project.

Current export limitations:

- PNG export does not include an editable vector format.
- Current-view export captures only the visible viewport.
- Very large trees can create large image files.

## Selected Person Relationship Summary

When a person is selected, the left panel shows a read-only summary of that person's:

- Parents
- Spouses
- Children

The summary is calculated from the existing relationship data and does not create or edit relationships.

## Faster Editing Workflow

The selected-person panel includes quick add buttons:

- `Add Child`: creates a new person and connects the selected person as parent.
- `Add Parent`: creates a new person and connects that new person as parent of the selected person.
- `Add Spouse`: creates a new person and adds a marriage relationship.
- `Add Unknown Descendant`: creates a blue `UnknownDescendant` card and connects it as a child.
- `Add Grouped People`: creates a `GroupedPeople` card and connects it as a marriage-style grouped relationship when possible.

These shortcuts are meant to make large tree building faster while still using the same underlying `Person` and `Relationship` data.

## Relationship List, Evidence Editing, And Deletion

The left panel also shows a relationship list for the selected person. Each item shows the category, relationship type, connected people, `ParentKind` when applicable, `EvidenceLevel`, and short label when present.

After selecting a relationship, the user can edit its short display label, parent kind, evidence level, Bible references, and notes. `Save Relationship Changes` stores the edits and refreshes the canvas if the short label changed.

You can select a relationship in that list and delete only that relationship. The app asks for confirmation first, and deleting a relationship does not delete either person.

## Duplicate Name Warning

When a new person is created, the app checks whether another person already uses the same `Name`, `DisplayName`, `AlsoKnownAs`, or `NameVariants.NameUsed`. Because biblical names repeat, this warning does not block creation. It simply gives the user a chance to continue or cancel.

## Check Tree Report

The toolbar includes `Check Tree`, which shows a friendly report with:

- Number of people.
- Number of relationships.
- Number of custom generation labels.
- Number of people with name variants.
- Total number of name variant entries.
- Number of relationships with Bible references.
- People with no relationships.
- Relationships pointing to missing people.
- Duplicate person or relationship IDs.
- Possible duplicate people by name, alias, or name variant, with a reminder to use `Merge People` if they are the same person.
- Name variants missing `NameUsed`.
- Name variants missing `Book`.
- Relationships missing evidence level.
- Relationships marked `Uncertain`.
- Relationships with notes.
- Relationships with `DisplayLabel`.
- Parent-child relationships with `ParentKind.Unknown`.
- List of custom generation labels.
- People with `GenerationOverride` set.
- People with manual X or Y offsets.

## Safer Relationship Creation

Relationship creation now checks for common mistakes before adding a relationship:

- Missing people.
- A person married to themselves.
- A person as their own parent or child.
- Duplicate identical relationships.

When a relationship cannot be created, the app shows a friendly message instead of crashing.

## Project Safety, Saving, And Backups

The app tracks the current project file during the session. After a successful save or load, the window title shows the file name, for example `Bible Family Tree Builder - my-tree.json`.

When the project has unsaved changes, the title shows an asterisk:

`Bible Family Tree Builder - my-tree.json *`

Unsaved changes are tracked for edits that affect saved JSON data, including people, relationships, notes, Bible references, name variants, generation labels, generation overrides, manual nudges, and merges.

Before starting a new sample tree, loading another project, or closing the app, BibleFamilyTreeBuilder asks for confirmation if there are unsaved changes.

The toolbar includes:

- `Save Project`: saves to the current file if one is known. If not, it asks where to save.
- `Save As Project`: saves the current project under a new file name and makes that the current file.
- `Create Backup`: creates a JSON backup without clearing the unsaved-changes indicator.
- `Open Backups Folder`: opens the backups folder in Windows Explorer when one exists.

If the project has a current file path, backups are stored next to that project file in a `backups` folder:

`backups/bible-family-tree-backup-yyyy-MM-dd-HHmm.json`

If the project has not been saved yet, `Create Backup` asks the user to choose where to save the backup.

Before destructive actions, the app automatically tries to create a backup when the project has a current file path. This happens before deleting a person, deleting a relationship, or merging people. If the project has not been saved yet, the app explains that no automatic backup could be created and continues with the normal confirmation flow.

To restore a backup manually, use `Load Project` and choose the backup JSON file from the `backups` folder.

## Safe Delete And File Loading

Deleting a person asks for confirmation first. If the person has connected relationships, those relationships are removed at the same time so the project does not keep broken links.

Invalid or unreadable project JSON is handled with a friendly error message instead of crashing the app. Valid project JSON with missing optional arrays is normalized when it loads.

## Included MVP Features

- Local WPF desktop app.
- One large tree project.
- Editable people.
- Editable parent-child relationships.
- Editable marriage relationships.
- Multiple spouses through multiple marriage relationships.
- Biological, adopted, legal, and unknown parent kinds.
- Unknown person cards.
- Unknown descendant blue cards.
- Grouped people cards.
- Manually selected Jesus Line cards.
- Bible references and notes stored on the person and shown in the edit panel when selected.
- Name variants for book-specific names, spellings, references, notes, and source labels.
- Relationship labels, Bible references, notes, parent-kind editing, and evidence levels.
- Deterministic generation-based auto layout.
- Generation lane labels.
- Custom generation/time-period labels with notes.
- Generation label manager for browsing, adding, editing, and deleting row labels.
- Legend for card and relationship styles.
- Search by name, display name, and alternate names.
- Center selected person.
- Fit tree to visible canvas.
- Ancestor, descendant, and full lineage canvas views.
- Export current view or full current tree view as PNG.
- Selected-person relationship summary.
- Quick add buttons for child, parent, spouse, unknown descendants, and grouped people.
- Relationship list and relationship deletion.
- Duplicate name warning.
- Merge People workflow for safely combining duplicate person records.
- Check Tree report.
- Current project file tracking and unsaved-changes title indicator.
- Save, Save As, manual backups, and backups folder access.
- Automatic backup attempt before deleting people, deleting relationships, or merging people.
- Safer relationship creation.
- Safe delete confirmation.
- Manual generation override.
- Manual position nudging.
- Pan and zoom on the tree canvas.
- Save and load readable indented JSON.
- Built-in sample project.

## Developer Handover

See `HANDOVER.md` for developer/project-maintenance details, including the folder structure, model/service responsibilities, safety behavior, known limitations, and testing checklist.

## Running From Visual Studio

1. Open `BibleFamilyTreeBuilder.sln`.
2. Set `BibleFamilyTreeBuilder.App` as the startup project if Visual Studio does not select it automatically.
3. Press `F5` to run with debugging, or `Ctrl+F5` to run without debugging.

## Running From The Command Line

From the repository root:

```powershell
dotnet build BibleFamilyTreeBuilder.sln
dotnet run --project src\BibleFamilyTreeBuilder.App\BibleFamilyTreeBuilder.App.csproj
```

## Features To Add Later

- More advanced biblical timeline tools.
- Search and filtering.
- GitHub backup instructions.
- Better graph layout.
- Optional export to PDF.
- Optional vector export for very large trees.
