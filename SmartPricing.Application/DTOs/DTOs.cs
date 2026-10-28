using SmartPricing.Domain.Enums;

namespace SmartPricing.Application.DTOs;

// ── Supplier Prices ───────────────────────────────────────────────────────────

public record SupplierPriceDto(
    int SupplierPriceID,
    int SupplierID,
    string SupplierName,
    int ItemID,
    string ERPItemCode,
    string ItemName,
    string UoM,
    string? CategoryName,
    decimal BasePrice,
    decimal ExtraCharges,
    decimal TotalCost,
    string? Currency,
    DateOnly ValidFrom,
    DateOnly? ValidTo
);

public record CreateSupplierPriceDto(
    int SupplierID,
    int ItemID,
    decimal BasePrice,
    decimal ExtraCharges,
    string? Currency,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    int CreatedBy
);

// ── Calculated Selling Prices ─────────────────────────────────────────────────

public record CalculatedPriceDto(
    int CalcPriceID,
    int ItemID,
    string ERPItemCode,
    string ItemName,
    string UoM,
    int ChannelID,
    string ChannelName,
    decimal Cost,
    decimal GrossMarginPercent,
    decimal MarkupPercent,
    decimal SuggestedPrice,
    ColorIndicator ColorIndicator,
    decimal? TargetMargin
);

public record CalculatePriceRequest(
    int ItemID,
    int ChannelID,
    decimal Cost,
    int? RuleID,
    int CreatedBy
);

// ── Batches ───────────────────────────────────────────────────────────────────

public record BatchSummaryDto(
    int BatchID,
    string BatchName,
    BatchStatus Status,
    string CreatedByName,
    DateTime CreatedAt,
    int TotalLines,
    int ApprovedLines,
    int RejectedLines,
    int PendingLines
);

public record BatchDetailDto(
    int BatchID,
    string BatchName,
    BatchStatus Status,
    string CreatedByName,
    DateTime CreatedAt,
    string? SubmittedByName,
    DateTime? SubmittedAt,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    List<BatchLineDto> Lines
);

public record BatchLineDto(
    int LineID,
    int BatchID,
    int ItemID,
    string ERPItemCode,
    string ItemName,
    int ChannelID,
    string ChannelName,
    decimal? OldPrice,
    decimal NewPrice,
    decimal? MarginPercent,
    LineStatus LineStatus,
    string? ReviewerComment,
    string? ApproverComment
);

public record CreateBatchDto(string BatchName, int CreatedBy);

public record AddBatchLineDto(
    int BatchID,
    int ItemID,
    int ChannelID,
    decimal NewPrice,
    int CreatedBy
);

public record ApproveBatchLineDto(int LineID, int ApproverID, string? Comment);
public record RejectBatchLineDto(int LineID, int ApproverID, string? Comment);

// ── Pricing Rules ─────────────────────────────────────────────────────────────

public record PricingRuleDto(
    int RuleID,
    int? CategoryID,
    string? CategoryName,
    int? ChannelID,
    string? ChannelName,
    decimal? TargetMarginPercent,
    decimal? MarkupPercent,
    int? RoundingRuleID,
    string? RoundingPattern,
    bool IsActive
);

public record CreatePricingRuleDto(
    int? CategoryID,
    int? ChannelID,
    decimal? TargetMarginPercent,
    decimal? MarkupPercent,
    int? RoundingRuleID,
    int CreatedBy
);

// ── ERP ───────────────────────────────────────────────────────────────────────

public record ErpItemDto(
    string ErpItemCode,
    string Name,
    string? CategoryCode,
    string? BrandCode,
    string Uom,
    bool IsActive
);

public record ErpSupplierDto(
    string ErpSupplierCode,
    string Name,
    bool IsActive
);

public record ErpPurchasePriceDto(
    string ErpSupplierCode,
    string ErpItemCode,
    decimal BasePrice,
    decimal ExtraCharges,
    string? Currency,
    DateOnly ValidFrom,
    DateOnly? ValidTo
);

public record ErpSellingPriceExportDto(
    string ErpItemCode,
    string ChannelCode,
    decimal NewPrice,
    DateOnly ValidFrom,
    int BatchId
);

public record SyncResultDto(bool Success, string Message, int RecordsProcessed);

// ── Dashboard ─────────────────────────────────────────────────────────────────

public record DashboardKpiDto(
    int TotalBatches,
    int PendingApproval,
    int ApprovedToday,
    int ItemsBelowTargetMargin,
    decimal AverageMarginPercent,
    List<RecentBatchDto> RecentBatches,
    List<MarginAlertDto> MarginAlerts
);

public record RecentBatchDto(int BatchID, string BatchName, BatchStatus Status, DateTime CreatedAt, string CreatedByName);
public record MarginAlertDto(string ItemName, string ChannelName, decimal MarginPercent, decimal TargetMargin, ColorIndicator Indicator);

// ── Auth ──────────────────────────────────────────────────────────────────────

public record LoginDto(string UserName, string Password);
public record UserDto(int UserID, string UserName, string DisplayName, string? Email, List<string> Roles);
public record AuthResultDto(UserDto? User, bool IsLockedOut, bool MustChangePassword);
public record CreateUserDto(string UserName, string DisplayName, string? Email, string Password, List<string> Roles);

// ── Audit Log ─────────────────────────────────────────────────────────────────

public record AuditLogDto(
    int AuditID,
    string? UserDisplayName,
    string Action,
    string EntityName,
    int? EntityID,
    string? OldValue,
    string? NewValue,
    DateTime Timestamp
);
