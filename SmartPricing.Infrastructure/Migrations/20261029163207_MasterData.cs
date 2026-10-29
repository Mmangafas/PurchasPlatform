using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartPricing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemGroupID",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemSubgroupID",
                table: "Items",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: true);   // Existing categories stay active

            migrationBuilder.CreateTable(
                name: "ItemGroups",
                columns: table => new
                {
                    ItemGroupID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemGroups", x => x.ItemGroupID);
                });

            migrationBuilder.CreateTable(
                name: "PackagingUnits",
                columns: table => new
                {
                    PackagingUnitID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackagingUnits", x => x.PackagingUnitID);
                });

            migrationBuilder.CreateTable(
                name: "UnitsOfMeasure",
                columns: table => new
                {
                    UnitID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitsOfMeasure", x => x.UnitID);
                });

            migrationBuilder.CreateTable(
                name: "ItemSubgroups",
                columns: table => new
                {
                    ItemSubgroupID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemGroupID = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemSubgroups", x => x.ItemSubgroupID);
                    table.ForeignKey(
                        name: "FK_ItemSubgroups_ItemGroups_ItemGroupID",
                        column: x => x.ItemGroupID,
                        principalTable: "ItemGroups",
                        principalColumn: "ItemGroupID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemPackagings",
                columns: table => new
                {
                    ItemPackagingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    PackagingUnitID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemPackagings", x => x.ItemPackagingID);
                    table.ForeignKey(
                        name: "FK_ItemPackagings_Items_ItemID",
                        column: x => x.ItemID,
                        principalTable: "Items",
                        principalColumn: "ItemID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemPackagings_PackagingUnits_PackagingUnitID",
                        column: x => x.PackagingUnitID,
                        principalTable: "PackagingUnits",
                        principalColumn: "PackagingUnitID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PackagingUnits",
                columns: new[] { "PackagingUnitID", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "BOX", true, "Κιβώτιο / Box" },
                    { 2, "PAL", true, "Παλέτα / Pallet" }
                });

            migrationBuilder.InsertData(
                table: "UnitsOfMeasure",
                columns: new[] { "UnitID", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "PCS", true, "Τεμάχιο / Piece" },
                    { 2, "KG", true, "Κιλό / Kilogram" },
                    { 3, "LT", true, "Λίτρο / Litre" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemGroupID",
                table: "Items",
                column: "ItemGroupID");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemSubgroupID",
                table: "Items",
                column: "ItemSubgroupID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemPackagings_ItemID",
                table: "ItemPackagings",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemPackagings_PackagingUnitID",
                table: "ItemPackagings",
                column: "PackagingUnitID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSubgroups_ItemGroupID",
                table: "ItemSubgroups",
                column: "ItemGroupID");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_ItemGroups_ItemGroupID",
                table: "Items",
                column: "ItemGroupID",
                principalTable: "ItemGroups",
                principalColumn: "ItemGroupID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_ItemSubgroups_ItemSubgroupID",
                table: "Items",
                column: "ItemSubgroupID",
                principalTable: "ItemSubgroups",
                principalColumn: "ItemSubgroupID",
                onDelete: ReferentialAction.Restrict);

            // Units already used by items become entries in the units list.
            migrationBuilder.Sql("""
                INSERT INTO [UnitsOfMeasure] ([Code], [Name], [IsActive])
                SELECT DISTINCT [UoM], [UoM], 1 FROM [Items]
                WHERE [UoM] <> '' AND [UoM] NOT IN (SELECT [Code] FROM [UnitsOfMeasure])
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_ItemGroups_ItemGroupID",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_ItemSubgroups_ItemSubgroupID",
                table: "Items");

            migrationBuilder.DropTable(
                name: "ItemPackagings");

            migrationBuilder.DropTable(
                name: "ItemSubgroups");

            migrationBuilder.DropTable(
                name: "UnitsOfMeasure");

            migrationBuilder.DropTable(
                name: "PackagingUnits");

            migrationBuilder.DropTable(
                name: "ItemGroups");

            migrationBuilder.DropIndex(
                name: "IX_Items_ItemGroupID",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_ItemSubgroupID",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ItemGroupID",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ItemSubgroupID",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Categories");
        }
    }
}
