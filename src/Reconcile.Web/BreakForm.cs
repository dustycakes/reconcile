using Reconcile.Core.SampleData;

namespace Reconcile.Web;

/// <summary>
/// The "try to break it" form: one optional edit of each kind, each aimed at a
/// clean pair of the visitor's choosing. Unchecked rows are ignored.
/// </summary>
public sealed class BreakForm
{
    public bool ChangeAmount { get; set; }
    public string? ChangeAmountRef { get; set; }
    public decimal? NewAmount { get; set; }

    public bool DeleteGift { get; set; }
    public string? DeleteGiftRef { get; set; }

    public bool DeleteSettlement { get; set; }
    public string? DeleteSettlementRef { get; set; }

    public bool BlankReference { get; set; }
    public string? BlankReferenceRef { get; set; }

    public bool Duplicate { get; set; }
    public string? DuplicateRef { get; set; }

    /// <summary>The clean pairs offered in each picker. Not bound from the form.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public List<Tampering.CleanPair> Pairs { get; set; } = [];

    /// <summary>Every edit switched on, each on a different random gift; the amount gets an extra zero, a classic keying slip.</summary>
    public static BreakForm Suggested(List<Tampering.CleanPair> pairs)
    {
        var pick = pairs.OrderBy(_ => Random.Shared.Next()).Take(5).ToList();
        return new BreakForm
        {
            Pairs = pairs,
            ChangeAmount = true, ChangeAmountRef = pick[0].ProcessorRef, NewAmount = pick[0].Amount * 10,
            DeleteGift = true, DeleteGiftRef = pick[1].ProcessorRef,
            DeleteSettlement = true, DeleteSettlementRef = pick[2].ProcessorRef,
            BlankReference = true, BlankReferenceRef = pick[3].ProcessorRef,
            Duplicate = true, DuplicateRef = pick[4].ProcessorRef,
        };
    }

    public List<TamperEdit> ToEdits()
    {
        var edits = new List<TamperEdit>();
        if (ChangeAmount) edits.Add(new(TamperKind.ChangeGiftAmount, ChangeAmountRef ?? "", NewAmount));
        if (DeleteGift) edits.Add(new(TamperKind.DeleteGift, DeleteGiftRef ?? ""));
        if (DeleteSettlement) edits.Add(new(TamperKind.DeleteSettlement, DeleteSettlementRef ?? ""));
        if (BlankReference) edits.Add(new(TamperKind.BlankGiftReference, BlankReferenceRef ?? ""));
        if (Duplicate) edits.Add(new(TamperKind.DuplicateSettlement, DuplicateRef ?? ""));
        return edits;
    }
}
