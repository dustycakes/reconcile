using Reconcile.Core.Domain;

namespace Reconcile.Core.Reporting;

/// <summary>
/// Walks the processor's settled gross to the CRM's recorded total, one kind of
/// finding at a time. The two totals come straight from the imported rows and
/// the adjustments from the results, so <see cref="Unexplained"/> is a real
/// check: if the engine ever dropped or double-counted a record, the bridge
/// would stop tying out to zero.
/// </summary>
public record ReconciliationBridge(
    decimal SettledGross,
    decimal SettledNotInCrm,
    decimal RecordedNotSettled,
    decimal NetAmountDifferences,
    decimal CrmRecorded)
{
    /// <summary>Where the settled gross lands after every finding is applied.</summary>
    public decimal Explained => SettledGross - SettledNotInCrm + RecordedNotSettled + NetAmountDifferences;

    /// <summary>CRM total minus the explained total. Zero when every dollar is accounted for.</summary>
    public decimal Unexplained => CrmRecorded - Explained;

    public static ReconciliationBridge From(
        IEnumerable<SettlementLine> lines,
        IEnumerable<DonationRecord> gifts,
        IEnumerable<MatchResult> results)
    {
        var list = results.ToList();
        return new ReconciliationBridge(
            SettledGross: lines.Sum(l => l.Gross),
            SettledNotInCrm: list.Where(r => r.Status == MatchStatus.MissingInCrm).Sum(r => r.SettlementLine!.Gross),
            RecordedNotSettled: list.Where(r => r.Status == MatchStatus.MissingInProcessor).Sum(r => r.DonationRecord!.Amount),
            NetAmountDifferences: list.Sum(r => r.AmountDelta ?? 0m),
            CrmRecorded: gifts.Sum(g => g.Amount));
    }
}
