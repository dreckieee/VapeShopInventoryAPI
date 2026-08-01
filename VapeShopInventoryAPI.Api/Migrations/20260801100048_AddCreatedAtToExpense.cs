using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VapeShopInventoryAPI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtToExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Expenses",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

                migrationBuilder.Sql("UPDATE Expenses SET CreatedAt = Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Expenses");
        }
    }
}
