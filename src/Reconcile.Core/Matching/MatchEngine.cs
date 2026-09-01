using Reconcile.Core.Domain;

namespace Reconcile.Core.Matching;

/// <summary>
/// Runs the rule chain over the two imported files and accounts for every
/// record on both sides. The invariant this engine exists to keep: nothing is
/// dropped. Every settlement line and every donation record appears in exactly
/// one result, so the output can be audited against the inputs by count alone.
/// </summary>
public class MatchEngine
{
    private readonly IReadOnlyList<IMatchRule> _rules;

    public MatchEngine(IEnumerable<IMatchRule>? rules = null)
    {
        _rules = rules?.ToList() ?? [new ExactRefRule(), new AmountDateRule()];
    }

    public List<MatchResult> Run(
        IEnumerable<SettlementLine> settlements,
        IEnumerable<DonationRecord> donations)
    {
        var remainingSettlements = settlements.ToList();
        var remainingDonations = donations.ToList();
        var results = new List<MatchResult>();

        foreach (var rule in _rules)
        {
            var claimed = rule.Claim(remainingSettlements, remainingDonations).ToList();
            results.AddRange(claimed);

            var claimedLineIds = claimed.Where(r => r.SettlementLineId.HasValue)
                                        .Select(r => r.SettlementLineId!.Value).ToHashSet();
            var claimedDonationIds = claimed.Where(r => r.DonationRecordId.HasValue)
                                            .Select(r => r.DonationRecordId!.Value).ToHashSet();

            remainingSettlements.RemoveAll(s => claimedLineIds.Contains(s.Id));
            remainingDonations.RemoveAll(d => claimedDonationIds.Contains(d.Id));
        }

        // Whatever no rule could defend becomes a finding in its own right.
        results.AddRange(remainingSettlements.Select(s => new MatchResult
        {
            Status = MatchStatus.MissingInCrm,
            MatchedBy = "unmatched",
            SettlementLineId = s.Id,
            SettlementLine = s,
        }));

        results.AddRange(remainingDonations.Select(d => new MatchResult
        {
            Status = MatchStatus.MissingInProcessor,
            MatchedBy = "unmatched",
            DonationRecordId = d.Id,
            DonationRecord = d,
        }));

        return results;
    }
}
