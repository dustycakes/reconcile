using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reconcile.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddResultNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "MatchResults",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "MatchResults");
        }
    }
}
