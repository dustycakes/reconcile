using Reconcile.Core.Domain;
using Reconcile.Core.Matching;
using Reconcile.Core.Reporting;
using Reconcile.Core.SampleData;

namespace Reconcile.Tests;

public class NotesAndBridgeTests
{
    private static SettlementLine Line(int id, string procRef, decimal gross, string date = "2026-08-03") => new()
    {
        Id = id, ProcessorRef = procRef, Gross = gross, SettledOn = DateOnly.Parse(date), BatchId = "DEP-1",
    };

    private static DonationRecord Gift(int id, string? procRef, decimal amount, string date = "2026-08-03") => new()
    {
        Id = id, RecordRef = $"NS-{id}", ProcessorRef = procRef, DonorName = $"Donor {id}",
        Fund = "Unrestricted", Amount = amount, ReceivedOn = DateOnly.Parse(date),
    };

    private static List<MatchResult> SampleResults(out SampleDataGenerator.Generated data)
    {
        data = SampleDataGenerator.Generate();
        for (var i = 0; i < data.Settlements.Count; i++) data.Settlements[i].Id = i + 1;
        for (var i = 0; i < data.Donations.Count; i++) data.Donations[i].Id = i + 1;
        return new MatchEngine().Run(data.Settlements, data.Donations);
    }

    [Fact]
    public void EveryFinding_SaysWhy_AndCleanMatchesStayQuiet()
    {
        var results = SampleResults(out _);

        Assert.All(results.Where(r => r.Status != MatchStatus.Matched), r => Assert.False(string.IsNullOrWhiteSpace(r.Note)));
        Assert.All(results.Where(r => r.Status == MatchStatus.Matched), r => Assert.Null(r.Note));
    }

    [Fact]
    public void AmbiguousGift_NamesBothSettlements_AndEachSettlementNamesTheGift()
    {
        var results = new MatchEngine().Run(
            [Line(1, "TXN-1", 100m, "2026-08-03"), Line(2, "TXN-2", 100m, "2026-08-04")],
            [Gift(7, null, 100m, "2026-08-03")]);

        var gift = Assert.Single(results, r => r.Status == MatchStatus.MissingInProcessor);
        Assert.Contains("Not paired on purpose", gift.Note);
        Assert.Contains("TXN-1", gift.Note);
        Assert.Contains("TXN-2", gift.Note);

        var one = Assert.Single(results, r => r.SettlementLineId == 1);
        Assert.Contains("NS-7", one.Note);
        Assert.Contains("TXN-2", one.Note);
    }

    [Fact]
    public void MistypedReference_IsCalledOut_AsAReference_NotAMissingGift()
    {
        var results = new MatchEngine().Run([], [Gift(1, "TXN-9342", 25m)]);

        var r = Assert.Single(results);
        Assert.Contains("TXN-9342 is not in this settlement file", r.Note);
    }

    [Fact]
    public void Bridge_TiesSettledGrossToCrmTotal_ToTheCent()
    {
        var results = SampleResults(out var data);

        var bridge = ReconciliationBridge.From(data.Settlements, data.Donations, results);

        // The sample month: 32,836.00 settled - 1,335.00 never recorded
        // + 393.00 recorded but never settled + 207.00 net keying errors = 32,101.00.
        Assert.Equal(32_836.00m, bridge.SettledGross);
        Assert.Equal(1_335.00m, bridge.SettledNotInCrm);
        Assert.Equal(393.00m, bridge.RecordedNotSettled);
        Assert.Equal(207.00m, bridge.NetAmountDifferences);
        Assert.Equal(32_101.00m, bridge.CrmRecorded);
        Assert.Equal(0m, bridge.Unexplained);
    }

    [Fact]
    public void Bridge_StopsTyingOut_WhenAResultGoesMissing()
    {
        var results = SampleResults(out var data);
        results.RemoveAll(r => r.Status == MatchStatus.MissingInCrm);

        var bridge = ReconciliationBridge.From(data.Settlements, data.Donations, results);

        Assert.NotEqual(0m, bridge.Unexplained);
    }

    [Fact]
    public void AnswerKey_AgreesWithTheGeneratorAndTheEngine()
    {
        var results = SampleResults(out var data);
        var key = SampleDataGenerator.AnswerKey;

        Assert.Equal(data.Settlements.Count, key.Sum(k => k.Settlements));
        Assert.Equal(data.Donations.Count, key.Sum(k => k.Gifts));

        // Rows the key files under each outcome equal the records the engine put there.
        int KeyRecords(string landsAs) => key.Where(k => k.ShouldLandAs == landsAs).Sum(k => k.Settlements + k.Gifts);
        int EngineRecords(MatchStatus s) => results.Where(r => r.Status == s)
            .Sum(r => (r.SettlementLine is null ? 0 : 1) + (r.DonationRecord is null ? 0 : 1));

        Assert.Equal(KeyRecords("Matched"), EngineRecords(MatchStatus.Matched));
        Assert.Equal(KeyRecords("Probable match, for review"), EngineRecords(MatchStatus.ProbableMatch));
        Assert.Equal(KeyRecords("Amount mismatch"), EngineRecords(MatchStatus.AmountMismatch));
        Assert.Equal(KeyRecords("Missing in CRM") + 2, EngineRecords(MatchStatus.MissingInCrm));
        Assert.Equal(KeyRecords("Missing at processor") + 1, EngineRecords(MatchStatus.MissingInProcessor));
        Assert.Equal(3, results.Count(r => r.Note?.StartsWith("Not paired on purpose") == true));
    }
}
