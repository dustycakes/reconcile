using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Reconcile.Core.Domain;

namespace Reconcile.Core.Import;

/// <summary>
/// Parses the two export shapes. Header names follow each source system's
/// vocabulary on purpose — the importer adapts to the files, the files don't
/// adapt to us. Rows that fail to parse are collected and reported, never
/// silently skipped: an import that drops rows quietly would corrupt the
/// reconciliation's core promise.
/// </summary>
public static class CsvImporter
{
    public record ImportResult<T>(List<T> Rows, List<string> Errors);

    public static ImportResult<SettlementLine> ReadSettlements(TextReader reader)
        => Read<SettlementLine, SettlementLineMap>(reader);

    public static ImportResult<DonationRecord> ReadDonations(TextReader reader)
        => Read<DonationRecord, DonationRecordMap>(reader);

    private static ImportResult<T> Read<T, TMap>(TextReader reader)
        where TMap : ClassMap
    {
        var rows = new List<T>();
        var errors = new List<string>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            ReadingExceptionOccurred = ctx =>
            {
                errors.Add($"row {ctx.Exception.Context?.Parser?.Row}: {ctx.Exception.Message.Split('\n')[0]}");
                return false; // collect and continue
            },
        };

        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<TMap>();
        rows.AddRange(csv.GetRecords<T>());

        return new ImportResult<T>(rows, errors);
    }

    private sealed class SettlementLineMap : ClassMap<SettlementLine>
    {
        public SettlementLineMap()
        {
            Map(m => m.ProcessorRef).Name("transaction_ref");
            Map(m => m.Gross).Name("gross_amount");
            Map(m => m.Fee).Name("fee_amount");
            Map(m => m.Net).Name("net_amount");
            Map(m => m.SettledOn).Name("settlement_date");
            Map(m => m.BatchId).Name("batch_id");
        }
    }

    private sealed class DonationRecordMap : ClassMap<DonationRecord>
    {
        public DonationRecordMap()
        {
            Map(m => m.RecordRef).Name("Internal ID");
            Map(m => m.ProcessorRef).Name("Processor Ref");
            Map(m => m.DonorName).Name("Donor");
            Map(m => m.Fund).Name("Fund");
            Map(m => m.Amount).Name("Amount");
            Map(m => m.ReceivedOn).Name("Date Received");
        }
    }
}
