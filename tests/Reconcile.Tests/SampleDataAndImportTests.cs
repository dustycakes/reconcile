using Reconcile.Core.Domain;
using Reconcile.Core.Import;
using Reconcile.Core.Matching;
using Reconcile.Core.SampleData;

namespace Reconcile.Tests;

public class SampleDataAndImportTests
{
    [Fact]
    public void SampleData_RoundTripsThroughCsv_Losslessly()
    {
        var data = SampleDataGenerator.Generate();
        var (settlementsCsv, donationsCsv) = SampleDataGenerator.ToCsv(data);

        var s = CsvImporter.ReadSettlements(new StringReader(settlementsCsv));
        var d = CsvImporter.ReadDonations(new StringReader(donationsCsv));

        Assert.Empty(s.Errors);
        Assert.Empty(d.Errors);
        Assert.Equal(data.Settlements.Count, s.Rows.Count);
        Assert.Equal(data.Donations.Count, d.Rows.Count);
        Assert.Equal(data.Settlements.Sum(x => x.Gross), s.Rows.Sum(x => x.Gross));
        Assert.Equal(data.Donations.Sum(x => x.Amount), d.Rows.Sum(x => x.Amount));
    }

    [Fact]
    public void SampleData_ProducesEveryFindingKind_AndIsDeterministic()
    {
        var data = SampleDataGenerator.Generate();
        AssignIds(data);

        var results = new MatchEngine().Run(data.Settlements, data.Donations);

        // The generator's engineered defects, found by the engine:
        Assert.Equal(100, results.Count(r => r.Status == MatchStatus.Matched));
        Assert.Equal(6, results.Count(r => r.Status == MatchStatus.ProbableMatch));
        Assert.Equal(3, results.Count(r => r.Status == MatchStatus.AmountMismatch));
        // 4 unrecorded web gifts + 2 halves of the ambiguous pair
        Assert.Equal(6, results.Count(r => r.Status == MatchStatus.MissingInCrm));
        // 3 mistyped refs + 1 blank-ref orphan + 1 ambiguous gift
        Assert.Equal(5, results.Count(r => r.Status == MatchStatus.MissingInProcessor));

        // Same seed, same output — screenshots stay reproducible.
        var again = SampleDataGenerator.Generate();
        Assert.Equal(data.Settlements.Sum(x => x.Gross), again.Settlements.Sum(x => x.Gross));
    }

    [Fact]
    public void Import_CollectsBadRows_InsteadOfDroppingThemSilently()
    {
        const string csv = "transaction_ref,gross_amount,fee_amount,net_amount,settlement_date,batch_id\n" +
                           "TXN-1,50.00,1.40,48.60,2026-08-03,DEP-1\n" +
                           "TXN-2,not-a-number,1.40,48.60,2026-08-03,DEP-1\n" +
                           "TXN-3,75.00,1.95,73.05,2026-08-04,DEP-1\n";

        var result = CsvImporter.ReadSettlements(new StringReader(csv));

        Assert.Equal(2, result.Rows.Count);
        var error = Assert.Single(result.Errors);
        Assert.Contains("row 3", error);
    }

    private static void AssignIds(SampleDataGenerator.Generated data)
    {
        for (var i = 0; i < data.Settlements.Count; i++) data.Settlements[i].Id = i + 1;
        for (var i = 0; i < data.Donations.Count; i++) data.Donations[i].Id = i + 1;
    }
}
