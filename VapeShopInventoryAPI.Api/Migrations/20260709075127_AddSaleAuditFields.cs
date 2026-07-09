using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VapeShopInventoryAPI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReductionFrequency",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalQuantityReduction",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TransactionCount",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TransactionNumber",
                table: "SaleItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReductionFrequency",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TotalQuantityReduction",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TransactionCount",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TransactionNumber",
                table: "SaleItems");
        }
    }
}
