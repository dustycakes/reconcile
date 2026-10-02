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

---

## 2026-10-02: Session 5 — a demo that explains itself

**Why:** reviewing the live demo as a stranger would. The front page was a
list of identical runs (the sample uses a fixed seed, by design) with no
explanation. The engine's best decision, refusing to pair one $200 gift with
either of two $200 settlements, looked the same on screen as any missing
record. Status chips showed C# enum names. The report printed both totals but
never explained the $735.00 between them.

**Landed:**

- A front page that says what the tool is for, runs the sample in one click,
  and shows the sample's answer key: each planted defect and where it should
  land. The key lives in the generator, and a test holds it to the generator
  and the engine. Demo mode keeps the newest three runs instead of twelve.
- A note on every finding (`FindingNotes`), written after the rule chain so
  it can see both sides: the ambiguous gift names both settlements, and each
  settlement names the gift and its rival. Third code-first migration,
  `AddResultNote`, adds the column (nvarchar 400; a note lists at most three
  references so a busy day cannot overflow it).
- A bridge on the report (`ReconciliationBridge`): settled gross, less
  settlements the CRM never recorded, plus gifts the processor never settled,
  plus net amount differences, equals the CRM total. For the sample month:
  32,836.00 − 1,335.00 + 393.00 + 207.00 = 32,101.00, unexplained 0.00.
- Plain-language status labels; the mismatch tile shows the net difference
  (+$207.00) instead of the sum of absolute differences; the report's
  percentages count records (229) rather than results (120).
- Screenshots recaptured from the running app.

**What review caught** (the model, while writing the bridge; Dustin's review of
this session is still to come): the report loaded settlement lines and gifts only
through their results, so the bridge's "unexplained" check could never have
failed: a record without a result would not have been loaded at all. The run
query now loads lines and gifts in their own right, and a test confirms the
bridge stops tying out when results go missing. The first wording of the
probable-match note ("the same amount 1 day apart") was rewritten.

**Verified:** 18 tests green (6 new). Migration applied against SQL Server in
Docker; sample run created through the browser in demo mode; run page,
filters and report checked at desktop and phone widths.

---

## 2026-10-02: Session 6 — let the reviewer break it

**Why:** a fixed sample with a fixed answer key cannot tell a working engine
from hard-coded output. The demo needed a way for a stranger to hand it data
nobody prepared.

**Landed:**

- *Try to break it* (`/break`, `Tampering.cs`): five kinds of edit (change a
  gift's amount, delete a gift, delete a settlement, clear a gift's
  reference, post a settlement twice), each aimed at a clean pair the
  visitor picks, applied to a freshly generated month that then goes through
  the ordinary CSV import and engine. The run page shows each edit, the
  outcome predicted for it, and the finding it produced. Edits are stored on
  the run as JSON (fourth migration, `AddRunScenario`); nothing else a
  visitor sends is kept. Demo mode keeps five runs.
- A note for duplicated settlements: the copy is flagged as a likely double
  settlement instead of advising a gift entry.
- A three-step guide on sample runs: the refused guess, the bridge, the
  break-it page.
- *How it was built* (`/built`): each CLAUDE.md rule beside the test that
  holds it, and the dated record of what review and testing caught.

**What testing caught:** the first tampering test found that the
amount-and-date rule, documented as the fallback for gifts with no
reference, also paired gifts that carried a mistyped one. A duplicated $25
settlement was paired with an unrelated $25 gift. The rule now considers
only ref-less gifts, with a test; the sample month's counts did not change.

**Verified:** 25 tests green (7 new). Migration applied against SQL Server in
Docker. In the browser, in demo mode: an edited run caught 5 of 5 edits and
the bridge tied; a repeated gift was refused with a message; at phone width
the pages have no sideways scroll. Dustin's review of sessions 5 and 6 is
still to come.
