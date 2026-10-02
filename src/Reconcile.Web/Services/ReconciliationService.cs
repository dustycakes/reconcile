using System.Text.Json;
using Reconcile.Core.Domain;
using Reconcile.Core.Import;
using Reconcile.Core.Matching;
using Reconcile.Core.SampleData;
using Reconcile.Web.Data;

namespace Reconcile.Web.Services;

/// <summary>
/// Orchestrates one reconciliation: import both files, persist the raw rows,
/// run the engine over the persisted (id-bearing) entities, persist results.
/// Import problems are recorded on the run itself — visible, never swallowed.
/// </summary>
public class ReconciliationService
{
    private readonly ReconcileDbContext _db;

    public ReconciliationService(ReconcileDbContext db) => _db = db;

    public async Task<ReconciliationRun> RunAsync(
        string settlementFileName, TextReader settlementsCsv,
        string donationFileName, TextReader donationsCsv,
        CancellationToken ct = default, string? scenario = null)
    {
        var settlements = CsvImporter.ReadSettlements(settlementsCsv);
        var donations = CsvImporter.ReadDonations(donationsCsv);

        var notes = settlements.Errors.Select(e => $"settlements: {e}")
            .Concat(donations.Errors.Select(e => $"donations: {e}"))
            .ToList();

        var run = new ReconciliationRun
        {
            CreatedAtUtc = DateTime.UtcNow,
            SettlementFileName = settlementFileName,
            DonationFileName = donationFileName,
            SettlementLines = settlements.Rows,
            DonationRecords = donations.Rows,
            ImportNotes = notes.Count == 0 ? null : string.Join("\n", notes),
            Scenario = scenario,
        };

        _db.Runs.Add(run);
        await _db.SaveChangesAsync(ct); // rows now carry real ids

        var results = new MatchEngine().Run(run.SettlementLines, run.DonationRecords);
        foreach (var r in results) r.ReconciliationRunId = run.Id;
        _db.MatchResults.AddRange(results);
        await _db.SaveChangesAsync(ct);

        return run;
    }

    public async Task<ReconciliationRun> RunSampleAsync(CancellationToken ct = default)
    {
        var (settlementsCsv, donationsCsv) = SampleDataGenerator.ToCsv(SampleDataGenerator.Generate());
        return await RunAsync(
            "sample-settlements.csv", new StringReader(settlementsCsv),
            "sample-donations.csv", new StringReader(donationsCsv), ct);
    }

    /// <summary>
    /// The sample month with a visitor's edits applied. The edited month goes
    /// through the same CSV import as an upload, so nothing is special-cased.
    /// </summary>
    public async Task<ReconciliationRun> RunTamperedAsync(IReadOnlyList<TamperEdit> edits, CancellationToken ct = default)
    {
        var data = SampleDataGenerator.Generate();
        var applied = Tampering.Apply(data, edits);
        var (settlementsCsv, donationsCsv) = SampleDataGenerator.ToCsv(data);
        return await RunAsync(
            "sample-settlements (edited).csv", new StringReader(settlementsCsv),
            "sample-donations (edited).csv", new StringReader(donationsCsv), ct,
            scenario: JsonSerializer.Serialize(applied));
    }

    public static List<TamperEdit> ReadScenario(ReconciliationRun run) =>
        run.Scenario is null ? [] : JsonSerializer.Deserialize<List<TamperEdit>>(run.Scenario) ?? [];
}
