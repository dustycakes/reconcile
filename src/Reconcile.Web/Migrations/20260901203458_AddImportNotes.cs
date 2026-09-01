using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reconcile.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddImportNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImportNotes",
                table: "Runs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImportNotes",
                table: "Runs");
        }
    }
}
