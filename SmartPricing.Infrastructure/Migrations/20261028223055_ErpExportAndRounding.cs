using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPricing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ErpExportAndRounding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExportedAt",
                table: "PriceBatchLines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "RoundingRules",
                keyColumn: "RoundingRuleID",
                keyValue: 1,
                column: "Description",
                value: "Round up to X.99");

            migrationBuilder.UpdateData(
                table: "RoundingRules",
                keyColumn: "RoundingRuleID",
                keyValue: 2,
                column: "Description",
                value: "Round up to X.50");

            migrationBuilder.UpdateData(
                table: "RoundingRules",
                keyColumn: "RoundingRuleID",
                keyValue: 3,
                columns: new[] { "Description", "RuleName" },
                values: new object[] { "Round up to the next .05", "Next .05" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExportedAt",
                table: "PriceBatchLines");

            migrationBuilder.UpdateData(
                table: "RoundingRules",
                keyColumn: "RoundingRuleID",
                keyValue: 1,
                column: "Description",
                value: "Round to X.99");

            migrationBuilder.UpdateData(
                table: "RoundingRules",
                keyColumn: "RoundingRuleID",
                keyValue: 2,
                column: "Description",
                value: "Round to X.50");

            migrationBuilder.UpdateData(
                table: "RoundingRules",
                keyColumn: "RoundingRuleID",
                keyValue: 3,
                columns: new[] { "Description", "RuleName" },
                values: new object[] { "Round to nearest .05", "Nearest .05" });
        }
    }
}
