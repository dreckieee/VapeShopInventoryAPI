using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VapeShopInventoryAPI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settlements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SaleId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExpenseId = table.Column<int>(type: "INTEGER", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaymentMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentNote = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settlements_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Settlements_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_ExpenseId",
                table: "Settlements",
                column: "ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_SaleId",
                table: "Settlements",
                column: "SaleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settlements");
        }
    }
}
