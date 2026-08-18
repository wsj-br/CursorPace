Create a new release notes file `release-notes/RELEASE_NOTES_<version>.md` for Cursor Usage Progress using the instructions below. This file is the `--notes-file` input for `.\scripts\release.ps1`.

**Instructions:**

1. **Read `CursorUsageProgress.csproj`** and take `<Version>` (`x.y.z`). Do not bump the version in this step; it must already be the version being released.
2. **Open `dev/CHANGELOG.md`**.
3. **Copy all entries under the `## [Unreleased]` section** up to (but not including) the next `## [` heading (the last released version). If `[Unreleased]` has no bullets, stop and say there is nothing to release.
4. **Format the new file** according to prior notes in `release-notes/RELEASE_NOTES_x.y.z.md`:
   - Title: `# Cursor Usage Progress <version> Release Notes`
   - Sections:
     - `## Highlights` — Summarize the most important user-facing changes from the changelog bullets (features, fixes, major improvements). Do not list every change verbatim; write clear summaries for people who install the app.
     - `## Why this release matters` — One or two sentences on the main impact or reason for this release.
     - `## Detailed Changes` — The `[Unreleased]` bullets, cleaned up but not rewritten into marketing copy. Keep conventional types (**Added**, **Changed**, **Fixed**, **Removed**) and a short scope.
     - `---`
     - `## Install` — Tell users to download `CursorUsageProgress-<version>-win-x64-setup.exe` from this GitHub Release. Note that the build is unsigned and SmartScreen may require **More info**, then **Run anyway**.
     - `---`
     - `## Documentation` — Link QUICKSTART, DEVELOPMENT, and README as in the example below. Use the `master` branch on `https://github.com/wsj-br/CursorUsageProgress`.
     - `---`
     - `## License` — Same MIT line as prior notes.
     - Do not include a `### Full Changelog` heading or an `[Unreleased]` section.
5. **Update `dev/CHANGELOG.md`**:
   - Move all lines from `[Unreleased]` to a new section with the current version and today's date (`## [x.y.z] - YYYY-MM-DD`).
   - Leave an empty `[Unreleased]` section at the top for future work.

**Example format for the file:**

```markdown
# Cursor Usage Progress 1.0.1 Release Notes

## Highlights

- Briefly state the most important new features, fixes, or improvements.
- Focus on what most directly affects people using the app.

## Why this release matters

One or two sentences describing the practical impact (for example, "Fixes tray restore after Explorer restarts so the quota calendar stays reachable without relaunching.").

## Detailed Changes

- **Fixed**: tray — recreate the notify icon after Explorer restarts.
- **Changed**: calendar — clearer highlight for projected run-out days.

---

## Install

Download `CursorUsageProgress-1.0.1-win-x64-setup.exe` from this release. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**.

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorUsageProgress/blob/master/QUICKSTART.md) — install, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorUsageProgress/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorUsageProgress/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorUsageProgress)
```

**Summary:**
Ensure the new release notes file matches prior notes, highlights user-facing changes from the changelog, names the matching installer, and leaves the changelog ready for the next iteration. Write clearly and concisely for GitHub Release readers.
