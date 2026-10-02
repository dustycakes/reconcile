using System.Globalization;
using Reconcile.Core.Domain;

namespace Reconcile.Core.SampleData;

public enum TamperKind
{
    /// <summary>Key a different amount on the CRM gift.</summary>
    ChangeGiftAmount,

    /// <summary>Remove the gift from the CRM export.</summary>
    DeleteGift,

    /// <summary>Remove the settlement line from the processor export.</summary>
    DeleteSettlement,

    /// <summary>Clear the processor reference on the CRM gift, as if it were keyed by hand.</summary>
    BlankGiftReference,

    /// <summary>Post the same settlement line twice.</summary>
    DuplicateSettlement,
}

/// <summary>
/// One visitor edit to a clean pair in the sample month. Targets the pair by its
/// processor reference; the record reference and old amount are filled in when
/// the edit is applied, so the run can show where each edit landed.
/// </summary>
public record TamperEdit(TamperKind Kind, string ProcessorRef, decimal? NewAmount = null)
{
    public string? RecordRef { get; init; }
    public decimal? OldAmount { get; init; }

    public string Describe() => Kind switch
    {
        TamperKind.ChangeGiftAmount => $"Changed gift {RecordRef} from {Money(OldAmount)} to {Money(NewAmount)} in the CRM",
        TamperKind.DeleteGift => $"Deleted gift {RecordRef} ({Money(OldAmount)}) from the CRM",
        TamperKind.DeleteSettlement => $"Deleted settlement {ProcessorRef} ({Money(OldAmount)}) from the processor file",
        TamperKind.BlankGiftReference => $"Cleared the processor reference on gift {RecordRef} ({Money(OldAmount)})",
        TamperKind.DuplicateSettlement => $"Posted settlement {ProcessorRef} ({Money(OldAmount)}) twice",
        _ => Kind.ToString(),
    };

    private static string Money(decimal? v) => v is null ? "?" : string.Create(CultureInfo.InvariantCulture, $"${v:N2}");
}

/// <summary>
/// Lets a visitor plant their own defects in the sample month, so the demo is
/// not a recording: the engine meets data nobody prepared for it. Edits apply
/// only to the 100 clean pairs, one edit per pair, on freshly generated data;
/// nothing a visitor types is stored except the edit itself.
/// </summary>
public static class Tampering
{
    public record CleanPair(string ProcessorRef, string RecordRef, string Donor, decimal Amount, DateOnly ReceivedOn);

    /// <summary>The planted-defect-free pairs a visitor may edit, in generation order.</summary>
    public static List<CleanPair> CleanPairs(SampleDataGenerator.Generated data) =>
        data.Donations.Take(SampleDataGenerator.CleanPairCount)
            .Select(d => new CleanPair(d.ProcessorRef!, d.RecordRef, d.DonorName, d.Amount, d.ReceivedOn))
            .ToList();

    /// <summary>What the engine should do with each kind of edit. Shown before and after the run.</summary>
    public static string Predicted(TamperKind kind) => kind switch
    {
        TamperKind.ChangeGiftAmount => "Amount mismatch",
        TamperKind.DeleteGift => "Missing in CRM",
        TamperKind.DeleteSettlement => "Missing at processor",
        TamperKind.BlankGiftReference => "Probable match, or left unpaired if another open settlement fits as well",
        TamperKind.DuplicateSettlement => "One line matched, the copy flagged as a possible double settlement",
        _ => "",
    };

    /// <summary>
    /// Applies the edits to <paramref name="data"/> in place and returns them with
    /// record references and old amounts filled in. Throws <see cref="ArgumentException"/>
    /// with a message fit for the visitor when an edit can't be applied.
    /// </summary>
    public static List<TamperEdit> Apply(SampleDataGenerator.Generated data, IReadOnlyList<TamperEdit> edits)
    {
        if (edits.Count == 0)
            throw new ArgumentException("Pick at least one edit.");

        var pairs = CleanPairs(data).ToDictionary(p => p.ProcessorRef, StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var applied = new List<TamperEdit>();

        foreach (var edit in edits)
        {
            if (!pairs.TryGetValue(edit.ProcessorRef, out var pair))
                throw new ArgumentException($"{edit.ProcessorRef} is not one of the sample's clean gifts.");
            if (!seen.Add(pair.ProcessorRef))
                throw new ArgumentException($"{pair.ProcessorRef} is picked twice. Use a different gift for each edit.");

            var line = data.Settlements.Single(s => s.ProcessorRef == pair.ProcessorRef);
            var gift = data.Donations.Single(d => d.RecordRef == pair.RecordRef);

            switch (edit.Kind)
            {
                case TamperKind.ChangeGiftAmount:
                    if (edit.NewAmount is not { } amount || amount <= 0 || amount > 1_000_000)
                        throw new ArgumentException("A changed amount must be between $0.01 and $1,000,000.");
                    if (Math.Round(amount, 2) == gift.Amount)
                        throw new ArgumentException($"{amount:N2} is the amount {pair.RecordRef} already has. Pick a different one.");
                    gift.Amount = Math.Round(amount, 2);
                    break;
                case TamperKind.DeleteGift:
                    data.Donations.Remove(gift);
                    break;
                case TamperKind.DeleteSettlement:
                    data.Settlements.Remove(line);
                    break;
                case TamperKind.BlankGiftReference:
                    gift.ProcessorRef = null;
                    break;
                case TamperKind.DuplicateSettlement:
                    data.Settlements.Add(new SettlementLine
                    {
                        ProcessorRef = line.ProcessorRef, Gross = line.Gross, Fee = line.Fee, Net = line.Net,
                        SettledOn = line.SettledOn, BatchId = line.BatchId,
                    });
                    break;
                default:
                    throw new ArgumentException("Unknown edit.");
            }

            applied.Add(edit with { ProcessorRef = pair.ProcessorRef, RecordRef = pair.RecordRef, OldAmount = pair.Amount });
        }

        return applied;
    }
}
