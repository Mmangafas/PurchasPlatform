using Microsoft.EntityFrameworkCore;
using SmartPricing.Application.DTOs;
using SmartPricing.Application.Helpers;
using SmartPricing.Application.Interfaces;
using SmartPricing.Domain.Entities;
using SmartPricing.Domain.Enums;
using SmartPricing.Infrastructure.Data;

namespace SmartPricing.Infrastructure.Services;

// ── Master Data ───────────────────────────────────────────────────────────────

public class MasterDataService : IMasterDataService
{
    private readonly SmartPricingDbContext _db;
    public MasterDataService(SmartPricingDbContext db) => _db = db;

    public Task<List<Category>> GetCategoriesAsync() =>
        _db.Categories.OrderBy(c => c.Name).ToListAsync();

    public Task<List<Channel>> GetChannelsAsync() =>
        _db.Channels.OrderBy(c => c.Name).ToListAsync();

    public Task<List<Supplier>> GetSuppliersAsync(bool activeOnly = true) =>
        _db.Suppliers.Where(s => !activeOnly || s.IsActive).OrderBy(s => s.SupplierName).ToListAsync();

    public Task<List<Item>> GetItemsAsync(bool activeOnly = true) =>
        _db.Items.Include(i => i.Category).Where(i => !activeOnly || i.IsActive).OrderBy(i => i.ItemName).ToListAsync();

    public Task<List<RoundingRule>> GetRoundingRulesAsync() =>
        _db.RoundingRules.Where(r => r.IsActive).ToListAsync();
}

// ── Supplier Prices ───────────────────────────────────────────────────────────

public class SupplierPriceService : ISupplierPriceService
{
    private readonly SmartPricingDbContext _db;
    private readonly IAuditService _audit;
    public SupplierPriceService(SmartPricingDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    private IQueryable<SupplierPriceList> BaseQuery() =>
        _db.SupplierPriceLists
            .Include(s => s.Supplier)
            .Include(s => s.Item).ThenInclude(i => i!.Category);

    private static SupplierPriceDto Map(SupplierPriceList s) => new(
        s.SupplierPriceID, s.SupplierID, s.Supplier!.SupplierName,
        s.ItemID, s.Item!.ERPItemCode, s.Item.ItemName, s.Item.UoM,
        s.Item.Category?.Name, s.BasePrice, s.ExtraCharges, s.TotalCost,
        s.Currency, s.ValidFrom, s.ValidTo);

    public async Task<List<SupplierPriceDto>> GetAllAsync(bool activeOnly = true)
    {
        var q = BaseQuery();
        if (activeOnly) q = q.Where(s => s.ValidTo == null || s.ValidTo >= DateOnly.FromDateTime(DateTime.Today));
        return (await q.OrderByDescending(s => s.ValidFrom).ToListAsync()).Select(Map).ToList();
    }

    public async Task<List<SupplierPriceDto>> GetBySupplierAsync(int supplierID) =>
        (await BaseQuery().Where(s => s.SupplierID == supplierID).ToListAsync()).Select(Map).ToList();

    public async Task<List<SupplierPriceDto>> GetByItemAsync(int itemID) =>
        (await BaseQuery().Where(s => s.ItemID == itemID).ToListAsync()).Select(Map).ToList();

    public async Task<SupplierPriceDto?> GetCurrentAsync(int supplierID, int itemID)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var entity = await BaseQuery()
            .Where(s => s.SupplierID == supplierID && s.ItemID == itemID &&
                        s.ValidFrom <= today && (s.ValidTo == null || s.ValidTo >= today))
            .OrderByDescending(s => s.ValidFrom)
            .FirstOrDefaultAsync();
        return entity is null ? null : Map(entity);
    }

    public async Task<int> CreateAsync(CreateSupplierPriceDto dto)
    {
        var entity = new SupplierPriceList
        {
            SupplierID = dto.SupplierID, ItemID = dto.ItemID,
            BasePrice = dto.BasePrice, ExtraCharges = dto.ExtraCharges,
            Currency = dto.Currency, ValidFrom = dto.ValidFrom, ValidTo = dto.ValidTo,
            CreatedBy = dto.CreatedBy
        };
        _db.SupplierPriceLists.Add(entity);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(dto.CreatedBy, "Create", "SupplierPriceList", entity.SupplierPriceID);
        return entity.SupplierPriceID;
    }

