using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class variablesettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SettlementDay",
                table: "CreditCards",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SettlementDay",
                table: "CreditCards");
        }
    }
}
