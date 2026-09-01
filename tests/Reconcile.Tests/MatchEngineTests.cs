using Reconcile.Core.Domain;
using Reconcile.Core.Matching;

namespace Reconcile.Tests;

public class MatchEngineTests
{
    private static SettlementLine Line(int id, string procRef, decimal gross, string date = "2026-08-03") => new()
    {
        Id = id, ProcessorRef = procRef, Gross = gross, Fee = Math.Round(gross * 0.022m, 2),
        Net = gross - Math.Round(gross * 0.022m, 2), SettledOn = DateOnly.Parse(date), BatchId = "B-1",
    };

    private static DonationRecord Gift(int id, string? procRef, decimal amount, string date = "2026-08-03") => new()
    {
        Id = id, RecordRef = $"NS-{id}", ProcessorRef = procRef, DonorName = $"Donor {id}",
        Fund = "Unrestricted", Amount = amount, ReceivedOn = DateOnly.Parse(date),
    };

    [Fact]
    public void ExactRef_SameAmount_IsMatched()
    {
        var results = new MatchEngine().Run(
            [Line(1, "TXN-100", 50m)],
            [Gift(1, "TXN-100", 50m)]);

        var r = Assert.Single(results);
        Assert.Equal(MatchStatus.Matched, r.Status);
        Assert.Equal("exact-ref", r.MatchedBy);
        Assert.Null(r.AmountDelta);
    }

    [Fact]
    public void ExactRef_DifferentAmount_IsMismatch_WithDelta()
    {
        var results = new MatchEngine().Run(
            [Line(1, "TXN-100", 50m)],
            [Gift(1, "TXN-100", 500m)]); // keyed with an extra zero

        var r = Assert.Single(results);
        Assert.Equal(MatchStatus.AmountMismatch, r.Status);
        Assert.Equal(450m, r.AmountDelta);
    }

    [Fact]
    public void ExactRef_IsCaseInsensitive_AndTrims()
    {
        var results = new MatchEngine().Run(
            [Line(1, "txn-100", 50m)],
            [Gift(1, "  TXN-100 ", 50m)]);

        Assert.Equal(MatchStatus.Matched, Assert.Single(results).Status);
    }

    [Fact]
    public void BlankRef_NeverExactMatches()
    {
        var results = new MatchEngine(rules: [new ExactRefRule()]).Run(
            [Line(1, "", 50m)],
            [Gift(1, "", 50m)]);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Status == MatchStatus.MissingInCrm);
        Assert.Contains(results, r => r.Status == MatchStatus.MissingInProcessor);
    }

    [Fact]
    public void AmountDate_UniqueCandidate_IsProbable()
    {
        var results = new MatchEngine().Run(
            [Line(1, "TXN-200", 250m, "2026-08-04")],
            [Gift(1, null, 250m, "2026-08-03")]); // phone gift, ref never captured

        var r = Assert.Single(results);
        Assert.Equal(MatchStatus.ProbableMatch, r.Status);
        Assert.Equal("amount-date", r.MatchedBy);
    }

    [Fact]
    public void AmountDate_TwoCandidates_ClaimsNeither()
    {
        // Two $100 settlements the same week; one $100 phone gift. A guess
        // recorded as a match is worse than an honest unmatched.
        var results = new MatchEngine().Run(
            [Line(1, "TXN-1", 100m, "2026-08-03"), Line(2, "TXN-2", 100m, "2026-08-04")],
            [Gift(1, null, 100m, "2026-08-03")]);

        Assert.Equal(3, results.Count);
        Assert.DoesNotContain(results, r => r.Status == MatchStatus.ProbableMatch);
        Assert.Equal(2, results.Count(r => r.Status == MatchStatus.MissingInCrm));
        Assert.Single(results, r => r.Status == MatchStatus.MissingInProcessor);
    }

    [Fact]
    public void AmountDate_OutsideWindow_DoesNotMatch()
    {
        var results = new MatchEngine().Run(
            [Line(1, "TXN-1", 100m, "2026-08-01")],
            [Gift(1, null, 100m, "2026-08-08")]); // 7 days apart, window is 3

        Assert.Equal(2, results.Count);
        Assert.DoesNotContain(results, r => r.Status == MatchStatus.ProbableMatch);
    }

    [Fact]
    public void EveryInputAppearsInExactlyOneResult()
    {
        // The engine's core promise: nothing dropped, nothing double-counted.
        var settlements = new[]
        {
            Line(1, "TXN-1", 50m), Line(2, "TXN-2", 75m), Line(3, "", 20m),
            Line(4, "TXN-4", 100m, "2026-08-02"),
        };
        var donations = new[]
        {
            Gift(1, "TXN-1", 50m), Gift(2, "TXN-2", 750m), Gift(3, null, 100m, "2026-08-03"),
            Gift(4, "TXN-MISSING", 33m), Gift(5, null, 8m),
        };

        var results = new MatchEngine().Run(settlements, donations);

        var lineIds = results.Where(r => r.SettlementLineId.HasValue).Select(r => r.SettlementLineId!.Value).ToList();
        var giftIds = results.Where(r => r.DonationRecordId.HasValue).Select(r => r.DonationRecordId!.Value).ToList();

        Assert.Equal(settlements.Length, lineIds.Count);
        Assert.Equal(settlements.Length, lineIds.Distinct().Count());
        Assert.Equal(donations.Length, giftIds.Count);
        Assert.Equal(donations.Length, giftIds.Distinct().Count());
    }

    [Fact]
    public void StrongRuleRunsBeforeWeakRule()
    {
        // A donation with a captured ref must go to exact-ref even when an
        // amount/date coincidence exists elsewhere in the pool.
        var results = new MatchEngine().Run(
            [Line(1, "TXN-1", 100m, "2026-08-03"), Line(2, "TXN-2", 100m, "2026-08-03")],
            [Gift(1, "TXN-2", 100m, "2026-08-03")]);

        var claimed = Assert.Single(results, r => r.DonationRecordId == 1);
        Assert.Equal("exact-ref", claimed.MatchedBy);
        Assert.Equal(2, claimed.SettlementLineId);
    }
}