    public async Task DeleteAsync(int supplierPriceID)
    {
        var entity = await _db.SupplierPriceLists.FindAsync(supplierPriceID);
        if (entity is not null) { _db.SupplierPriceLists.Remove(entity); await _db.SaveChangesAsync(); }
    }
}

// ── Selling Price / Calculation ───────────────────────────────────────────────

public class SellingPriceService : ISellingPriceService
{
    private readonly SmartPricingDbContext _db;
    private readonly IAuditService _audit;
    public SellingPriceService(SmartPricingDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    public async Task<List<CalculatedPriceDto>> GetAllAsync()
    {
        var list = await _db.CalculatedSellingPrices
            .Include(c => c.Item)
            .Include(c => c.Channel)
            .Include(c => c.Rule)
            .OrderBy(c => c.Item!.ItemName)
            .ToListAsync();

        return list.Select(c => new CalculatedPriceDto(
            c.CalcPriceID, c.ItemID, c.Item!.ERPItemCode, c.Item.ItemName, c.Item.UoM,
            c.ChannelID, c.Channel!.Name, c.Cost, c.GrossMarginPercent, c.MarkupPercent,
            c.SuggestedPrice, c.ColorIndicator, c.Rule?.TargetMarginPercent)).ToList();
    }

    public async Task<CalculatedPriceDto?> CalculateAsync(CalculatePriceRequest request)
    {
        var item = await _db.Items.Include(i => i.Category).FirstOrDefaultAsync(i => i.ItemID == request.ItemID);
        var channel = await _db.Channels.FindAsync(request.ChannelID);
        if (item is null || channel is null) return null;

        // Find applicable pricing rule
        PricingRule? rule = null;
        if (request.RuleID.HasValue)
            rule = await _db.PricingRules.Include(r => r.RoundingRule).FirstOrDefaultAsync(r => r.RuleID == request.RuleID);
        else
            rule = await _db.PricingRules.Include(r => r.RoundingRule)
                .Where(r => r.IsActive &&
                    (r.CategoryID == null || r.CategoryID == item.CategoryID) &&
                    (r.ChannelID == null || r.ChannelID == request.ChannelID))
                .OrderByDescending(r => r.CategoryID).ThenByDescending(r => r.ChannelID)
                .FirstOrDefaultAsync();

        decimal suggestedPrice = request.Cost;
        if (rule is not null)
        {
            if (rule.TargetMarginPercent.HasValue)
                suggestedPrice = PricingCalculator.SuggestPriceFromMargin(request.Cost, rule.TargetMarginPercent.Value);
            else if (rule.MarkupPercent.HasValue)
                suggestedPrice = PricingCalculator.SuggestPriceFromMarkup(request.Cost, rule.MarkupPercent.Value);

            if (rule.RoundingRule is not null)
                suggestedPrice = PricingCalculator.ApplyRounding(suggestedPrice, rule.RoundingRule.Pattern);
        }

        var gm = PricingCalculator.GrossMargin(suggestedPrice, request.Cost);
        var markup = PricingCalculator.Markup(suggestedPrice, request.Cost);
        var indicator = rule?.TargetMarginPercent.HasValue == true
            ? PricingCalculator.GetColorIndicator(gm, rule.TargetMarginPercent.Value)
            : ColorIndicator.Green;

        // Upsert
        var existing = await _db.CalculatedSellingPrices
            .FirstOrDefaultAsync(c => c.ItemID == request.ItemID && c.ChannelID == request.ChannelID);

        if (existing is null)
        {
            existing = new CalculatedSellingPrice { ItemID = request.ItemID, ChannelID = request.ChannelID, CreatedBy = request.CreatedBy };
            _db.CalculatedSellingPrices.Add(existing);
        }

        existing.Cost = request.Cost;
        existing.GrossMarginPercent = gm;
        existing.MarkupPercent = markup;
        existing.SuggestedPrice = suggestedPrice;
        existing.ColorIndicator = indicator;
        existing.RuleID = rule?.RuleID;
        existing.CreatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return new CalculatedPriceDto(existing.CalcPriceID, item.ItemID, item.ERPItemCode, item.ItemName, item.UoM,
            channel.ChannelID, channel.Name, request.Cost, gm, markup, suggestedPrice, indicator, rule?.TargetMarginPercent);
    }

    public async Task<List<CalculatedPriceDto>> RecalculateAllAsync(int createdBy)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var items = await _db.Items.Where(i => i.IsActive).ToListAsync();
        var channels = await _db.Channels.ToListAsync();
        var results = new List<CalculatedPriceDto>();

        foreach (var item in items)
        {
            // Get latest supplier price
            var supplierPrice = await _db.SupplierPriceLists
                .Where(s => s.ItemID == item.ItemID && s.ValidFrom <= today && (s.ValidTo == null || s.ValidTo >= today))
                .OrderByDescending(s => s.ValidFrom).FirstOrDefaultAsync();
            if (supplierPrice is null) continue;

            foreach (var channel in channels)
            {
                var dto = await CalculateAsync(new CalculatePriceRequest(item.ItemID, channel.ChannelID, supplierPrice.TotalCost, null, createdBy));
                if (dto is not null) results.Add(dto);
            }
        }
        return results;
    }
}

