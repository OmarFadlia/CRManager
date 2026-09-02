using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSettlementDayToCreditCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SettlementDay",
                table: "CreditCards",
                type: "int",
                nullable: false);
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
