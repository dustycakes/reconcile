namespace Reconcile.Web;

/// <summary>
/// Settings for a public demo deployment. Bound from the "Reconcile" configuration
/// section (environment: Reconcile__DemoMode=true, Reconcile__MigrateOnStartup=true).
/// </summary>
public sealed class DemoOptions
{
    public const string Section = "Reconcile";

    /// <summary>Sample runs only: the CSV upload form is hidden and uploads are refused.</summary>
    public bool DemoMode { get; set; }

    /// <summary>Apply pending EF migrations when the app starts, retrying while SQL Server comes up.</summary>
    public bool MigrateOnStartup { get; set; }

    /// <summary>In demo mode, keep only this many most recent runs.</summary>
    public int KeepRuns { get; set; } = 12;
}
