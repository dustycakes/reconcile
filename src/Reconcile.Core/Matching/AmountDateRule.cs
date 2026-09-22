using Reconcile.Core.Domain;

namespace Reconcile.Core.Matching;

/// <summary>
/// Fallback rule for donations whose processor reference was never captured
/// (phone and mail gifts keyed by hand). Pairs a donation with a settlement
/// line when the amount matches exactly and the dates sit within a small
/// window — but only when the pairing is unambiguous. If two settlements
/// could explain the same donation, this rule claims neither: a guess
/// recorded as a match is worse than an honest unmatched.
/// </summary>
public class AmountDateRule : IMatchRule
{
    private readonly int _windowDays;

    public AmountDateRule(int windowDays = 3) => _windowDays = windowDays;

    public string Name => "amount-date";

    public IEnumerable<MatchResult> Claim(
        IReadOnlyList<SettlementLine> unmatchedSettlements,
        IReadOnlyList<DonationRecord> unmatchedDonations)
    {
        var claimedLines = new HashSet<int>();

        foreach (var donation in unmatchedDonations)
        {
            var candidates = unmatchedSettlements
                .Where(s => !claimedLines.Contains(s.Id)
                            && s.Gross == donation.Amount
                            && Math.Abs(s.SettledOn.DayNumber - donation.ReceivedOn.DayNumber) <= _windowDays)
                .ToList();

            // Ambiguity is a reason to stop, not to pick.
            if (candidates.Count != 1)
                continue;

            var line = candidates[0];
            claimedLines.Add(line.Id);

            yield return new MatchResult
            {
                Status = MatchStatus.ProbableMatch,
                MatchedBy = Name,
                SettlementLineId = line.Id,
                SettlementLine = line,
                DonationRecordId = donation.Id,
                DonationRecord = donation,
            };
        }
    }
}
