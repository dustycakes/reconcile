using System.Globalization;
using Reconcile.Core.Domain;

namespace Reconcile.Core.Matching;

/// <summary>
/// Explains each finding in words a reviewer can act on. Runs after the rule
/// chain, over the final results, so a note can see the whole picture: an
/// unpaired gift can name the settlements that made it ambiguous, and those
/// settlements can name the gift. A finding without a reason is a count, and a
/// count you can't act on is not information.
/// </summary>
internal static class FindingNotes
{
    public static void Annotate(List<MatchResult> results, int windowDays)
    {
        var openLines = results.Where(r => r.Status == MatchStatus.MissingInCrm)
            .Select(r => r.SettlementLine!).ToList();
        var openGifts = results.Where(r => r.Status == MatchStatus.MissingInProcessor)
            .Select(r => r.DonationRecord!).ToList();

        bool Fits(SettlementLine s, DonationRecord d) =>
            s.Gross == d.Amount && Math.Abs(s.SettledOn.DayNumber - d.ReceivedOn.DayNumber) <= windowDays;

        foreach (var r in results)
        {
            var s = r.SettlementLine;
            var d = r.DonationRecord;

            r.Note = r.Status switch
            {
                MatchStatus.Matched => null,

                MatchStatus.ProbableMatch =>
                    $"No reference keyed on the gift. One settlement has the same amount, {Days(s!, d!)}, " +
                    $"so they are paired pending review. Confirm it, then key {s!.ProcessorRef} on {d!.RecordRef}.",

                MatchStatus.AmountMismatch =>
                    $"Same reference, different amounts: the processor settled {Money(s!.Gross)}, " +
                    $"the CRM recorded {Money(d!.Amount)}. Check the gift entry against the charge.",

                MatchStatus.MissingInCrm => MissingInCrm(s!),

                MatchStatus.MissingInProcessor => MissingAtProcessor(d!),

                _ => null,
            };
        }

        string MissingInCrm(SettlementLine s)
        {
            var rival = openGifts.FirstOrDefault(d => IsBlank(d.ProcessorRef) && Fits(s, d));
            if (rival is not null)
            {
                var others = openLines.Where(l => l.Id != s.Id && Fits(l, rival)).Select(l => l.ProcessorRef).ToList();
                if (others.Count > 0)
                    return $"Not paired on purpose. Gift {rival.RecordRef} ({Money(rival.Amount)}) fits this settlement " +
                           $"and {Refs(others)} equally well, so neither was claimed. Decide which by hand.";
            }
            return $"Settled in batch {s.BatchId} with no gift in the CRM. Enter the gift with reference {s.ProcessorRef}.";
        }

        string MissingAtProcessor(DonationRecord d)
        {
            if (!IsBlank(d.ProcessorRef))
                return $"Reference {d.ProcessorRef!.Trim()} is not in this settlement file. " +
                       "Check the gift entry for a mistyped reference, or look for it in the next period.";

            var candidates = openLines.Where(l => Fits(l, d)).Select(l => l.ProcessorRef).ToList();
            return candidates.Count switch
            {
                0 => $"No reference keyed, and nothing settled for {Money(d.Amount)} within {windowDays} days. " +
                     "Likely a check or cash gift: confirm it against the bank deposit.",
                1 => $"No reference keyed. {candidates[0]} fits by amount and date, but no rule paired them. Review by hand.",
                _ => $"Not paired on purpose. {candidates.Count} settlements fit this gift by amount and date " +
                     $"({Refs(candidates)}). Picking one would be a guess; decide by hand and key its reference.",
            };
        }
    }

    /// <summary>At most three refs, then a count, so a busy day can't overflow the 400-character column.</summary>
    private static string Refs(IReadOnlyList<string> refs) =>
        refs.Count <= 3 ? string.Join(", ", refs) : $"{string.Join(", ", refs.Take(3))} and {refs.Count - 3} more";

    private static bool IsBlank(string? s) => string.IsNullOrWhiteSpace(s);

    private static string Days(SettlementLine s, DonationRecord d)
    {
        var gap = Math.Abs(s.SettledOn.DayNumber - d.ReceivedOn.DayNumber);
        return gap switch
        {
            0 => "on the gift date",
            1 => "1 day from the gift date",
            _ => $"{gap} days from the gift date",
        };
    }

    private static string Money(decimal amount) => string.Create(CultureInfo.InvariantCulture, $"${amount:N2}");
}
