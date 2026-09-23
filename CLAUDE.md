# Reconcile — AI context and conventions

Reconcile matches a payment processor's settlement export against a CRM's
donation records, record by record, and reports every mismatch as a specific
fixable item. Built as a working demonstration of AI-assisted development in
the .NET stack: this file is the context an AI coding session loads before
touching the code, and the conventions human review holds that code to.

## What this is (and isn't)

- A small, honest line-of-business app: import two CSVs, reconcile, drill into
  findings, print a report.
- All data is generated. There are no real donors, no real transactions, and
  no data from any employer, anywhere in this repo. Keep it that way.
- Built by Dustin Mennie directing Claude Code, with every change reviewed
  before it lands. The division of labor is documented in BUILD-LOG.md and is
  part of what this repo demonstrates.

## Architecture

```
src/Reconcile.Core   pure C# domain + matching engine + CSV import. No EF, no
                     web. Everything here is unit-testable with plain objects.
src/Reconcile.Web    ASP.NET Core MVC + EF Core (SQL Server, code-first
                     migrations). Owns persistence and presentation.
tests/               xUnit. The matching engine's promises live here.
```

The matching engine is an ordered chain of `IMatchRule` strategies
(Strategy + Chain of Responsibility). Order encodes confidence: exact
reference matches run before amount/date matches, so a weak rule can never
claim a pair a strong rule would have. The engine's invariant — every imported
record appears in exactly one result — is enforced by test
(`EveryInputAppearsInExactlyOneResult`) and must survive every change.

## Conventions

- **Nothing is dropped silently.** Import errors are collected and shown, not
  skipped. Ambiguous matches are left unmatched, never guessed. If a change
  would trade this for convenience, stop and flag it.
- **Findings are records, not counts.** Any surface that reports a problem
  links to the specific rows behind it. A number you can't act on is not
  information.
- **Money is `decimal` with explicit precision** (12,2 in the schema). Never
  float. Deltas are donation-side minus processor-side, consistently.
- **Migrations:** EF code-first, one migration per schema change, reviewed
  before `database update`. Never edit an applied migration; add a new one.
- **Tests before UI.** The engine changed? The tests changed with it in the
  same commit, and `dotnet test` is green before anything else proceeds.
- **C# style:** file-scoped namespaces, nullable enabled, expression-bodied
  members where they read better, XML doc comments on public types that carry
  design intent, not restatements of signatures.
- **Front-end:** server-rendered MVC views, vanilla JavaScript only, no SPA
  framework. Progressive enhancement; every page works with scripting off.

## Commands

```
dotnet build                                  # build everything
dotnet test                                   # engine + import tests
dotnet run --project src/Reconcile.Web        # run the app
dotnet ef migrations add <Name> --project src/Reconcile.Web
dotnet ef database update --project src/Reconcile.Web
```

Local SQL Server runs in Docker:

```
docker run -d --name reconcile-sql -e ACCEPT_EULA=Y \
  -e "MSSQL_SA_PASSWORD=Reconcile!Dev2026" -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

Dev connection string lives in `appsettings.Development.json`. The password
above is a local throwaway for a disposable container; nothing real is behind
it.

Container route (`Dockerfile` + `compose.yaml`): `docker compose up -d` builds
the app image, starts SQL Server Express beside it and applies migrations on
start (`Reconcile__MigrateOnStartup`). `docker build --target test .` runs the
suite inside the SDK image. `Reconcile__DemoMode=true` (compose default) hides
the upload form, refuses uploads with 403 and keeps only the newest runs; the
public demo at reconcile.mennie.dev runs this way. `DemoOptions.cs` holds the
settings.

## Review discipline

AI-generated code ships only after a human pass that asks, at minimum: does
this uphold the two invariants (nothing dropped, findings are records)? Do the
tests actually test the change, or just pass alongside it? Would this
migration survive being run twice? The build log records what review caught.
