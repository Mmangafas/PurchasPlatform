using SmartPricing.Application.DTOs;
using SmartPricing.Domain.Entities;

namespace SmartPricing.Application.Interfaces;

/// <summary>The signed-in user for the current request or circuit.</summary>
public interface ICurrentUser
{
    int UserID { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

public interface ISupplierPriceService
{
    Task<List<SupplierPriceDto>> GetAllAsync(bool activeOnly = true);
    Task<List<SupplierPriceDto>> GetBySupplierAsync(int supplierID);
    Task<List<SupplierPriceDto>> GetByItemAsync(int itemID);
    Task<SupplierPriceDto?> GetCurrentAsync(int supplierID, int itemID);
    Task<int> CreateAsync(CreateSupplierPriceDto dto);
    Task DeleteAsync(int supplierPriceID);
}

public interface ISellingPriceService
{
    Task<List<CalculatedPriceDto>> GetAllAsync();
    Task<CalculatedPriceDto?> CalculateAsync(CalculatePriceRequest request);
    Task<List<CalculatedPriceDto>> RecalculateAllAsync(int createdBy);
}

public interface IBatchService
{
    Task<List<BatchSummaryDto>> GetAllAsync();
    Task<List<BatchSummaryDto>> GetByStatusAsync(string status);
    Task<BatchDetailDto?> GetByIdAsync(int batchID);
    Task<int> CreateAsync(CreateBatchDto dto);
    Task AddLineAsync(AddBatchLineDto dto);
    Task RemoveLineAsync(int lineID);
    Task SubmitAsync(int batchID, int userID);
    Task ApproveBatchAsync(int batchID, int approverID);
    Task ApproveLinesAsync(List<ApproveBatchLineDto> lines);
    Task RejectLinesAsync(List<RejectBatchLineDto> lines);
    Task RejectBatchAsync(int batchID, int approverID);
}

public interface IPricingRuleService
{
    Task<List<PricingRuleDto>> GetAllAsync();
    Task<PricingRuleDto?> GetForItemChannelAsync(int? categoryID, int channelID);
    Task<int> CreateAsync(CreatePricingRuleDto dto);
    Task UpdateAsync(int ruleID, CreatePricingRuleDto dto);
    Task DeleteAsync(int ruleID);
}

public interface IErpIntegrationService
{
    Task<SyncResultDto> ImportItemsAsync(List<ErpItemDto> items);
    Task<SyncResultDto> ImportSuppliersAsync(List<ErpSupplierDto> suppliers);
    Task<SyncResultDto> ImportPurchasePricesAsync(List<ErpPurchasePriceDto> prices);
    Task<List<ErpSellingPriceExportDto>> ExportApprovedSellingPricesAsync();
    Task<List<ERPSyncLog>> GetSyncLogsAsync(int count = 50);
}

public interface IDashboardService
{
    Task<DashboardKpiDto> GetKpisAsync();
}

public interface IUserService
{
    Task<AuthResultDto> AuthenticateAsync(LoginDto dto);
    Task<bool> ChangePasswordAsync(int userID, string currentPassword, string newPassword);
    Task<UserDto?> GetByIdAsync(int userID);
    Task<List<UserDto>> GetAllAsync();
    Task<int> CreateAsync(CreateUserDto dto);
    Task UpdateRolesAsync(int userID, List<string> roles);
    Task DeactivateAsync(int userID);
}

public interface IAuditService
{
    Task LogAsync(int? userID, string action, string entityName, int? entityID, string? oldValue = null, string? newValue = null);
    Task<List<AuditLogDto>> GetRecentAsync(int count = 100);
    Task<List<AuditLogDto>> GetByEntityAsync(string entityName, int entityID);
}

public interface IMasterDataService
{
    Task<List<Category>> GetCategoriesAsync();
    Task<List<Channel>> GetChannelsAsync();
    Task<List<Supplier>> GetSuppliersAsync(bool activeOnly = true);
    Task<List<Item>> GetItemsAsync(bool activeOnly = true);
    Task<List<RoundingRule>> GetRoundingRulesAsync();
}
