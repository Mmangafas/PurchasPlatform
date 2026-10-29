using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using SmartPricing.Domain.Entities;
using SmartPricing.Domain.Enums;

namespace SmartPricing.Infrastructure.Data;

public class SmartPricingDbContext : DbContext
{
    public SmartPricingDbContext(DbContextOptions<SmartPricingDbContext> options) : base(options) { }

    // Master Data
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ItemGroup> ItemGroups => Set<ItemGroup>();
    public DbSet<ItemSubgroup> ItemSubgroups => Set<ItemSubgroup>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<PackagingUnit> PackagingUnits => Set<PackagingUnit>();
    public DbSet<ItemPackaging> ItemPackagings => Set<ItemPackaging>();
    public DbSet<Channel> Channels => Set<Channel>();

    // Pricing
    public DbSet<SupplierPriceList> SupplierPriceLists => Set<SupplierPriceList>();
    public DbSet<RoundingRule> RoundingRules => Set<RoundingRule>();
    public DbSet<PricingRule> PricingRules => Set<PricingRule>();
    public DbSet<CalculatedSellingPrice> CalculatedSellingPrices => Set<CalculatedSellingPrice>();

    // Workflow
    public DbSet<PriceBatch> PriceBatches => Set<PriceBatch>();
    public DbSet<PriceBatchLine> PriceBatchLines => Set<PriceBatchLine>();

    // Logging & Security
    public DbSet<ERPSyncLog> ERPSyncLogs => Set<ERPSyncLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PriceChangeHistory> PriceChangeHistories => Set<PriceChangeHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Explicit Primary Keys (non-conventional names) ──────────────────────
        modelBuilder.Entity<Item>().HasKey(e => e.ItemID);
        modelBuilder.Entity<Supplier>().HasKey(e => e.SupplierID);
        modelBuilder.Entity<Category>().HasKey(e => e.CategoryID);
        modelBuilder.Entity<ItemGroup>().HasKey(e => e.ItemGroupID);
        modelBuilder.Entity<ItemSubgroup>().HasKey(e => e.ItemSubgroupID);
        modelBuilder.Entity<UnitOfMeasure>().HasKey(e => e.UnitID);
        modelBuilder.Entity<PackagingUnit>().HasKey(e => e.PackagingUnitID);
        modelBuilder.Entity<ItemPackaging>().HasKey(e => e.ItemPackagingID);
        modelBuilder.Entity<Channel>().HasKey(e => e.ChannelID);
        modelBuilder.Entity<SupplierPriceList>().HasKey(e => e.SupplierPriceID);
        modelBuilder.Entity<RoundingRule>().HasKey(e => e.RoundingRuleID);
        modelBuilder.Entity<PricingRule>().HasKey(e => e.RuleID);
        modelBuilder.Entity<CalculatedSellingPrice>().HasKey(e => e.CalcPriceID);
        modelBuilder.Entity<PriceBatch>().HasKey(e => e.BatchID);
        modelBuilder.Entity<PriceBatchLine>().HasKey(e => e.LineID);
        modelBuilder.Entity<ERPSyncLog>().HasKey(e => e.SyncID);
        modelBuilder.Entity<User>().HasKey(e => e.UserID);
        modelBuilder.Entity<Role>().HasKey(e => e.RoleID);
        modelBuilder.Entity<AuditLog>().HasKey(e => e.AuditID);
        modelBuilder.Entity<PriceChangeHistory>().HasKey(e => e.HistoryID);

