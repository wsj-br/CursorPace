Create a new release notes file `release-notes/RELEASE_NOTES_<version>.md` for Cursor Pace using the instructions below. The release scripts require this file, and the tag-triggered Release workflow uses it as the GitHub Release description.

**Instructions:**

1. **Read `CursorPace.csproj`** and take `<Version>` (`x.y.z`). Do not bump the version in this step; it must already be the version being released.
2. **Open `dev/CHANGELOG.md`**.
3. **Copy all entries under the `## [Unreleased]` section** up to (but not including) the next `## [` heading (the last released version). If `[Unreleased]` has no bullets, stop and say there is nothing to release.
4. **Format the new file** according to prior notes in `release-notes/RELEASE_NOTES_x.y.z.md`:
   - Title: `# Cursor Pace <version> Release Notes`
   - Sections:
     - `## Highlights` — Summarize the most important user-facing changes from the changelog bullets (features, fixes, major improvements). Do not list every change verbatim; write clear summaries for people who install the app.
     - `## Why this release matters` — One or two sentences on the main impact or reason for this release.
     - `## Detailed Changes` — Do not copy changelog bullets. Point to `dev/CHANGELOG.md` on `master` with a fragment for the version heading (for example `[0.2.0] - 2026-08-30` becomes `#020---2026-08-30`).
     - `---`
     - `## Install` — List the unsigned Windows x64 installer, Linux x64 AppImage, and macOS ARM64/x64 zip files from the GitHub Release. Mention the relevant SmartScreen or Gatekeeper override and that **Sign in** on Windows needs the Microsoft Edge WebView2 Runtime.
     - `---`
     - `## Documentation` — Link QUICKSTART, DEVELOPMENT, and README as in the example below. Use the `master` branch on `https://github.com/wsj-br/CursorPace`.
     - `---`
     - `## License` — Same MIT line as prior notes.
     - Do not include a `### Full Changelog` heading or an `[Unreleased]` section.
5. **Update `dev/CHANGELOG.md`**:
   - Move all lines from `[Unreleased]` to a new section with the current version and today's date (`## [x.y.z] - YYYY-MM-DD`).
   - Leave an empty `[Unreleased]` section at the top for future work.

**Example format for the file:**

```markdown
# Cursor Pace 1.0.1 Release Notes

## Highlights

- Briefly state the most important new features, fixes, or improvements.
- Focus on what most directly affects people using the app.

## Why this release matters

One or two sentences describing the practical impact (for example, "Fixes tray restore after Explorer restarts so the quota calendar stays reachable without relaunching.").

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#101---2026-08-30) for the full list of changes in this release.

---

## Install

Download `CursorPace-1.0.1-win-x64-setup.exe` from this release. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorPace/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorPace/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorPace/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorPace)
```

**Summary:**
Ensure the new release notes file matches prior notes, highlights user-facing changes from the changelog, names the matching installer, and leaves the changelog ready for the next iteration. Write clearly and concisely for GitHub Release readers.
