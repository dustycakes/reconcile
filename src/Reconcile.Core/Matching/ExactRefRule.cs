using Reconcile.Core.Domain;

namespace Reconcile.Core.Matching;

/// <summary>
/// Highest-confidence rule: the CRM captured the processor's transaction
/// reference at gift entry, and it matches a settlement line exactly.
/// Produces Matched when the amounts agree and AmountMismatch when they
/// don't — a reference match with a wrong amount is a finding, not a pass.
/// </summary>
public class ExactRefRule : IMatchRule
{
    public string Name => "exact-ref";

    public IEnumerable<MatchResult> Claim(
        IReadOnlyList<SettlementLine> unmatchedSettlements,
        IReadOnlyList<DonationRecord> unmatchedDonations)
    {
        // Group donations by captured ref; skip blanks — a blank ref can't
        // assert identity with anything.
        var donationsByRef = unmatchedDonations
            .Where(d => !string.IsNullOrWhiteSpace(d.ProcessorRef))
            .GroupBy(d => d.ProcessorRef!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var claimedDonations = new HashSet<int>();

        foreach (var line in unmatchedSettlements)
        {
            if (!donationsByRef.TryGetValue(line.ProcessorRef.Trim(), out var candidates))
                continue;

            var donation = candidates.FirstOrDefault(d => !claimedDonations.Contains(d.Id));
            if (donation is null)
                continue;

            claimedDonations.Add(donation.Id);
            var delta = donation.Amount - line.Gross;

            yield return new MatchResult
            {
                Status = delta == 0 ? MatchStatus.Matched : MatchStatus.AmountMismatch,
                MatchedBy = Name,
                SettlementLineId = line.Id,
                SettlementLine = line,
                DonationRecordId = donation.Id,
                DonationRecord = donation,
                AmountDelta = delta == 0 ? null : delta,
            };
        }
    }
}
