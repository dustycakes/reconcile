# Build log

A dated record of what it cost to build this in a stack I had never touched,
using an AI-assisted workflow with review. I direct and review; Claude Code
writes most of the code. The log documents what that workflow produces and
what review catches, so the claims in my application can be checked against
a real record.

Dustin Mennie

---

## 2026-09-01: Hour zero

Starting position: I have never written a line of C#. My stack is
TypeScript / React / Next.js / PostgreSQL, all of it learned this year
building production floor tools for the plant where I work as Material
Manager. This machine had no .NET SDK, no Docker, and no SQL Server when the
session started.

**Session 1 (~2 hours), what landed:**

- Toolchain from nothing: .NET 10 SDK (Microsoft's install script, because
  the Homebrew cask wanted root), colima + Docker CLI, SQL Server 2022 in a
  container, verified with `sqlcmd`.
- Solution scaffold: `Reconcile.Core` (pure domain), `Reconcile.Web`
  (ASP.NET Core MVC), `Reconcile.Tests` (xUnit).
- The domain: settlement lines, donation records, runs, match results.
- The matching engine: an ordered chain of match rules. Exact reference
  first, then an amount+date fallback that refuses to guess when two
  candidates could explain the same gift. Engine invariant: every imported
  record lands in one result and only one.
- Nine unit tests on the engine's promises. Green on first run.
- EF Core code-first: DbContext with explicit money precision and enum-as-
  string storage, `InitialSchema` migration generated and applied against the
  real SQL Server container. Four tables plus migrations history, verified by
  querying `sys.tables`.

**What I noticed crossing stacks:** almost everything transferred. Entities,
migrations, DI, routing: the concepts are the ones I already use, and the
syntax and tooling are new. C# reads like TypeScript with the option types
welded shut. The one new muscle so far is the Microsoft tooling itself
(`dotnet ef` needed `DOTNET_ROOT` pointed at a home-dir install, the kind of
thing you learn once).

**Review notes for this session:** design decisions I made (not the AI):
match-rule ordering as a confidence statement; ambiguity resolving to
unmatched instead of a best guess; a mismatched amount on a matched reference
counting as a finding, not a failure. These come from reconciliation work on
real revenue data in my day job, where a guess recorded as a match is how
trust in the numbers erodes.

Next session: import + run pipeline in the web app, the findings UI (every
number drills to its records), sample data generator, printable report.

---

## 2026-09-01: Session 2 (~1.5 hours)

**What landed:** the whole working application.

- Sample-data generator: a month of activity with planted, labeled defects
  (keying errors, uncaptured refs, unrecorded web gifts, one engineered
  ambiguity) and a fixed seed so every screenshot reproduces.
- A test that predicts how the engine must classify the generated month: 100
  matched, 6 probable, 3 mismatched, 6 missing in CRM, 5 missing at
  processor. It passed on the first run, and the running app shows the same
  numbers, so generator, engine and UI agree end to end.
- Web pipeline: ReconciliationService (import → persist → match → persist),
  second code-first migration (`AddImportNotes`: import problems are stored
  on the run and shown, never swallowed).
- MVC UI, server-rendered with one small hand-written stylesheet and ~20
  lines of vanilla JS: runs list, upload page with a one-click sample month,
  findings dashboard where every tile filters to the specific records behind
  it, and a print-ready reconciliation report with totals, match summary,
  itemized exceptions, and sign-off lines.
- Verified in the browser against live SQL Server: sample run created, tiles
  filtered, report rendered.

**What review caught this session:** deleting the MVC template's `Models/`
folder broke `_ViewImports.cshtml`, which still imported the namespace. Ten
identical compile errors, fixed with one line. Small, and the reason I build
before I believe a change worked.

**Stack-crossing note:** Razor views are JSX's older cousin; EF's
`Include/ThenInclude` is an ORM eager-load like any other; tag helpers took
ten minutes to stop fighting. Nothing today was new in the way session 1's
tooling was.

Next session: README with screenshots, a Bicep file for the Azure deploy
story, repo polish, then the application materials.

---

## 2026-09-01: Session 3 (~1 hour)

**What landed:** the repo became presentable.

- README with real screenshots (captured headless from the running app
  against SQL Server, so they match what `dotnet run` shows), a two-minute
  quickstart, architecture notes, and a "what this is not" section.
- `infra/main.bicep`: App Service plan + Linux web app on .NET 10 + Azure SQL
  server and database, connection string injected as an app setting so the
  app runs unchanged from local Docker SQL Server to Azure SQL. Compiles clean
  under Bicep CLI 0.46. Not yet deployed to a live subscription, and the file
  says so.
- GitHub Actions CI: restore, build, test on every push and PR. MIT license.
  `.editorconfig` matching the conventions in CLAUDE.md.

**What review caught this session:** the hero screenshot. Negative deltas
rendered as `$-225.00`, a C# format string that put the currency symbol
before a signed number. Fixed to `−$225.00`, app restarted, screenshots
recaptured. The first draft of the Bicep header also claimed validation with
a tool I hadn't run yet. I installed the Bicep CLI, compiled the template
(zero diagnostics), and rewrote the claim to match what happened. A claim in
a comment gets the same review as the code.

**Totals so far:** three sessions, roughly 4.5 hours, from no SDK to a
working, tested, documented application in a stack I had never used, with
two code-first migrations applied against real SQL Server and every commit
reviewed.

Next: application materials.
