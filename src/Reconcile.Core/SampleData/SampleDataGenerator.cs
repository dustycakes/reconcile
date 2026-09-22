using System.Globalization;
using System.Text;
using Reconcile.Core.Domain;

namespace Reconcile.Core.SampleData;

/// <summary>
/// Generates the two demo exports with deliberate, labeled defects — the
/// kinds of problems reconciliation exists to find. Deterministic for a given
/// seed so the numbers in screenshots and the README stay reproducible.
/// Every donor is invented; no real person or organization appears here.
/// </summary>
public static class SampleDataGenerator
{
    private static readonly string[] FirstNames =
        ["Avery", "Jordan", "Casey", "Riley", "Morgan", "Quinn", "Hayden", "Rowan",
         "Skyler", "Emerson", "Dakota", "Reese", "Finley", "Sawyer", "Elliott", "Marlow"];

    private static readonly string[] LastNames =
        ["Larson", "Whitfield", "Bergstrom", "Callahan", "Denning", "Eastman", "Foss",
         "Granger", "Holloway", "Iverson", "Keller", "Lindqvist", "Mercer", "Nordell"];

    private static readonly string[] Funds =
        ["Habitat Stewardship", "Land Protection", "Elk Restoration", "Unrestricted", "Youth Education"];

    private static readonly decimal[] CommonGifts = [25m, 35m, 50m, 75m, 100m, 150m, 250m, 500m, 1000m];

    public record Generated(List<SettlementLine> Settlements, List<DonationRecord> Donations);

    public static Generated Generate(int seed = 42)
    {
        var rng = new Random(seed);
        var settlements = new List<SettlementLine>();
        var donations = new List<DonationRecord>();
        int txn = 1000, ns = 5000;

        DateOnly Day() => new DateOnly(2026, 8, 1).AddDays(rng.Next(0, 28));
        string NextRef() => $"TXN-{txn++}";
        string NextNs() => $"NS-{ns++}";
        string Donor() => $"{FirstNames[rng.Next(FirstNames.Length)]} {LastNames[rng.Next(LastNames.Length)]}";
        string Fund() => Funds[rng.Next(Funds.Length)];
        decimal Gift() => CommonGifts[rng.Next(CommonGifts.Length)];

        SettlementLine Settle(string procRef, decimal gross, DateOnly on)
        {
            var fee = Math.Round(gross * 0.022m + 0.30m, 2);
            var line = new SettlementLine
            {
                ProcessorRef = procRef, Gross = gross, Fee = fee, Net = gross - fee,
                SettledOn = on, BatchId = $"DEP-{on:yyyyMMdd}",
            };
            settlements.Add(line);
            return line;
        }

        DonationRecord Record(string? procRef, decimal amount, DateOnly on) =>
            new()
            {
                RecordRef = NextNs(), ProcessorRef = procRef, DonorName = Donor(),
                Fund = Fund(), Amount = amount, ReceivedOn = on,
            };

        // 100 clean pairs — the boring majority a healthy month is made of.
        for (var i = 0; i < 100; i++)
        {
            var on = Day();
            var amount = Gift();
            var line = Settle(NextRef(), amount, on);
            donations.Add(Record(line.ProcessorRef, amount, on));
        }

        // 6 phone/mail gifts: ref never captured, but amount + nearby date
        // point at exactly one settlement. Should land as ProbableMatch.
        for (var i = 0; i < 6; i++)
        {
            var on = Day();
            var amount = 111m + i * 17m; // unusual amounts so each is unique
            Settle(NextRef(), amount, on);
            donations.Add(Record(null, amount, on.AddDays(rng.Next(-2, 3))));
        }

        // 1 ambiguous case: two identical settlements, one ref-less gift.
        // The engine must refuse to guess; all three surface as findings.
        {
            var on = new DateOnly(2026, 8, 12);
            Settle(NextRef(), 200m, on);
            Settle(NextRef(), 200m, on.AddDays(1));
            donations.Add(Record(null, 200m, on));
        }

        // 3 keying errors: ref captured correctly, amount typed wrong.
        foreach (var (gross, keyed) in new[] { (50m, 500m), (75m, 57m), (250m, 25m) })
        {
            var on = Day();
            var line = Settle(NextRef(), gross, on);
            donations.Add(Record(line.ProcessorRef, keyed, on));
        }

        // 4 settlements the CRM never recorded (web gifts that skipped entry).
        for (var i = 0; i < 4; i++)
            Settle(NextRef(), Gift(), Day());

        // 3 donations whose refs match nothing (mistyped at entry).
        for (var i = 0; i < 3; i++)
            donations.Add(Record($"TXN-9{rng.Next(100, 999)}", Gift(), Day()));

        // 1 blank-ref donation matching nothing — a check that never cleared.
        donations.Add(Record(null, 68m, Day()));

        return new Generated(settlements, donations);
    }

    public static (string SettlementsCsv, string DonationsCsv) ToCsv(Generated data)
    {
        var inv = CultureInfo.InvariantCulture;

        var s = new StringBuilder("transaction_ref,gross_amount,fee_amount,net_amount,settlement_date,batch_id\n");
        foreach (var l in data.Settlements)
            s.AppendLine(string.Create(inv, $"{l.ProcessorRef},{l.Gross:F2},{l.Fee:F2},{l.Net:F2},{l.SettledOn:yyyy-MM-dd},{l.BatchId}"));

        var d = new StringBuilder("Internal ID,Processor Ref,Donor,Fund,Amount,Date Received\n");
        foreach (var r in data.Donations)
            d.AppendLine(string.Create(inv, $"{r.RecordRef},{r.ProcessorRef},{r.DonorName},{r.Fund},{r.Amount:F2},{r.ReceivedOn:yyyy-MM-dd}"));

        return (s.ToString(), d.ToString());
    }
}