// ── Batch Service ─────────────────────────────────────────────────────────────

public class BatchService : IBatchService
{
    private readonly SmartPricingDbContext _db;
    private readonly IAuditService _audit;
    public BatchService(SmartPricingDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    private static BatchSummaryDto MapSummary(PriceBatch b) => new(
        b.BatchID, b.BatchName, b.Status, b.Creator?.DisplayName ?? "",
        b.CreatedAt, b.Lines.Count,
        b.Lines.Count(l => l.LineStatus == LineStatus.Approved),
        b.Lines.Count(l => l.LineStatus == LineStatus.Rejected),
        b.Lines.Count(l => l.LineStatus == LineStatus.Pending));

    private static BatchDetailDto MapDetail(PriceBatch b) => new(
        b.BatchID, b.BatchName, b.Status, b.Creator?.DisplayName ?? "",
        b.CreatedAt, b.Submitter?.DisplayName, b.SubmittedAt,
        b.Approver?.DisplayName, b.ApprovedAt,
        b.Lines.Select(l => new BatchLineDto(
            l.LineID, l.BatchID, l.ItemID, l.Item?.ERPItemCode ?? "", l.Item?.ItemName ?? "",
            l.ChannelID, l.Channel?.Name ?? "", l.OldPrice, l.NewPrice,
            l.MarginPercent, l.LineStatus, l.ReviewerComment, l.ApproverComment)).ToList());

    private IQueryable<PriceBatch> BaseQuery() =>
        _db.PriceBatches
            .Include(b => b.Creator).Include(b => b.Submitter).Include(b => b.Approver)
            .Include(b => b.Lines).ThenInclude(l => l.Item)
            .Include(b => b.Lines).ThenInclude(l => l.Channel);

    public async Task<List<BatchSummaryDto>> GetAllAsync() =>
        (await BaseQuery().OrderByDescending(b => b.CreatedAt).ToListAsync()).Select(MapSummary).ToList();

    public async Task<List<BatchSummaryDto>> GetByStatusAsync(string status) =>
        (await BaseQuery().Where(b => b.Status.ToString() == status)
            .OrderByDescending(b => b.CreatedAt).ToListAsync()).Select(MapSummary).ToList();

    public async Task<BatchDetailDto?> GetByIdAsync(int batchID)
    {
        var b = await BaseQuery().FirstOrDefaultAsync(b => b.BatchID == batchID);
        return b is null ? null : MapDetail(b);
    }

    public async Task<int> CreateAsync(CreateBatchDto dto)
    {
        var batch = new PriceBatch { BatchName = dto.BatchName, CreatedBy = dto.CreatedBy };
        _db.PriceBatches.Add(batch);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(dto.CreatedBy, "CreateBatch", "PriceBatch", batch.BatchID, null, dto.BatchName);
        return batch.BatchID;
    }

    public async Task AddLineAsync(AddBatchLineDto dto)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        // Get old price from history
        var oldPrice = await _db.PriceChangeHistories
            .Where(h => h.ItemID == dto.ItemID && h.ChannelID == dto.ChannelID)
            .OrderByDescending(h => h.ChangedAt).Select(h => (decimal?)h.NewPrice).FirstOrDefaultAsync();

        var line = new PriceBatchLine
        {
            BatchID = dto.BatchID, ItemID = dto.ItemID, ChannelID = dto.ChannelID,
            OldPrice = oldPrice, NewPrice = dto.NewPrice, CreatedBy = dto.CreatedBy
        };

        // Calculate margin if we have supplier cost
        var cost = await _db.SupplierPriceLists
            .Where(s => s.ItemID == dto.ItemID && s.ValidFrom <= today && (s.ValidTo == null || s.ValidTo >= today))
            .OrderByDescending(s => s.ValidFrom).Select(s => (decimal?)s.TotalCost).FirstOrDefaultAsync();
        if (cost.HasValue)
            line.MarginPercent = PricingCalculator.GrossMargin(dto.NewPrice, cost.Value);

        _db.PriceBatchLines.Add(line);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveLineAsync(int lineID)
    {
        var line = await _db.PriceBatchLines.FindAsync(lineID);
        if (line is not null) { _db.PriceBatchLines.Remove(line); await _db.SaveChangesAsync(); }
    }

    public async Task SubmitAsync(int batchID, int userID)
    {
        var batch = await _db.PriceBatches.FindAsync(batchID) ?? throw new InvalidOperationException("Batch not found");
        batch.Status = BatchStatus.Submitted;
        batch.SubmittedBy = userID;
        batch.SubmittedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userID, "SubmitBatch", "PriceBatch", batchID);
    }

