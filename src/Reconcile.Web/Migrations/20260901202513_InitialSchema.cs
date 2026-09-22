using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reconcile.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Runs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SettlementFileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DonationFileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DonationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordRef = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProcessorRef = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DonorName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Fund = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ReceivedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ReconciliationRunId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonationRecords_Runs_ReconciliationRunId",
                        column: x => x.ReconciliationRunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SettlementLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessorRef = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Gross = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Net = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    SettledOn = table.Column<DateOnly>(type: "date", nullable: false),
                    BatchId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReconciliationRunId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementLines_Runs_ReconciliationRunId",
                        column: x => x.ReconciliationRunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    MatchedBy = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SettlementLineId = table.Column<int>(type: "int", nullable: true),
                    DonationRecordId = table.Column<int>(type: "int", nullable: true),
                    AmountDelta = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    ReconciliationRunId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchResults_DonationRecords_DonationRecordId",
                        column: x => x.DonationRecordId,
                        principalTable: "DonationRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchResults_Runs_ReconciliationRunId",
                        column: x => x.ReconciliationRunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchResults_SettlementLines_SettlementLineId",
                        column: x => x.SettlementLineId,
                        principalTable: "SettlementLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonationRecords_ReconciliationRunId_ProcessorRef",
                table: "DonationRecords",
                columns: new[] { "ReconciliationRunId", "ProcessorRef" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_DonationRecordId",
                table: "MatchResults",
                column: "DonationRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_ReconciliationRunId_Status",
                table: "MatchResults",
                columns: new[] { "ReconciliationRunId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_SettlementLineId",
                table: "MatchResults",
                column: "SettlementLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementLines_ReconciliationRunId_ProcessorRef",
                table: "SettlementLines",
                columns: new[] { "ReconciliationRunId", "ProcessorRef" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchResults");

            migrationBuilder.DropTable(
                name: "DonationRecords");

            migrationBuilder.DropTable(
                name: "SettlementLines");

            migrationBuilder.DropTable(
                name: "Runs");
        }
    }
}
