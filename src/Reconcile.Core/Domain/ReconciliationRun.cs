namespace Reconcile.Core.Domain;

/// <summary>
/// One reconciliation: two imported files, matched record by record.
/// </summary>
public class ReconciliationRun
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string SettlementFileName { get; set; } = "";
    public string DonationFileName { get; set; } = "";

    /// <summary>Import problems, one per line — recorded on the run, never swallowed.</summary>
    public string? ImportNotes { get; set; }

    /// <summary>
    /// The visitor's edits to the sample month, as JSON, when this run came from
    /// "try to break it". Null for plain sample runs and uploads.
    /// </summary>
    public string? Scenario { get; set; }

    public List<SettlementLine> SettlementLines { get; set; } = [];
    public List<DonationRecord> DonationRecords { get; set; } = [];
    public List<MatchResult> MatchResults { get; set; } = [];
}
