using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPricing.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                CategoryID = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Code = table.Column<string>(maxLength: 50, nullable: false),
                Name = table.Column<string>(maxLength: 100, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Categories", x => x.CategoryID));

        migrationBuilder.CreateTable(
            name: "Channels",
            columns: table => new
            {
                ChannelID = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Code = table.Column<string>(maxLength: 50, nullable: false),
                Name = table.Column<string>(maxLength: 100, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Channels", x => x.ChannelID));

        migrationBuilder.CreateTable(
            name: "ERPSyncLogs",
            columns: table => new
            {
                SyncID = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                SyncType = table.Column<string>(maxLength: 50, nullable: false),
                Entity = table.Column<string>(maxLength: 50, nullable: false),
                Status = table.Column<string>(maxLength: 20, nullable: false),
                Message = table.Column<string>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "SYSDATETIME()")
            },
            constraints: table => table.PrimaryKey("PK_ERPSyncLogs", x => x.SyncID));

        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                RoleID = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                RoleName = table.Column<string>(maxLength: 100, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Roles", x => x.RoleID));

        migrationBuilder.CreateTable(
            name: "RoundingRules",
            columns: table => new
            {
                RoundingRuleID = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                RuleName = table.Column<string>(maxLength: 100, nullable: false),
                Pattern = table.Column<string>(maxLength: 10, nullable: false),
                Description = table.Column<string>(maxLength: 200, nullable: true),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true)
            },
            constraints: table => table.PrimaryKey("PK_RoundingRules", x => x.RoundingRuleID));

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                UserID = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserName = table.Column<string>(maxLength: 100, nullable: false),
                DisplayName = table.Column<string>(maxLength: 200, nullable: false),
                Email = table.Column<string>(maxLength: 200, nullable: true),
                PasswordHash = table.Column<string>(nullable: false),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true)
            },
            constraints: table => table.PrimaryKey("PK_Users", x => x.UserID));

        // Seed data handled in DbContext.OnModelCreating via HasData
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Categories");
        migrationBuilder.DropTable("Channels");
        migrationBuilder.DropTable("ERPSyncLogs");
        migrationBuilder.DropTable("Roles");
        migrationBuilder.DropTable("RoundingRules");
        migrationBuilder.DropTable("Users");
    }
}

/*
  NOTE: Rather than maintaining a hand-written full migration, run:
  
    dotnet ef migrations add InitialCreate --project SmartPricing.Infrastructure --startup-project SmartPricing.Web
    dotnet ef database update --project SmartPricing.Infrastructure --startup-project SmartPricing.Web
  
  This will auto-generate the complete migration from the DbContext.
*/