    public async Task ApproveBatchAsync(int batchID, int approverID)
    {
        var batch = await _db.PriceBatches.Include(b => b.Lines).FirstOrDefaultAsync(b => b.BatchID == batchID)
            ?? throw new InvalidOperationException("Batch not found");

        if (batch.Lines.Any(l => l.LineStatus != LineStatus.Approved))
            throw new InvalidOperationException("Not all lines approved");

        batch.Status = BatchStatus.Approved;
        batch.ApprovedBy = approverID;
        batch.ApprovedAt = DateTime.UtcNow;

        // Write price change history
        foreach (var line in batch.Lines)
        {
            _db.PriceChangeHistories.Add(new PriceChangeHistory
            {
                ItemID = line.ItemID, ChannelID = line.ChannelID,
                OldPrice = line.OldPrice, NewPrice = line.NewPrice,
                ChangePercent = PricingCalculator.ChangePercent(line.OldPrice, line.NewPrice),
                ChangedBy = approverID
            });
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(approverID, "ApproveBatch", "PriceBatch", batchID);
    }

    public async Task ApproveLinesAsync(List<ApproveBatchLineDto> lines)
    {
        foreach (var dto in lines)
        {
            var line = await _db.PriceBatchLines.FindAsync(dto.LineID);
            if (line is null) continue;
            line.LineStatus = LineStatus.Approved;
            line.ApproverComment = dto.Comment;
        }
        await _db.SaveChangesAsync();
    }

    public async Task RejectLinesAsync(List<RejectBatchLineDto> lines)
    {
        foreach (var dto in lines)
        {
            var line = await _db.PriceBatchLines.FindAsync(dto.LineID);
            if (line is null) continue;
            line.LineStatus = LineStatus.Rejected;
            line.ApproverComment = dto.Comment;
        }
        await _db.SaveChangesAsync();
    }
}

// ── Pricing Rules ─────────────────────────────────────────────────────────────

public class PricingRuleService : IPricingRuleService
{
    private readonly SmartPricingDbContext _db;
    public PricingRuleService(SmartPricingDbContext db) => _db = db;

