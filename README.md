# DocumentDeploy

A Windows desktop app for teachers that tells you, at the right moment, which
documents you need for what's happening today - and asks you to file the
completed ones back in the right place.

**It never stores your documents.** Only metadata about them: their name,
where to find them, where a completed copy should be filed, and whether
that's happened yet. The documents themselves - and anything to do with
pupils - never leave your machine; there's no cloud sync, no server, no
network calls at all.

## What it does

- **Morning brief.** Open it in the morning and it shows your whole day:
  every lesson, duty, and meeting, with the documents each one needs.
- **Time-and-lesson aware.** Open it again at 1pm and it shows what's
  relevant right now instead of the whole day again. It knows your
  timetable, so it never pops up over a lesson, duty, or meeting - only
  during your own free time.
- **Proactive, not just on-demand.** It runs quietly in the system tray and
  pops itself up automatically: shortly before something starts (so you can
  grab what you need), right after something with a document due back ends,
  and for the morning brief - then gets out of the way again.
- **Give it a document back.** When something needs to come back (e.g. a
  signed form after a 1-on-1), drag the completed file onto the app, or
  browse for it, and it copies it into the folder you've set up for that
  document. **It only ever copies** - your original file is never moved,
  edited, or deleted, wherever it came from.
- **Never forgets.** Anything you miss stays outstanding and keeps
  resurfacing - in the next morning brief, the next free moment, and on the
  weekly planning screen - until you deal with it.
- **Friday (or any day) planning.** A dedicated weekly planner: generate the
  week from your recurring timetable in one click, then add one-off items
  (meetings, trips) on top.
- **Templates with two kinds of questions.** Define a "Phonics Lesson"
  template once with its usual documents plus custom questions, each tagged
  as either a **planning** question (e.g. "Today's sound" - answered in the
  weekly planner, all in one place, right after generating the week) or a
  **completion** question (e.g. "How did it go" - answered from the
  dashboard once the lesson is actually over). Completion answers are saved
  and shown back to you as a recap in the planner the next time you plan,
  so you can see how last week went before deciding this week's plan.
- **Repeating one-off items.** Adding something in the weekly planner (a
  child's 1-on-1, say) offers a "repeat this every week" option, which
  quietly turns it into a proper recurring timetable slot - no need to set
  it up twice.
- **Duties and your own breaks count too.** Break, lunchtime and afternoon
  duty block popups just like a lesson does (you're supervising, not free);
  your own personal breaks don't, since that's exactly when a reminder is
  useful.
- **Bulk editing.** Export your timetable or document library to CSV, edit
  in Excel, and re-import - handy for setting up a new term in one go.
- **Move your setup to another computer.** Settings has an "Export all
  setup" button that bundles your timetable, document templates, session
  templates and settings into a single file - build everything at home,
  carry the file over (USB stick, email to yourself, OneDrive), and
  "Import setup" on the other machine. It never includes already-planned
  weeks, filed documents, or answered questions - only the setup itself,
  so it's safe to use on a machine that's already in daily use too.

## Installing

Download the latest `DocumentDeploy-*-win-x64.zip` from the
[Releases page](../../releases), unzip it anywhere, and run `DocumentDeploy.exe`.
It's a single self-contained executable - no .NET install required, and it
doesn't touch anything outside its own folder and its own data folder
(`%AppData%\DocumentDeploy`).

On first run it registers itself to start automatically with Windows (you
can turn this off in Settings) since it's meant to run quietly in the tray
all day. Look for its icon in the system tray - double-click it, or right
click for Open / Plan next week / Settings / Exit.

## Getting started

1. Open **Timetable** and add your recurring weekly slots (lessons, duties,
   your own breaks).
2. Open **Templates** and set up the documents you regularly need - where to
   find them, whether a completed copy needs to come back and where it
   should be filed. Add a **session template** for anything with recurring
   custom questions (like the phonics example above), and attach it to the
   relevant timetable slot.
3. Open **Plan Week**, click **Generate from timetable**, and add anything
   one-off for the week (a specific child's 1-on-1, a trip).
4. Leave it running in the tray. It'll bring itself up when there's
   something to prep for or a document to hand back.

## Project layout

```
src/
  DocumentDeploy.Core/   Platform-agnostic logic: models, the scheduling
                         engine, week generation, file filing, JSON
                         storage, CSV import/export. No Windows
                         dependency, so this half is unit-tested on any OS.
  DocumentDeploy.App/    The WPF desktop app: tray icon, background
                         scheduler, and every window.
  DocumentDeploy.Tests/  xunit tests for DocumentDeploy.Core.
```

## Building it yourself

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
and Windows (WPF only runs on Windows, though the solution will *compile* -
not run - on other platforms too, which is how this project's own tests are
validated in CI on Linux).

```
dotnet test src/DocumentDeploy.Tests/DocumentDeploy.Tests.csproj
dotnet build DocumentDeploy.sln
```

To produce a single-file exe like the one attached to Releases:

```
dotnet publish src/DocumentDeploy.App/DocumentDeploy.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

## CI and releases

- **`.github/workflows/ci.yml`** runs on every push and pull request: the
  Core test suite (fast, on Linux) and a full Windows build of the whole
  app, to catch anything before it ever reaches a release.
- **`.github/workflows/release.yml`** only runs when a version tag (e.g.
  `v1.0.0`) is pushed - not on every commit - and publishes the
  self-contained Windows exe as a GitHub Release. This repo is public, so
  none of this counts against any GitHub Actions minutes limit.

## A note on the "file it back" feature

The app copies whatever file you drag or browse to into the destination
folder configured for that document - it creates the folder if needed, and
if a file with the same name is already there, the copy is renamed rather
than overwriting it. It never reads, moves, or deletes the original file,
and it never opens or inspects the contents of any document.
