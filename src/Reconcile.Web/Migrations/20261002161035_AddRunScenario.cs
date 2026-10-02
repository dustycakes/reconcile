using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reconcile.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRunScenario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Scenario",
                table: "Runs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scenario",
                table: "Runs");
        }
    }
}