    private IQueryable<PricingRule> BaseQuery() =>
        _db.PricingRules.Include(r => r.Category).Include(r => r.Channel).Include(r => r.RoundingRule);

    private static PricingRuleDto Map(PricingRule r) => new(
        r.RuleID, r.CategoryID, r.Category?.Name, r.ChannelID, r.Channel?.Name,
        r.TargetMarginPercent, r.MarkupPercent, r.RoundingRuleID, r.RoundingRule?.Pattern, r.IsActive);

    public async Task<List<PricingRuleDto>> GetAllAsync() =>
        (await BaseQuery().ToListAsync()).Select(Map).ToList();

    public async Task<PricingRuleDto?> GetForItemChannelAsync(int? categoryID, int channelID)
    {
        var rule = await BaseQuery()
            .Where(r => r.IsActive && (r.CategoryID == categoryID || r.CategoryID == null) && (r.ChannelID == channelID || r.ChannelID == null))
            .OrderByDescending(r => r.CategoryID).ThenByDescending(r => r.ChannelID)
            .FirstOrDefaultAsync();
        return rule is null ? null : Map(rule);
    }

    public async Task<int> CreateAsync(CreatePricingRuleDto dto)
    {
        var rule = new PricingRule
        {
            CategoryID = dto.CategoryID, ChannelID = dto.ChannelID,
            TargetMarginPercent = dto.TargetMarginPercent, MarkupPercent = dto.MarkupPercent,
            RoundingRuleID = dto.RoundingRuleID, CreatedBy = dto.CreatedBy
        };
        _db.PricingRules.Add(rule);
        await _db.SaveChangesAsync();
        return rule.RuleID;
    }

    public async Task UpdateAsync(int ruleID, CreatePricingRuleDto dto)
    {
        var rule = await _db.PricingRules.FindAsync(ruleID) ?? throw new InvalidOperationException("Rule not found");
        rule.CategoryID = dto.CategoryID; rule.ChannelID = dto.ChannelID;
        rule.TargetMarginPercent = dto.TargetMarginPercent; rule.MarkupPercent = dto.MarkupPercent;
        rule.RoundingRuleID = dto.RoundingRuleID;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int ruleID)
    {
        var rule = await _db.PricingRules.FindAsync(ruleID);
        if (rule is not null) { _db.PricingRules.Remove(rule); await _db.SaveChangesAsync(); }
    }
}

// ── ERP Integration ───────────────────────────────────────────────────────────

public class ErpIntegrationService : IErpIntegrationService
{
    private readonly SmartPricingDbContext _db;
    private readonly IAuditService _audit;
    public ErpIntegrationService(SmartPricingDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    public async Task<SyncResultDto> ImportItemsAsync(List<ErpItemDto> items)
    {
        int count = 0;
        foreach (var dto in items)
        {
            var existing = await _db.Items.FirstOrDefaultAsync(i => i.ERPItemCode == dto.ErpItemCode);
            if (existing is null)
            {
                // Resolve category
                int? catID = null;
                if (dto.CategoryCode is not null)
                {
                    var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Code == dto.CategoryCode);
                    if (cat is null) { cat = new Category { Code = dto.CategoryCode, Name = dto.CategoryCode }; _db.Categories.Add(cat); await _db.SaveChangesAsync(); }
                    catID = cat.CategoryID;
                }
                _db.Items.Add(new Item { ERPItemCode = dto.ErpItemCode, ItemName = dto.Name, CategoryID = catID, UoM = dto.Uom, IsActive = dto.IsActive });
            }
            else { existing.ItemName = dto.Name; existing.IsActive = dto.IsActive; }
            count++;
        }
        await _db.SaveChangesAsync();
        await LogSync("ItemsImport", "Items", "Success", $"{count} items processed");
        return new SyncResultDto(true, $"Imported {count} items", count);
    }

