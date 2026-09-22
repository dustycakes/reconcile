using Microsoft.EntityFrameworkCore;
using Reconcile.Core.Domain;

namespace Reconcile.Web.Data;

public class ReconcileDbContext : DbContext
{
    public ReconcileDbContext(DbContextOptions<ReconcileDbContext> options) : base(options) { }

    public DbSet<ReconciliationRun> Runs => Set<ReconciliationRun>();
    public DbSet<SettlementLine> SettlementLines => Set<SettlementLine>();
    public DbSet<DonationRecord> DonationRecords => Set<DonationRecord>();
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SettlementLine>(e =>
        {
            e.Property(p => p.ProcessorRef).HasMaxLength(64);
            e.Property(p => p.BatchId).HasMaxLength(32);
            e.Property(p => p.Gross).HasPrecision(12, 2);
            e.Property(p => p.Fee).HasPrecision(12, 2);
            e.Property(p => p.Net).HasPrecision(12, 2);
            e.HasIndex(p => new { p.ReconciliationRunId, p.ProcessorRef });
        });

        modelBuilder.Entity<DonationRecord>(e =>
        {
            e.Property(p => p.RecordRef).HasMaxLength(64);
            e.Property(p => p.ProcessorRef).HasMaxLength(64);
            e.Property(p => p.DonorName).HasMaxLength(128);
            e.Property(p => p.Fund).HasMaxLength(64);
            e.Property(p => p.Amount).HasPrecision(12, 2);
            e.HasIndex(p => new { p.ReconciliationRunId, p.ProcessorRef });
        });

        modelBuilder.Entity<MatchResult>(e =>
        {
            e.Property(p => p.MatchedBy).HasMaxLength(32);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(24);
            e.Property(p => p.AmountDelta).HasPrecision(12, 2);
            e.HasIndex(p => new { p.ReconciliationRunId, p.Status });

            // A result may reference a line, a record, or both. Restrict (not
            // cascade) on the optional sides so deleting a run cascades once,
            // through the run itself.
            e.HasOne(p => p.SettlementLine).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.DonationRecord).WithMany().OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReconciliationRun>(e =>
        {
            e.Property(p => p.SettlementFileName).HasMaxLength(256);
            e.Property(p => p.DonationFileName).HasMaxLength(256);
        });
    }
}
