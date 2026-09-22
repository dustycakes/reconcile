# Build log

A dated record of three sessions that took this project from an empty folder to
a working, tested application in a stack that was new to the person directing
it.

**How it was built.** Claude Code wrote the code. Dustin Mennie set the design
constraints, reviewed every change before it landed, and decided what shipped.
This log records what each session produced and what review caught, so the
claims made about the project can be checked against the work.

**Starting position, 2026-09-01.** No C# had been written by the author before
this project; the working stack was TypeScript / React / Next.js / PostgreSQL,
learned during 2026 building production tools for a manufacturing plant. The
machine had no .NET SDK, no Docker and no SQL Server when session 1 began.

---

## 2026-09-01: Session 1 — domain, engine, schema

**Landed:**

- Toolchain from nothing: .NET 10 SDK (Microsoft's install script, because the
  Homebrew cask wanted root), colima + Docker CLI, SQL Server 2022 in a
  container, verified with `sqlcmd`. `dotnet ef` required `DOTNET_ROOT` pointed
  at the home-directory install.
- Solution scaffold: `Reconcile.Core` (pure domain), `Reconcile.Web`
  (ASP.NET Core MVC), `Reconcile.Tests` (xUnit).
- The domain: settlement lines, donation records, runs, match results.
- The matching engine: an ordered chain of match rules. Exact reference first,
  then an amount+date fallback that refuses to guess when two candidates could
  explain the same gift. Engine invariant: every imported record lands in one
  result and only one.
- Nine unit tests on the engine's promises. Green on first run.
- EF Core code-first: DbContext with explicit money precision and enum-as-string
  storage, `InitialSchema` migration generated and applied against the real SQL
  Server container. Four tables plus migrations history, verified by querying
  `sys.tables`.

**Design constraints set at direction, not by the model:** match-rule ordering
as a statement of confidence; ambiguity resolving to unmatched rather than a
best guess; a mismatched amount on a matched reference counting as a finding
rather than a failure. These come from reconciliation work on real revenue data
in a manufacturing job, where a guess recorded as a match is how trust in the
numbers erodes.

**Next:** import and run pipeline in the web app, findings UI, sample data
generator, printable report.

---

## 2026-09-01: Session 2 — the working application

**Landed:**

- Sample-data generator: a month of activity with planted, labeled defects
  (keying errors, uncaptured refs, unrecorded web gifts, one engineered
  ambiguity) and a fixed seed so every screenshot reproduces.
- A test that predicts how the engine must classify the generated month: 100
  matched, 6 probable, 3 mismatched, 6 missing in CRM, 5 missing at processor.
  It passed on the first run, and the running app shows the same numbers, so
  generator, engine and UI agree end to end.
- Web pipeline: ReconciliationService (import → persist → match → persist),
  second code-first migration (`AddImportNotes`: import problems are stored on
  the run and shown, never swallowed).
- MVC UI, server-rendered with one small hand-written stylesheet and ~20 lines
  of vanilla JS: runs list, upload page with a one-click sample month, findings
  dashboard where every tile filters to the specific records behind it, and a
  print-ready reconciliation report with totals, match summary, itemized
  exceptions and sign-off lines.
- Verified in the browser against live SQL Server: sample run created, tiles
  filtered, report rendered.

**What review caught:** deleting the MVC template's `Models/` folder broke
`_ViewImports.cshtml`, which still imported the namespace — ten identical
compile errors, fixed with one line. Caught because a build runs before any
change is believed.

**Next:** README with screenshots, a Bicep file for the Azure deploy story,
repo polish.

---

## 2026-09-01: Session 3 — presentable repository

**Landed:**

- README with real screenshots (captured headless from the running app against
  SQL Server, so they match what `dotnet run` shows), a two-minute quickstart,
  architecture notes, and a "what this is not" section.
- `infra/main.bicep`: App Service plan + Linux web app on .NET 10 + Azure SQL
  server and database, connection string injected as an app setting so the app
  runs unchanged from local Docker SQL Server to Azure SQL. Compiles clean under
  Bicep CLI 0.46. Not deployed to a live subscription, and the file says so.
- GitHub Actions CI: restore, build, test on every push and PR. MIT license.
  `.editorconfig` matching the conventions in CLAUDE.md.

**What review caught:** the hero screenshot rendered negative deltas as
`$-225.00`, a C# format string placing the currency symbol before a signed
number. Fixed to `−$225.00`, app restarted, screenshots recaptured. The first
draft of the Bicep header also claimed validation with a tool that had not been
run; the Bicep CLI was installed, the template compiled (zero diagnostics), and
the claim rewritten to match what actually happened. A claim in a comment gets
the same review as the code.

**Totals:** three sessions, from no SDK to a working, tested, documented
application, with two code-first migrations applied against real SQL Server and
every commit reviewed.