    public async Task<SyncResultDto> ImportSuppliersAsync(List<ErpSupplierDto> suppliers)
    {
        int count = 0;
        foreach (var dto in suppliers)
        {
            var existing = await _db.Suppliers.FirstOrDefaultAsync(s => s.ERPSupplierCode == dto.ErpSupplierCode);
            if (existing is null) _db.Suppliers.Add(new Supplier { ERPSupplierCode = dto.ErpSupplierCode, SupplierName = dto.Name, IsActive = dto.IsActive });
            else { existing.SupplierName = dto.Name; existing.IsActive = dto.IsActive; }
            count++;
        }
        await _db.SaveChangesAsync();
        await LogSync("SuppliersImport", "Suppliers", "Success", $"{count} suppliers processed");
        return new SyncResultDto(true, $"Imported {count} suppliers", count);
    }

    public async Task<SyncResultDto> ImportPurchasePricesAsync(List<ErpPurchasePriceDto> prices)
    {
        int count = 0;
        foreach (var dto in prices)
        {
            var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.ERPSupplierCode == dto.ErpSupplierCode);
            var item = await _db.Items.FirstOrDefaultAsync(i => i.ERPItemCode == dto.ErpItemCode);
            if (supplier is null || item is null) continue;

            _db.SupplierPriceLists.Add(new SupplierPriceList
            {
                SupplierID = supplier.SupplierID, ItemID = item.ItemID,
                BasePrice = dto.BasePrice, ExtraCharges = dto.ExtraCharges,
                Currency = dto.Currency, ValidFrom = dto.ValidFrom, ValidTo = dto.ValidTo
            });
            count++;
        }
        await _db.SaveChangesAsync();
        await LogSync("PurchasePricesImport", "SupplierPriceList", "Success", $"{count} prices processed");
        return new SyncResultDto(true, $"Imported {count} purchase prices", count);
    }

    public async Task<List<ErpSellingPriceExportDto>> ExportApprovedSellingPricesAsync()
    {
        var lines = await _db.PriceBatchLines
            .Include(l => l.Item).Include(l => l.Channel).Include(l => l.Batch)
            .Where(l => l.LineStatus == LineStatus.Approved && l.Batch!.Status == BatchStatus.Approved)
            .ToListAsync();

        return lines.Select(l => new ErpSellingPriceExportDto(
            l.Item!.ERPItemCode, l.Channel!.Code, l.NewPrice,
            DateOnly.FromDateTime(l.Batch!.ApprovedAt ?? DateTime.Today), l.BatchID)).ToList();
    }

    public Task<List<ERPSyncLog>> GetSyncLogsAsync(int count = 50) =>
        _db.ERPSyncLogs.OrderByDescending(l => l.CreatedAt).Take(count).ToListAsync();

    private async Task LogSync(string syncType, string entity, string status, string message)
    {
        _db.ERPSyncLogs.Add(new ERPSyncLog { SyncType = syncType, Entity = entity, Status = status, Message = message });
        await _db.SaveChangesAsync();
    }
}

// ── Dashboard ─────────────────────────────────────────────────────────────────

public class DashboardService : IDashboardService
{
    private readonly SmartPricingDbContext _db;
    public DashboardService(SmartPricingDbContext db) => _db = db;

    public async Task<DashboardKpiDto> GetKpisAsync()
    {
        var today = DateTime.UtcNow.Date;
        var totalBatches = await _db.PriceBatches.CountAsync();
        var pendingApproval = await _db.PriceBatches.CountAsync(b => b.Status == BatchStatus.Submitted);
        var approvedToday = await _db.PriceBatches.CountAsync(b => b.Status == BatchStatus.Approved && b.ApprovedAt >= today);

        var prices = await _db.CalculatedSellingPrices.Include(c => c.Item).Include(c => c.Channel).Include(c => c.Rule).ToListAsync();
        var belowTarget = prices.Count(c => c.ColorIndicator == ColorIndicator.Red);
        var avgMargin = prices.Any() ? Math.Round(prices.Average(c => c.GrossMarginPercent), 2) : 0;

        var recentBatches = await _db.PriceBatches.Include(b => b.Creator)
            .OrderByDescending(b => b.CreatedAt).Take(5)
            .Select(b => new RecentBatchDto(b.BatchID, b.BatchName, b.Status, b.CreatedAt, b.Creator!.DisplayName))
            .ToListAsync();

        var alerts = prices.Where(c => c.ColorIndicator != ColorIndicator.Green)
            .Select(c => new MarginAlertDto(c.Item!.ItemName, c.Channel!.Name, c.GrossMarginPercent, c.Rule?.TargetMarginPercent ?? 0, c.ColorIndicator))
            .OrderBy(a => a.MarginPercent).Take(10).ToList();

        return new DashboardKpiDto(totalBatches, pendingApproval, approvedToday, belowTarget, avgMargin, recentBatches, alerts);
    }
}

