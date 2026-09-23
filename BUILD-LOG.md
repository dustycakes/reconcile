# Build log

A dated record of the sessions that took this project from an empty folder to
a working, tested application in a stack that was new to the person directing
it, and then to a hosted demo.

**How it was built.** Claude Code proposed the project, designed it and wrote
the code. Dustin Mennie picked the target, reviewed what landed and decided
what shipped. The point of the exercise is the workflow, not the domain
expertise: what a directed AI build produces in a stack the director had never
used, and what review catches along the way.

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

---

## 2026-09-23: Session 4 — a demo a stranger can load

**Why:** a repository is a record of a build, not a demo. A reviewer will not
install SQL Server to see an applicant's work. The app already generated its
own sample month, so it could run in public with no uploads at all.

**Landed:**

- Demo mode (`DemoOptions`, bound from configuration): the upload form is
  hidden, `Create` refuses uploads with 403, and after each sample run only the
  newest twelve runs are kept, deleted through the run so the cascade removes
  lines, records and results in one statement.
- Migrations on startup, behind a setting, with a two-minute retry loop while
  SQL Server comes up beside the app. Forwarded-header handling so the app
  honours the tunnel's scheme.
- A multi-stage `Dockerfile` (restore, build, `test` stage, publish, runtime)
  and a `compose.yaml` that runs SQL Server Express with a memory cap next to
  the app, bound to localhost only. The suite ran inside the SDK image on a
  machine with no .NET SDK installed: 12 passed.
- Hosted at reconcile.mennie.dev: an always-on Linux box at home, Docker
  Compose with restart policies, a Cloudflare Tunnel outbound to Cloudflare so
  nothing on the home network is opened inbound. Azure remains the deploy story
  the Bicep describes; it is still not deployed, and the README still says so.

**What review caught:** the exception handler pointed at `/Home/Error`, and no
Home controller exists, so any server error would have produced a 404 instead
of an error page. The handler now points at a real action with a view. The
first draft of the trimming query removed runs by creation time, which the
sample generator sets identically within a second; ordering by id made it
deterministic.

**Verified:** sample runs created through the running container and the
findings page rendered the predicted classification; an upload with a valid
anti-forgery token returned 403 in demo mode.
