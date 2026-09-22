namespace Reconcile.Core.Domain;

/// <summary>
/// One donation as the CRM/ERP recorded it — what the organization believes it
/// received. Mock NetSuite-shaped data; donor names are generated, not real.
/// </summary>
public class DonationRecord
{
    public int Id { get; set; }

    /// <summary>CRM's own record reference (e.g. a NetSuite internal id).</summary>
    public string RecordRef { get; set; } = "";

    /// <summary>
    /// Processor reference as captured at gift entry. Nullable on purpose:
    /// phone and mail gifts get keyed by hand, and sometimes this field is
    /// blank or mistyped. That gap is much of what reconciliation is for.
    /// </summary>
    public string? ProcessorRef { get; set; }

    public string DonorName { get; set; } = "";

    /// <summary>Fund the gift was designated to (e.g. Habitat, Unrestricted).</summary>
    public string Fund { get; set; } = "";

    public decimal Amount { get; set; }

    public DateOnly ReceivedOn { get; set; }

    public int ReconciliationRunId { get; set; }
    public ReconciliationRun? ReconciliationRun { get; set; }
}