        // ── UserRole composite PK ─────────────────────────────────────────────
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserID, ur.RoleID });

        // ── SupplierPriceList: TotalCost is a C# expression property, not a DB column ─
        modelBuilder.Entity<SupplierPriceList>()
            .Ignore(s => s.TotalCost);

        // ── Enum conversions stored as strings ────────────────────────────────
        modelBuilder.Entity<PriceBatch>()
            .Property(b => b.Status)
            .HasConversion<string>();

        modelBuilder.Entity<PriceBatchLine>()
            .Property(l => l.LineStatus)
            .HasConversion<string>();

        modelBuilder.Entity<CalculatedSellingPrice>()
            .Property(c => c.ColorIndicator)
            .HasConversion<string>();

        // ── Decimal precision ─────────────────────────────────────────────────
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetPrecision(18);
                    property.SetScale(4);
                }
            }
        }

        // ── Relationships ─────────────────────────────────────────────────────
        // Master data that items point to can't be deleted while in use (the service checks first).
        modelBuilder.Entity<ItemGroup>().ToTable("Groups");
        modelBuilder.Entity<ItemSubgroup>().ToTable("Subgroups");
        modelBuilder.Entity<UnitOfMeasure>().ToTable("Units");

        // Category → Group → Subgroup
        modelBuilder.Entity<ItemGroup>()
            .HasOne(g => g.Category).WithMany(c => c.Groups)
            .HasForeignKey(g => g.CategoryID).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Item>()
            .HasOne(i => i.ItemGroup).WithMany()
            .HasForeignKey(i => i.ItemGroupID).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Item>()
            .HasOne(i => i.ItemSubgroup).WithMany()
            .HasForeignKey(i => i.ItemSubgroupID).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ItemSubgroup>()
            .HasOne(s => s.ItemGroup).WithMany(g => g.Subgroups)
            .HasForeignKey(s => s.ItemGroupID).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ItemPackaging>()
            .HasOne(p => p.Item).WithMany(i => i.Packagings)
            .HasForeignKey(p => p.ItemID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ItemPackaging>()
            .HasOne(p => p.PackagingUnit).WithMany()
            .HasForeignKey(p => p.PackagingUnitID).OnDelete(DeleteBehavior.Restrict);

        // Units: an item's base unit, and the unit a packaging unit converts to
        modelBuilder.Entity<Item>()
            .HasOne(i => i.Unit).WithMany()
            .HasForeignKey(i => i.UnitID).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PackagingUnit>()
            .HasOne(p => p.Unit).WithMany()
            .HasForeignKey(p => p.UnitID).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PriceBatch>()
            .HasOne(b => b.Creator)
            .WithMany()
            .HasForeignKey(b => b.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PriceBatch>()
            .HasOne(b => b.Submitter)
            .WithMany()
            .HasForeignKey(b => b.SubmittedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PriceBatch>()
            .HasOne(b => b.Approver)
            .WithMany()
            .HasForeignKey(b => b.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PriceChangeHistory>()
            .HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PricingRule>()
            .HasOne(r => r.RoundingRule)
            .WithMany()
            .HasForeignKey(r => r.RoundingRuleID)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Default data (Roles, Channels) ────────────────────────────────────
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleID = 1, RoleName = "Requester" },
            new Role { RoleID = 2, RoleName = "Reviewer" },
            new Role { RoleID = 3, RoleName = "Approver" },
            new Role { RoleID = 4, RoleName = "Admin" }
        );

        modelBuilder.Entity<Channel>().HasData(
            new Channel { ChannelID = 1, Code = "RETAIL", Name = "Retail" },
            new Channel { ChannelID = 2, Code = "HORECA", Name = "HoReCa" },
            new Channel { ChannelID = 3, Code = "WHOLESALE", Name = "Wholesale" }
        );

        modelBuilder.Entity<UnitOfMeasure>().HasData(
            new UnitOfMeasure { UnitID = 1, Code = "PCS", Name = "Τεμάχιο / Piece" },
            new UnitOfMeasure { UnitID = 2, Code = "KG", Name = "Κιλό / Kilogram" },
            new UnitOfMeasure { UnitID = 3, Code = "LT", Name = "Λίτρο / Litre" }
        );

        modelBuilder.Entity<PackagingUnit>().HasData(
            new PackagingUnit { PackagingUnitID = 1, Code = "BOX24", Name = "Κιβώτιο 24 τεμ. / Box of 24", UnitID = 1, Factor = 24 }
        );

        modelBuilder.Entity<RoundingRule>().HasData(
            new RoundingRule { RoundingRuleID = 1, RuleName = "Cents .99", Pattern = ".99", Description = "Round up to X.99", IsActive = true },
            new RoundingRule { RoundingRuleID = 2, RuleName = "Cents .50", Pattern = ".50", Description = "Round up to X.50", IsActive = true },
            new RoundingRule { RoundingRuleID = 3, RuleName = "Next .05", Pattern = ".05", Description = "Round up to the next .05", IsActive = true }
        );

        // ── Default Admin user (password: Admin@123) ──────────────────────────
        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserID = 1,
                UserName = "admin",
                DisplayName = "Administrator",
                Email = "admin@smartpricing.local",
                MustChangePassword = true,
                PasswordHash = "$2a$11$K5DSFLEWBfB3rOZoizqJuuHk1HHxGh4GH2YFMoaqkCuF.MxhZLqrS", // Invalid legacy hash; migration AddLoginSecurity replaces it with Admin@123
                IsActive = true
            }
        );

        modelBuilder.Entity<UserRole>().HasData(
            new UserRole { UserID = 1, RoleID = 4 }
        );

        ApplyColumnNamingConvention(modelBuilder);
    }

    // ── Column naming convention (all tables) ─────────────────────────────────
    // Every column carries its table's name as prefix (ItemName, CategoryCode, PriceBatchStatus …),
    // the key is <Table>ID, and a foreign key keeps the name of the key it points to, so joins read
    // Items.CategoryID = Categories.CategoryID, Groups.CategoryID = Categories.CategoryID, and so on.

    private static readonly Dictionary<Type, string> Prefixes = new()
    {
        [typeof(ItemGroup)] = "Group",
        [typeof(ItemSubgroup)] = "Subgroup",
        [typeof(UnitOfMeasure)] = "Unit",
    };

    private static readonly Dictionary<(Type, string), string> ColumnOverrides = new()
    {
        [(typeof(Item), nameof(Item.ERPItemCode))] = "ItemCode",
        [(typeof(Supplier), nameof(Supplier.ERPSupplierCode))] = "SupplierCode",
    };

    private static void ApplyColumnNamingConvention(ModelBuilder modelBuilder)
    {
        static string Prefix(IMutableEntityType e) => Prefixes.GetValueOrDefault(e.ClrType) ?? e.ClrType.Name;

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var prefix = Prefix(entity);
            // Trailing words of the prefix: "PriceBatchLine" → "BatchLine", "Line"; so LineStatus → PriceBatchLineStatus.
            var words = Regex.Matches(prefix, "[A-Z]+(?![a-z])|[A-Z][a-z]*").Select(m => m.Value).ToArray();
            var shortPrefixes = Enumerable.Range(1, words.Length - 1).Select(i => string.Concat(words[i..])).ToList();
            var key = entity.FindPrimaryKey()?.Properties;

            foreach (var property in entity.GetProperties())
            {
                var name = property.Name;
                var fk = property.GetContainingForeignKeys()
                    .FirstOrDefault(f => f.PrincipalKey.Properties.Count == 1 && f.PrincipalKey.Properties[0].Name == name);

                string column;
                if (ColumnOverrides.TryGetValue((entity.ClrType, name), out var overridden))
                    column = overridden;
                else if (fk is not null)
                    column = Prefix(fk.PrincipalEntityType) + "ID";
                else if (key is { Count: 1 } && key[0] == property)
                    column = prefix + "ID";
                else if (name.StartsWith(prefix))
                    column = name;
                else if (shortPrefixes.FirstOrDefault(name.StartsWith) is { } shortPrefix)
                    column = prefix + name[shortPrefix.Length..];
                else
                    column = prefix + name;

                property.SetColumnName(column);
            }
        }
    }
}
