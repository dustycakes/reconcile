using Reconcile.Core.Domain;

namespace Reconcile.Core.Matching;

/// <summary>
/// One matching strategy. The engine runs an ordered chain of these
/// (Strategy + Chain of Responsibility): each rule claims the pairs it can
/// defend, and passes the rest along. Order encodes confidence — exact
/// reference matches run before fuzzier amount/date matches, so a weak rule
/// can never steal a pair a strong rule would have claimed.
/// </summary>
public interface IMatchRule
{
    /// <summary>Short stable name recorded on every result this rule produces.</summary>
    string Name { get; }

    /// <summary>
    /// Examine the remaining unmatched pools and return the results this rule
    /// claims. Implementations must not return results that reuse a settlement
    /// line or donation record twice; the engine removes claimed items from
    /// the pools between rules.
    /// </summary>
    IEnumerable<MatchResult> Claim(
        IReadOnlyList<SettlementLine> unmatchedSettlements,
        IReadOnlyList<DonationRecord> unmatchedDonations);
}
