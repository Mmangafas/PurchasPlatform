using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPricing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CalculatedPriceDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "CalculatedSellingPriceValidFrom",
                table: "CalculatedSellingPrices",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // Existing prices were calculated from the cost valid on the day they were calculated.
            migrationBuilder.Sql("""
                UPDATE [CalculatedSellingPrices]
                SET [CalculatedSellingPriceValidFrom] = CAST([CalculatedSellingPriceCreatedAt] AS date)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalculatedSellingPriceValidFrom",
                table: "CalculatedSellingPrices");
        }
    }
}
