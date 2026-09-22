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

    public List<SettlementLine> SettlementLines { get; set; } = [];
    public List<DonationRecord> DonationRecords { get; set; } = [];
    public List<MatchResult> MatchResults { get; set; } = [];
}
