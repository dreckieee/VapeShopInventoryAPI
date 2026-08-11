using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VapeShopInventoryAPI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodAndNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PaymentNote",
                table: "Sales",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Expenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PaymentNote",
                table: "Expenses",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PaymentNote",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PaymentNote",
                table: "Expenses");
        }
    }
}
