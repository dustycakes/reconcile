using Reconcile.Core.Domain;

namespace Reconcile.Web;

/// <summary>What a reviewer reads. The enum names stay in CSS classes and data attributes.</summary>
public static class StatusLabels
{
    public static string Label(this MatchStatus status) => status switch
    {
        MatchStatus.Matched => "Matched",
        MatchStatus.ProbableMatch => "Probable match",
        MatchStatus.AmountMismatch => "Amount mismatch",
        MatchStatus.MissingInCrm => "Missing in CRM",
        MatchStatus.MissingInProcessor => "Missing at processor",
        _ => status.ToString(),
    };
}
