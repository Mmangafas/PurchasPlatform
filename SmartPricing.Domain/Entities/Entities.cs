using SmartPricing.Domain.Enums;

namespace SmartPricing.Domain.Entities;

// ── Master Data ──────────────────────────────────────────────────────────────

public class Item
{
    public int ItemID { get; set; }
    public string ERPItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int? CategoryID { get; set; }
    public int? BrandID { get; set; }
    public string UoM { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }

    public Category? Category { get; set; }
    public ICollection<SupplierPriceList> SupplierPrices { get; set; } = [];
    public ICollection<PriceBatchLine> BatchLines { get; set; } = [];
    public ICollection<PriceChangeHistory> PriceHistory { get; set; } = [];
    public ICollection<CalculatedSellingPrice> CalculatedPrices { get; set; } = [];
}

public class Supplier
{
    public int SupplierID { get; set; }
    public string ERPSupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }

    public ICollection<SupplierPriceList> PriceLists { get; set; } = [];
}

public class Category
{
    public int CategoryID { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Item> Items { get; set; } = [];
    public ICollection<PricingRule> PricingRules { get; set; } = [];
}

public class Channel
{
    public int ChannelID { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<PricingRule> PricingRules { get; set; } = [];
    public ICollection<PriceBatchLine> BatchLines { get; set; } = [];
    public ICollection<PriceChangeHistory> PriceHistory { get; set; } = [];
    public ICollection<CalculatedSellingPrice> CalculatedPrices { get; set; } = [];
}

// ── Pricing ──────────────────────────────────────────────────────────────────

public class SupplierPriceList
{
    public int SupplierPriceID { get; set; }
    public int SupplierID { get; set; }
    public int ItemID { get; set; }
    public decimal BasePrice { get; set; }
    public decimal ExtraCharges { get; set; }
    public decimal TotalCost => BasePrice + ExtraCharges;
    public string? Currency { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }

    public Supplier? Supplier { get; set; }
    public Item? Item { get; set; }
}

public class RoundingRule
{
    public int RoundingRuleID { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty; // e.g. ".99", ".50", ".05"
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PricingRule
{
    public int RuleID { get; set; }
    public int? CategoryID { get; set; }
    public int? ChannelID { get; set; }
    public decimal? TargetMarginPercent { get; set; }
    public decimal? MarkupPercent { get; set; }
    public int? RoundingRuleID { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }

    public Category? Category { get; set; }
    public Channel? Channel { get; set; }
    public RoundingRule? RoundingRule { get; set; }
}

public class CalculatedSellingPrice
{
    public int CalcPriceID { get; set; }
    public int ItemID { get; set; }
    public int ChannelID { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossMarginPercent { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal SuggestedPrice { get; set; }
    public ColorIndicator ColorIndicator { get; set; }
    public int? RuleID { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }

    public Item? Item { get; set; }
    public Channel? Channel { get; set; }
    public PricingRule? Rule { get; set; }
}

// ── Workflow ─────────────────────────────────────────────────────────────────

public class PriceBatch
{
    public int BatchID { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public BatchStatus Status { get; set; } = BatchStatus.Draft;
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public User? Creator { get; set; }
    public User? Submitter { get; set; }
    public User? Approver { get; set; }
    public ICollection<PriceBatchLine> Lines { get; set; } = [];
}

public class PriceBatchLine
{
    public int LineID { get; set; }
    public int BatchID { get; set; }
    public int ItemID { get; set; }
    public int ChannelID { get; set; }
    public decimal? OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal? MarginPercent { get; set; }
    public LineStatus LineStatus { get; set; } = LineStatus.Pending;
    public string? ReviewerComment { get; set; }
    public string? ApproverComment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedBy { get; set; }

    public PriceBatch? Batch { get; set; }
    public Item? Item { get; set; }
    public Channel? Channel { get; set; }
}

// ── Logging & Security ────────────────────────────────────────────────────────

public class ERPSyncLog
{
    public int SyncID { get; set; }
    public string SyncType { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class User
{
    public int UserID { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public bool MustChangePassword { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}

public class Role
{
    public int RoleID { get; set; }
    public string RoleName { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = [];
}

public class UserRole
{
    public int UserID { get; set; }
    public int RoleID { get; set; }

    public User? User { get; set; }
    public Role? Role { get; set; }
}

public class AuditLog
{
    public int AuditID { get; set; }
    public int? UserID { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int? EntityID { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}

public class PriceChangeHistory
{
    public int HistoryID { get; set; }
    public int ItemID { get; set; }
    public int ChannelID { get; set; }
    public decimal? OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal? ChangePercent { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public int ChangedBy { get; set; }

    public Item? Item { get; set; }
    public Channel? Channel { get; set; }
    public User? ChangedByUser { get; set; }
}
