namespace Reconcile.Core.Domain;

/// <summary>
/// One line from a payment processor's settlement export — what the processor
/// says it collected and deposited. Mock CyberSource-shaped data; no real donors.
/// </summary>
public class SettlementLine
{
    public int Id { get; set; }

    /// <summary>Processor's own transaction reference. The primary join key.</summary>
    public string ProcessorRef { get; set; } = "";

    /// <summary>Gross amount charged to the donor's card.</summary>
    public decimal Gross { get; set; }

    /// <summary>Processing fee withheld before deposit.</summary>
    public decimal Fee { get; set; }

    /// <summary>Net amount deposited (Gross - Fee).</summary>
    public decimal Net { get; set; }

    public DateOnly SettledOn { get; set; }

    /// <summary>Deposit batch the line settled in.</summary>
    public string BatchId { get; set; } = "";

    public int ReconciliationRunId { get; set; }
    public ReconciliationRun? ReconciliationRun { get; set; }
}