// ── User Service ──────────────────────────────────────────────────────────────

public class UserService : IUserService
{
    private readonly SmartPricingDbContext _db;
    public UserService(SmartPricingDbContext db) => _db = db;

    private static UserDto Map(User u) => new(u.UserID, u.UserName, u.DisplayName, u.Email,
        u.UserRoles.Select(ur => ur.Role!.RoleName).ToList());

    private IQueryable<User> BaseQuery() => _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

    public async Task<UserDto?> AuthenticateAsync(LoginDto dto)
    {
        var user = await BaseQuery().FirstOrDefaultAsync(u => u.UserName == dto.UserName && u.IsActive);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) return null;
        return Map(user);
    }

    public async Task<UserDto?> GetByIdAsync(int userID)
    {
        var user = await BaseQuery().FirstOrDefaultAsync(u => u.UserID == userID);
        return user is null ? null : Map(user);
    }

    public async Task<List<UserDto>> GetAllAsync() =>
        (await BaseQuery().Where(u => u.IsActive).ToListAsync()).Select(Map).ToList();

    public async Task<int> CreateAsync(CreateUserDto dto)
    {
        var user = new User
        {
            UserName = dto.UserName, DisplayName = dto.DisplayName, Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await UpdateRolesAsync(user.UserID, dto.Roles);
        return user.UserID;
    }

    public async Task UpdateRolesAsync(int userID, List<string> roles)
    {
        var existing = _db.UserRoles.Where(ur => ur.UserID == userID);
        _db.UserRoles.RemoveRange(existing);
        foreach (var roleName in roles)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role is not null) _db.UserRoles.Add(new UserRole { UserID = userID, RoleID = role.RoleID });
        }
        await _db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int userID)
    {
        var user = await _db.Users.FindAsync(userID);
        if (user is not null) { user.IsActive = false; await _db.SaveChangesAsync(); }
    }
}

// ── Audit Service ─────────────────────────────────────────────────────────────

public class AuditService : IAuditService
{
    private readonly SmartPricingDbContext _db;
    public AuditService(SmartPricingDbContext db) => _db = db;

    public async Task LogAsync(int? userID, string action, string entityName, int? entityID, string? oldValue = null, string? newValue = null)
    {
        _db.AuditLogs.Add(new AuditLog { UserID = userID, Action = action, EntityName = entityName, EntityID = entityID, OldValue = oldValue, NewValue = newValue });
        await _db.SaveChangesAsync();
    }

    public async Task<List<AuditLogDto>> GetRecentAsync(int count = 100)
    {
        var logs = await _db.AuditLogs.Include(a => a.User)
            .OrderByDescending(a => a.Timestamp).Take(count).ToListAsync();
        return logs.Select(a => new AuditLogDto(a.AuditID, a.User?.DisplayName, a.Action, a.EntityName, a.EntityID, a.OldValue, a.NewValue, a.Timestamp)).ToList();
    }

    public async Task<List<AuditLogDto>> GetByEntityAsync(string entityName, int entityID)
    {
        var logs = await _db.AuditLogs.Include(a => a.User)
            .Where(a => a.EntityName == entityName && a.EntityID == entityID)
            .OrderByDescending(a => a.Timestamp).ToListAsync();
        return logs.Select(a => new AuditLogDto(a.AuditID, a.User?.DisplayName, a.Action, a.EntityName, a.EntityID, a.OldValue, a.NewValue, a.Timestamp)).ToList();
    }
}
