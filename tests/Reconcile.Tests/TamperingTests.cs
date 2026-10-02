using Reconcile.Core.Domain;
using Reconcile.Core.Matching;
using Reconcile.Core.Reporting;
using Reconcile.Core.SampleData;

namespace Reconcile.Tests;

public class TamperingTests
{
    private static (List<MatchResult> Results, SampleDataGenerator.Generated Data, List<TamperEdit> Applied) Run(params TamperEdit[] edits)
    {
        var data = SampleDataGenerator.Generate();
        var applied = Tampering.Apply(data, edits);
        for (var i = 0; i < data.Settlements.Count; i++) data.Settlements[i].Id = i + 1;
        for (var i = 0; i < data.Donations.Count; i++) data.Donations[i].Id = i + 1;
        return (new MatchEngine().Run(data.Settlements, data.Donations), data, applied);
    }

    private static IEnumerable<MatchResult> Touching(List<MatchResult> results, TamperEdit e) =>
        results.Where(r => r.SettlementLine?.ProcessorRef == e.ProcessorRef
                           || r.DonationRecord?.RecordRef == e.RecordRef);

    [Fact]
    public void EachKindOfEdit_LandsWhereItIsPredicted()
    {
        var (results, _, applied) = Run(
            new TamperEdit(TamperKind.ChangeGiftAmount, "TXN-1003", 999m),
            new TamperEdit(TamperKind.DeleteGift, "TXN-1010"),
            new TamperEdit(TamperKind.DeleteSettlement, "TXN-1020"),
            new TamperEdit(TamperKind.DuplicateSettlement, "TXN-1030"));

        Assert.Equal(MatchStatus.AmountMismatch, Assert.Single(Touching(results, applied[0])).Status);
        Assert.Equal(MatchStatus.MissingInCrm, Assert.Single(Touching(results, applied[1])).Status);

        var orphan = Assert.Single(Touching(results, applied[2]));
        Assert.Equal(MatchStatus.MissingInProcessor, orphan.Status);
        Assert.Contains("TXN-1020 is not in this settlement file", orphan.Note);

        var dup = Touching(results, applied[3]).ToList();
        Assert.Equal(2, dup.Count);
        Assert.Single(dup, r => r.Status == MatchStatus.Matched);
        Assert.Contains("double settlement", Assert.Single(dup, r => r.Status == MatchStatus.MissingInCrm).Note);
    }

    [Fact]
    public void BlankedReference_IsPairedOnAmountAndDate_OrRefused_NeverMatchedOnReference()
    {
        var (results, _, applied) = Run(new TamperEdit(TamperKind.BlankGiftReference, "TXN-1007"));

        var touched = Touching(results, applied[0]).ToList();
        Assert.DoesNotContain(touched, r => r.Status == MatchStatus.Matched);
        Assert.All(touched, r => Assert.False(string.IsNullOrWhiteSpace(r.Note)));
    }

    [Fact]
    public void AfterAnyEdits_EveryRecordIsAccountedFor_AndTheBridgeStillTies()
    {
        var (results, data, _) = Run(
            new TamperEdit(TamperKind.ChangeGiftAmount, "TXN-1001", 5000m),
            new TamperEdit(TamperKind.DeleteGift, "TXN-1002"),
            new TamperEdit(TamperKind.DeleteSettlement, "TXN-1004"),
            new TamperEdit(TamperKind.BlankGiftReference, "TXN-1005"),
            new TamperEdit(TamperKind.DuplicateSettlement, "TXN-1006"));

        Assert.Equal(data.Settlements.Count, results.Count(r => r.SettlementLine is not null));
        Assert.Equal(data.Donations.Count, results.Count(r => r.DonationRecord is not null));
        Assert.Equal(0m, ReconciliationBridge.From(data.Settlements, data.Donations, results).Unexplained);
    }

    [Theory]
    [InlineData("TXN-1150")]  // a planted defect, not a clean pair
    [InlineData("NOPE")]
    public void Edits_OnlyTouchCleanPairs(string procRef)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Tampering.Apply(SampleDataGenerator.Generate(), [new TamperEdit(TamperKind.DeleteGift, procRef)]));
        Assert.Contains("not one of the sample's clean gifts", ex.Message);
    }

    [Fact]
    public void OneEditPerGift_AndAChangedAmountMustChange()
    {
        Assert.Throws<ArgumentException>(() => Tampering.Apply(SampleDataGenerator.Generate(),
            [new TamperEdit(TamperKind.DeleteGift, "TXN-1001"), new TamperEdit(TamperKind.DuplicateSettlement, "TXN-1001")]));

        var data = SampleDataGenerator.Generate();
        var same = data.Donations[0].Amount;
        Assert.Throws<ArgumentException>(() => Tampering.Apply(data,
            [new TamperEdit(TamperKind.ChangeGiftAmount, data.Donations[0].ProcessorRef!, same)]));
    }
}
