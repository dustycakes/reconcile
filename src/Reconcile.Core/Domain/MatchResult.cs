namespace Reconcile.Core.Domain;

public enum MatchStatus
{
    /// <summary>Both systems agree: same reference, same amount.</summary>
    Matched,

    /// <summary>Reference matched but the amounts differ.</summary>
    AmountMismatch,

    /// <summary>No exact reference, but amount + date point at one candidate.</summary>
    ProbableMatch,

    /// <summary>The processor settled money the CRM never recorded.</summary>
    MissingInCrm,

    /// <summary>The CRM recorded a gift the processor never settled.</summary>
    MissingInProcessor,
}

/// <summary>
/// The unit of output. Every settlement line and every donation record ends up
/// in exactly one MatchResult — nothing is dropped, and unmatched items are
/// first-class results, not leftovers. A count you can't act on is not
/// information, so each result names the records behind it.
/// </summary>
public class MatchResult
{
    public int Id { get; set; }

    public MatchStatus Status { get; set; }

    /// <summary>Which rule produced this result (e.g. "exact-ref", "amount-date").</summary>
    public string MatchedBy { get; set; } = "";

    public int? SettlementLineId { get; set; }
    public SettlementLine? SettlementLine { get; set; }

    public int? DonationRecordId { get; set; }
    public DonationRecord? DonationRecord { get; set; }

    /// <summary>Donation amount minus settlement gross, when both sides exist.</summary>
    public decimal? AmountDelta { get; set; }

    public int ReconciliationRunId { get; set; }
    public ReconciliationRun? ReconciliationRun { get; set; }
}
