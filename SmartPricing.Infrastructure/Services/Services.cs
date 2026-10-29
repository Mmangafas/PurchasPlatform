using Microsoft.EntityFrameworkCore;
using SmartPricing.Application.DTOs;
using SmartPricing.Application.Helpers;
using SmartPricing.Application.Interfaces;
using SmartPricing.Domain.Entities;
using SmartPricing.Domain.Enums;
using SmartPricing.Infrastructure.Data;

namespace SmartPricing.Infrastructure.Services;

// ── Authorization guard ───────────────────────────────────────────────────────
// Mirrors the policies in Program.cs so services stay safe even if a UI check is missed.

internal static class Roles
{
    public static readonly string[] Requester = ["Requester", "Reviewer", "Approver", "Admin"];
    public static readonly string[] Approver = ["Approver", "Admin"];
    public static readonly string[] Admin = ["Admin"];
    public static readonly string[] Erp = ["Admin", SystemRoles.ErpIntegration];

    public static void Require(this ICurrentUser user, string[] roles)
    {
        if (!user.IsAuthenticated || !roles.Any(user.IsInRole))
            throw new UnauthorizedAccessException("Δεν έχετε δικαίωμα για αυτή την ενέργεια / Not authorized.");
    }
}

internal static class DbContextExtensions
{
    /// <summary>
    /// Runs work in one transaction, so a new row (whose ID is only known after saving)
    /// and its audit entry are committed together. Retry-safe under an execution strategy.
    /// </summary>
    public static Task InTransactionAsync(this DbContext db, Func<Task> work) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await work();
            await tx.CommitAsync();
        });
}

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
    private readonly ICurrentUser _user;
    public SupplierPriceService(SmartPricingDbContext db, IAuditService audit, ICurrentUser user) { _db = db; _audit = audit; _user = user; }

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
        var today = BusinessClock.Today;
        if (activeOnly) q = q.Where(s => s.ValidTo == null || s.ValidTo >= today);
        return (await q.OrderByDescending(s => s.ValidFrom).ToListAsync()).Select(Map).ToList();
    }

    public async Task<List<SupplierPriceDto>> GetBySupplierAsync(int supplierID) =>
        (await BaseQuery().Where(s => s.SupplierID == supplierID).ToListAsync()).Select(Map).ToList();

    public async Task<List<SupplierPriceDto>> GetByItemAsync(int itemID) =>
        (await BaseQuery().Where(s => s.ItemID == itemID).ToListAsync()).Select(Map).ToList();

    public async Task<SupplierPriceDto?> GetCurrentAsync(int supplierID, int itemID)
    {
        var today = BusinessClock.Today;
        var entity = await BaseQuery()
            .Where(s => s.SupplierID == supplierID && s.ItemID == itemID &&
                        s.ValidFrom <= today && (s.ValidTo == null || s.ValidTo >= today))
            .OrderByDescending(s => s.ValidFrom)
            .FirstOrDefaultAsync();
        return entity is null ? null : Map(entity);
    }

    public async Task<int> CreateAsync(CreateSupplierPriceDto dto)
    {
        _user.Require(Roles.Requester);
        var entity = new SupplierPriceList
        {
            SupplierID = dto.SupplierID, ItemID = dto.ItemID,
            BasePrice = dto.BasePrice, ExtraCharges = dto.ExtraCharges,
            Currency = dto.Currency, ValidFrom = dto.ValidFrom, ValidTo = dto.ValidTo,
            CreatedBy = _user.UserID
        };
        await _db.InTransactionAsync(async () =>
        {
            _db.SupplierPriceLists.Add(entity);
            await _db.SaveChangesAsync();
            _audit.Log(_user.UserID, "Create", "SupplierPriceList", entity.SupplierPriceID, null, Describe(entity));
            await _db.SaveChangesAsync();
        });
        return entity.SupplierPriceID;
    }

    public async Task DeleteAsync(int supplierPriceID)
    {
        _user.Require(Roles.Requester);
        var entity = await _db.SupplierPriceLists.FindAsync(supplierPriceID);
        if (entity is null) return;
        _db.SupplierPriceLists.Remove(entity);
        _audit.Log(_user.UserID, "Delete", "SupplierPriceList", supplierPriceID, Describe(entity));
        await _db.SaveChangesAsync();
    }

    private static string Describe(SupplierPriceList s) =>
        $"{s.SupplierID}/{s.ItemID} {s.BasePrice}+{s.ExtraCharges} {s.ValidFrom:yyyy-MM-dd}";
}

// ── Selling Price / Calculation ───────────────────────────────────────────────

public class SellingPriceService : ISellingPriceService
{
    private readonly SmartPricingDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;
    public SellingPriceService(SmartPricingDbContext db, IAuditService audit, ICurrentUser user) { _db = db; _audit = audit; _user = user; }

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
        _user.Require(Roles.Requester);
        var item = await _db.Items.Include(i => i.Category).FirstOrDefaultAsync(i => i.ItemID == request.ItemID);
        var channel = await _db.Channels.FindAsync(request.ChannelID);
        if (item is null || channel is null) return null;

        var rule = request.RuleID.HasValue
            ? await _db.PricingRules.Include(r => r.RoundingRule).FirstOrDefaultAsync(r => r.RuleID == request.RuleID)
            : FindRule(await ActiveRulesAsync(), item.CategoryID, request.ChannelID);

        var existing = await _db.CalculatedSellingPrices
            .FirstOrDefaultAsync(c => c.ItemID == request.ItemID && c.ChannelID == request.ChannelID);
        var calc = Upsert(existing, item.ItemID, channel.ChannelID, request.Cost, rule);

        await _db.SaveChangesAsync();
        return ToDto(calc, item, channel, rule);
    }

    public async Task<List<CalculatedPriceDto>> RecalculateAllAsync(int createdBy)
    {
        _user.Require(Roles.Requester);
        var today = BusinessClock.Today;

        // Load everything once, calculate in memory, save once.
        var items = await _db.Items.Where(i => i.IsActive).ToListAsync();
        var channels = await _db.Channels.ToListAsync();
        var rules = await ActiveRulesAsync();

        // Latest valid supplier cost per item
        var costs = await _db.SupplierPriceLists
            .Where(s => s.ValidFrom <= today && (s.ValidTo == null || s.ValidTo >= today))
            .GroupBy(s => s.ItemID)
            .Select(g => g.OrderByDescending(s => s.ValidFrom).ThenByDescending(s => s.SupplierPriceID)
                .Select(s => new { s.ItemID, Cost = s.BasePrice + s.ExtraCharges }).First())
            .ToDictionaryAsync(x => x.ItemID, x => x.Cost);

        var existing = (await _db.CalculatedSellingPrices.ToListAsync())
            .GroupBy(c => (c.ItemID, c.ChannelID))
            .ToDictionary(g => g.Key, g => g.First());

        var calculated = new List<(CalculatedSellingPrice Calc, Item Item, Channel Channel, PricingRule? Rule)>();
        foreach (var item in items)
        {
            if (!costs.TryGetValue(item.ItemID, out var cost)) continue;
            foreach (var channel in channels)
            {
                var rule = FindRule(rules, item.CategoryID, channel.ChannelID);
                var calc = Upsert(existing.GetValueOrDefault((item.ItemID, channel.ChannelID)), item.ItemID, channel.ChannelID, cost, rule);
                calculated.Add((calc, item, channel, rule));
            }
        }

        await _db.SaveChangesAsync();
        return calculated.Select(c => ToDto(c.Calc, c.Item, c.Channel, c.Rule)).ToList();
    }

    private Task<List<PricingRule>> ActiveRulesAsync() =>
        _db.PricingRules.Include(r => r.RoundingRule).Where(r => r.IsActive).ToListAsync();

    // The most specific rule wins: category + channel, then category, then channel, then global.
    private static PricingRule? FindRule(IEnumerable<PricingRule> rules, int? categoryID, int channelID) =>
        rules.Where(r => r.IsActive &&
                (r.CategoryID == null || r.CategoryID == categoryID) &&
                (r.ChannelID == null || r.ChannelID == channelID))
            .OrderByDescending(r => r.CategoryID.HasValue).ThenByDescending(r => r.ChannelID.HasValue)
            .FirstOrDefault();

    /// <summary>Applies the rule to the cost and writes the result into a new or existing (tracked) row.</summary>
    private CalculatedSellingPrice Upsert(CalculatedSellingPrice? existing, int itemID, int channelID, decimal cost, PricingRule? rule)
    {
        decimal suggestedPrice = cost;
        if (rule is not null)
        {
            if (rule.TargetMarginPercent.HasValue)
                suggestedPrice = PricingCalculator.SuggestPriceFromMargin(cost, rule.TargetMarginPercent.Value);
            else if (rule.MarkupPercent.HasValue)
                suggestedPrice = PricingCalculator.SuggestPriceFromMarkup(cost, rule.MarkupPercent.Value);

            if (rule.RoundingRule is not null)
                suggestedPrice = PricingCalculator.ApplyRounding(suggestedPrice, rule.RoundingRule.Pattern);
        }

        var gm = PricingCalculator.GrossMargin(suggestedPrice, cost);

        if (existing is null)
        {
            existing = new CalculatedSellingPrice { ItemID = itemID, ChannelID = channelID, CreatedBy = _user.UserID };
            _db.CalculatedSellingPrices.Add(existing);
        }

        existing.Cost = cost;
        existing.GrossMarginPercent = gm;
        existing.MarkupPercent = PricingCalculator.Markup(suggestedPrice, cost);
        existing.SuggestedPrice = suggestedPrice;
        existing.ColorIndicator = rule?.TargetMarginPercent.HasValue == true
            ? PricingCalculator.GetColorIndicator(gm, rule.TargetMarginPercent.Value)
            : ColorIndicator.Green;
        existing.RuleID = rule?.RuleID;
        existing.CreatedAt = DateTime.UtcNow;
        return existing;
    }

    private static CalculatedPriceDto ToDto(CalculatedSellingPrice c, Item item, Channel channel, PricingRule? rule) => new(
        c.CalcPriceID, item.ItemID, item.ERPItemCode, item.ItemName, item.UoM,
        channel.ChannelID, channel.Name, c.Cost, c.GrossMarginPercent, c.MarkupPercent,
        c.SuggestedPrice, c.ColorIndicator, rule?.TargetMarginPercent);
}

// ── Batch Service ─────────────────────────────────────────────────────────────

public class BatchService : IBatchService
{
    private readonly SmartPricingDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;
    public BatchService(SmartPricingDbContext db, IAuditService audit, ICurrentUser user) { _db = db; _audit = audit; _user = user; }

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

    // Draft and Rejected batches can be edited and (re)submitted.
    private static bool IsEditable(BatchStatus status) => status is BatchStatus.Draft or BatchStatus.Rejected;

    private async Task<PriceBatch> GetEditableBatchAsync(int batchID)
    {
        var batch = await _db.PriceBatches.FindAsync(batchID) ?? throw new InvalidOperationException("Batch not found");
        if (!IsEditable(batch.Status))
            throw new InvalidOperationException("Το batch δεν είναι επεξεργάσιμο / Batch can only be edited while Draft or Rejected.");
        return batch;
    }

    private async Task<List<PriceBatchLine>> GetPendingLinesOfSubmittedBatchesAsync(IEnumerable<int> lineIDs)
    {
        var ids = lineIDs.ToList();
        var lines = await _db.PriceBatchLines.Include(l => l.Batch).Where(l => ids.Contains(l.LineID)).ToListAsync();
        if (lines.Any(l => l.Batch!.Status != BatchStatus.Submitted))
            throw new InvalidOperationException("Μόνο γραμμές υποβεβλημένων batches / Only lines of submitted batches can be approved or rejected.");
        return lines.Where(l => l.LineStatus == LineStatus.Pending).ToList();
    }

    public async Task<List<BatchSummaryDto>> GetAllAsync() =>
        (await BaseQuery().OrderByDescending(b => b.CreatedAt).ToListAsync()).Select(MapSummary).ToList();

    public async Task<List<BatchSummaryDto>> GetByStatusAsync(string status)
    {
        if (!Enum.TryParse<BatchStatus>(status, true, out var parsed)) return [];
        return (await BaseQuery().Where(b => b.Status == parsed)
            .OrderByDescending(b => b.CreatedAt).ToListAsync()).Select(MapSummary).ToList();
    }

    public async Task<BatchDetailDto?> GetByIdAsync(int batchID)
    {
        var b = await BaseQuery().FirstOrDefaultAsync(b => b.BatchID == batchID);
        return b is null ? null : MapDetail(b);
    }

    public async Task<int> CreateAsync(CreateBatchDto dto)
    {
        _user.Require(Roles.Requester);
        var batch = new PriceBatch { BatchName = dto.BatchName, CreatedBy = _user.UserID };
        await _db.InTransactionAsync(async () =>
        {
            _db.PriceBatches.Add(batch);
            await _db.SaveChangesAsync();
            _audit.Log(_user.UserID, "CreateBatch", "PriceBatch", batch.BatchID, null, dto.BatchName);
            await _db.SaveChangesAsync();
        });
        return batch.BatchID;
    }

    public async Task AddLineAsync(AddBatchLineDto dto)
    {
        _user.Require(Roles.Requester);
        if (dto.NewPrice <= 0) throw new ArgumentException("Η νέα τιμή πρέπει να είναι > 0 / New price must be greater than 0.");
        await GetEditableBatchAsync(dto.BatchID);

        if (await _db.PriceBatchLines.AnyAsync(l => l.BatchID == dto.BatchID && l.ItemID == dto.ItemID && l.ChannelID == dto.ChannelID))
            throw new InvalidOperationException("Το είδος υπάρχει ήδη για αυτό το κανάλι / This item is already in the batch for this channel.");

        var today = BusinessClock.Today;
        // Get old price from history
        var oldPrice = await _db.PriceChangeHistories
            .Where(h => h.ItemID == dto.ItemID && h.ChannelID == dto.ChannelID)
            .OrderByDescending(h => h.ChangedAt).Select(h => (decimal?)h.NewPrice).FirstOrDefaultAsync();

        var line = new PriceBatchLine
        {
            BatchID = dto.BatchID, ItemID = dto.ItemID, ChannelID = dto.ChannelID,
            OldPrice = oldPrice, NewPrice = dto.NewPrice, CreatedBy = _user.UserID
        };

        // Calculate margin if we have supplier cost (TotalCost is not mapped, so compute it in SQL)
        var cost = await _db.SupplierPriceLists
            .Where(s => s.ItemID == dto.ItemID && s.ValidFrom <= today && (s.ValidTo == null || s.ValidTo >= today))
            .OrderByDescending(s => s.ValidFrom).Select(s => (decimal?)(s.BasePrice + s.ExtraCharges)).FirstOrDefaultAsync();
        if (cost.HasValue)
            line.MarginPercent = PricingCalculator.GrossMargin(dto.NewPrice, cost.Value);

        _db.PriceBatchLines.Add(line);
        _audit.Log(_user.UserID, "AddLine", "PriceBatch", dto.BatchID, null, $"Item={dto.ItemID} Ch={dto.ChannelID} {dto.NewPrice}");
        await _db.SaveChangesAsync();
    }

    public async Task RemoveLineAsync(int lineID)
    {
        _user.Require(Roles.Requester);
        var line = await _db.PriceBatchLines.FindAsync(lineID);
        if (line is null) return;
        await GetEditableBatchAsync(line.BatchID);
        _db.PriceBatchLines.Remove(line);
        _audit.Log(_user.UserID, "RemoveLine", "PriceBatch", line.BatchID, $"Item={line.ItemID} Ch={line.ChannelID} {line.NewPrice}");
        await _db.SaveChangesAsync();
    }

    public async Task SubmitAsync(int batchID, int userID)
    {
        _user.Require(Roles.Requester);
        var batch = await GetEditableBatchAsync(batchID);
        var lines = await _db.PriceBatchLines.Where(l => l.BatchID == batchID).ToListAsync();
        if (lines.Count == 0)
            throw new InvalidOperationException("Το batch δεν έχει γραμμές / Batch has no lines.");

        // On resubmission every line goes back for review
        foreach (var line in lines) { line.LineStatus = LineStatus.Pending; line.ApproverComment = null; }

        batch.Status = BatchStatus.Submitted;
        batch.SubmittedBy = _user.UserID;
        batch.SubmittedAt = DateTime.UtcNow;
        batch.ApprovedBy = null;
        batch.ApprovedAt = null;
        _audit.Log(_user.UserID, "SubmitBatch", "PriceBatch", batchID);
        await _db.SaveChangesAsync();
    }

    public async Task ApproveBatchAsync(int batchID, int approverID)
    {
        _user.Require(Roles.Approver);
        var batch = await _db.PriceBatches.Include(b => b.Lines).FirstOrDefaultAsync(b => b.BatchID == batchID)
            ?? throw new InvalidOperationException("Batch not found");

        if (batch.Status != BatchStatus.Submitted)
            throw new InvalidOperationException("Μόνο υποβεβλημένα batches εγκρίνονται / Only submitted batches can be approved.");
        if (batch.Lines.Count == 0)
            throw new InvalidOperationException("Το batch δεν έχει γραμμές / Batch has no lines.");
        if (batch.Lines.Any(l => l.LineStatus != LineStatus.Approved))
            throw new InvalidOperationException("Not all lines approved");

        batch.Status = BatchStatus.Approved;
        batch.ApprovedBy = _user.UserID;
        batch.ApprovedAt = DateTime.UtcNow;

        // Write price change history
        foreach (var line in batch.Lines)
        {
            _db.PriceChangeHistories.Add(new PriceChangeHistory
            {
                ItemID = line.ItemID, ChannelID = line.ChannelID,
                OldPrice = line.OldPrice, NewPrice = line.NewPrice,
                ChangePercent = PricingCalculator.ChangePercent(line.OldPrice, line.NewPrice),
                ChangedBy = _user.UserID
            });
        }

        _audit.Log(_user.UserID, "ApproveBatch", "PriceBatch", batchID);
        await _db.SaveChangesAsync();
    }

    public async Task RejectBatchAsync(int batchID, int approverID)
    {
        _user.Require(Roles.Approver);
        var batch = await _db.PriceBatches.FindAsync(batchID) ?? throw new InvalidOperationException("Batch not found");
        if (batch.Status != BatchStatus.Submitted)
            throw new InvalidOperationException("Μόνο υποβεβλημένα batches απορρίπτονται / Only submitted batches can be rejected.");

        batch.Status = BatchStatus.Rejected;
        _audit.Log(_user.UserID, "RejectBatch", "PriceBatch", batchID);
        await _db.SaveChangesAsync();
    }

    public async Task ApproveLinesAsync(List<ApproveBatchLineDto> lines)
    {
        _user.Require(Roles.Approver);
        var comments = lines.ToDictionary(l => l.LineID, l => l.Comment);
        foreach (var line in await GetPendingLinesOfSubmittedBatchesAsync(comments.Keys))
        {
            line.LineStatus = LineStatus.Approved;
            line.ApproverComment = comments[line.LineID];
            _audit.Log(_user.UserID, "ApproveLine", "PriceBatchLine", line.LineID, null, line.NewPrice.ToString());
        }
        await _db.SaveChangesAsync();
    }

    public async Task RejectLinesAsync(List<RejectBatchLineDto> lines)
    {
        _user.Require(Roles.Approver);
        var comments = lines.ToDictionary(l => l.LineID, l => l.Comment);
        foreach (var line in await GetPendingLinesOfSubmittedBatchesAsync(comments.Keys))
        {
            line.LineStatus = LineStatus.Rejected;
            line.ApproverComment = comments[line.LineID];
            _audit.Log(_user.UserID, "RejectLine", "PriceBatchLine", line.LineID, null, line.NewPrice.ToString());
        }
        await _db.SaveChangesAsync();
    }
}

// ── Pricing Rules ─────────────────────────────────────────────────────────────

public class PricingRuleService : IPricingRuleService
{
    private readonly SmartPricingDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;
    public PricingRuleService(SmartPricingDbContext db, IAuditService audit, ICurrentUser user) { _db = db; _audit = audit; _user = user; }

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
        _user.Require(Roles.Approver);
        var rule = new PricingRule
        {
            CategoryID = dto.CategoryID, ChannelID = dto.ChannelID,
            TargetMarginPercent = dto.TargetMarginPercent, MarkupPercent = dto.MarkupPercent,
            RoundingRuleID = dto.RoundingRuleID, CreatedBy = _user.UserID
        };
        await _db.InTransactionAsync(async () =>
        {
            _db.PricingRules.Add(rule);
            await _db.SaveChangesAsync();
            _audit.Log(_user.UserID, "Create", "PricingRule", rule.RuleID, null, Describe(rule));
            await _db.SaveChangesAsync();
        });
        return rule.RuleID;
    }

    public async Task UpdateAsync(int ruleID, CreatePricingRuleDto dto)
    {
        _user.Require(Roles.Approver);
        var rule = await _db.PricingRules.FindAsync(ruleID) ?? throw new InvalidOperationException("Rule not found");
        var before = Describe(rule);
        rule.CategoryID = dto.CategoryID; rule.ChannelID = dto.ChannelID;
        rule.TargetMarginPercent = dto.TargetMarginPercent; rule.MarkupPercent = dto.MarkupPercent;
        rule.RoundingRuleID = dto.RoundingRuleID;
        _audit.Log(_user.UserID, "Update", "PricingRule", ruleID, before, Describe(rule));
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int ruleID)
    {
        _user.Require(Roles.Approver);
        var rule = await _db.PricingRules.FindAsync(ruleID);
        if (rule is null) return;
        _db.PricingRules.Remove(rule);
        _audit.Log(_user.UserID, "Delete", "PricingRule", ruleID, Describe(rule));
        await _db.SaveChangesAsync();
    }

    private static string Describe(PricingRule r) =>
        $"Cat={r.CategoryID?.ToString() ?? "*"} Ch={r.ChannelID?.ToString() ?? "*"} GM={r.TargetMarginPercent} MU={r.MarkupPercent} Round={r.RoundingRuleID}";
}

// ── ERP Integration ───────────────────────────────────────────────────────────

public class ErpIntegrationService : IErpIntegrationService
{
    private readonly SmartPricingDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;
    public ErpIntegrationService(SmartPricingDbContext db, IAuditService audit, ICurrentUser user) { _db = db; _audit = audit; _user = user; }

    // Imports validate every row, report skipped rows instead of silently dropping them,
    // load lookups once and save everything (including the sync log) in one SaveChanges.

    public async Task<SyncResultDto> ImportItemsAsync(List<ErpItemDto> items)
    {
        _user.Require(Roles.Erp);
        var errors = new List<string>();
        var codes = items.Select(i => i.ErpItemCode).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        var existing = await _db.Items.Where(i => codes.Contains(i.ERPItemCode)).ToDictionaryAsync(i => i.ERPItemCode);
        var categories = (await _db.Categories.ToListAsync())
            .GroupBy(c => c.Code).ToDictionary(g => g.Key, g => g.First());
        var seen = new HashSet<string>();
        int count = 0;

        for (var row = 0; row < items.Count; row++)
        {
            var dto = items[row];
            if (string.IsNullOrWhiteSpace(dto.ErpItemCode) || string.IsNullOrWhiteSpace(dto.Name))
            { errors.Add($"row {row + 1}: code and name are required"); continue; }
            if (!seen.Add(dto.ErpItemCode))
            { errors.Add($"{dto.ErpItemCode}: duplicate in payload"); continue; }

            Category? category = null;
            if (!string.IsNullOrWhiteSpace(dto.CategoryCode) && !categories.TryGetValue(dto.CategoryCode, out category))
            {
                category = new Category { Code = dto.CategoryCode, Name = dto.CategoryCode };
                categories[dto.CategoryCode] = category;
                _db.Categories.Add(category);
            }

            if (!existing.TryGetValue(dto.ErpItemCode, out var item))
            {
                item = new Item { ERPItemCode = dto.ErpItemCode };
                _db.Items.Add(item);
            }
            item.ItemName = dto.Name;
            item.UoM = dto.Uom ?? item.UoM;
            item.Category = category;
            if (category is null) item.CategoryID = null;
            item.IsActive = dto.IsActive;
            count++;
        }
        return await FinishImportAsync("ItemsImport", "Items", "items", count, errors);
    }

    public async Task<SyncResultDto> ImportSuppliersAsync(List<ErpSupplierDto> suppliers)
    {
        _user.Require(Roles.Erp);
        var errors = new List<string>();
        var codes = suppliers.Select(s => s.ErpSupplierCode).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        var existing = await _db.Suppliers.Where(s => codes.Contains(s.ERPSupplierCode)).ToDictionaryAsync(s => s.ERPSupplierCode);
        var seen = new HashSet<string>();
        int count = 0;

        for (var row = 0; row < suppliers.Count; row++)
        {
            var dto = suppliers[row];
            if (string.IsNullOrWhiteSpace(dto.ErpSupplierCode) || string.IsNullOrWhiteSpace(dto.Name))
            { errors.Add($"row {row + 1}: code and name are required"); continue; }
            if (!seen.Add(dto.ErpSupplierCode))
            { errors.Add($"{dto.ErpSupplierCode}: duplicate in payload"); continue; }

            if (!existing.TryGetValue(dto.ErpSupplierCode, out var supplier))
            {
                supplier = new Supplier { ERPSupplierCode = dto.ErpSupplierCode };
                _db.Suppliers.Add(supplier);
            }
            supplier.SupplierName = dto.Name;
            supplier.IsActive = dto.IsActive;
            count++;
        }
        return await FinishImportAsync("SuppliersImport", "Suppliers", "suppliers", count, errors);
    }

    public async Task<SyncResultDto> ImportPurchasePricesAsync(List<ErpPurchasePriceDto> prices)
    {
        _user.Require(Roles.Erp);
        var errors = new List<string>();
        var supplierCodes = prices.Select(p => p.ErpSupplierCode).Distinct().ToList();
        var itemCodes = prices.Select(p => p.ErpItemCode).Distinct().ToList();
        var suppliers = await _db.Suppliers.Where(s => supplierCodes.Contains(s.ERPSupplierCode)).ToDictionaryAsync(s => s.ERPSupplierCode);
        var items = await _db.Items.Where(i => itemCodes.Contains(i.ERPItemCode)).ToDictionaryAsync(i => i.ERPItemCode);

        // A price for the same supplier, item and start date is updated, not duplicated.
        var itemIDs = items.Values.Select(i => i.ItemID).ToList();
        var existing = (await _db.SupplierPriceLists.Where(s => itemIDs.Contains(s.ItemID)).ToListAsync())
            .GroupBy(s => (s.SupplierID, s.ItemID, s.ValidFrom))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.SupplierPriceID).First());
        int count = 0;

        for (var row = 0; row < prices.Count; row++)
        {
            var dto = prices[row];
            var label = $"row {row + 1} ({dto.ErpSupplierCode}/{dto.ErpItemCode})";
            if (string.IsNullOrWhiteSpace(dto.ErpSupplierCode) || string.IsNullOrWhiteSpace(dto.ErpItemCode))
            { errors.Add($"row {row + 1}: supplier and item codes are required"); continue; }
            if (!suppliers.TryGetValue(dto.ErpSupplierCode, out var supplier))
            { errors.Add($"{label}: unknown supplier"); continue; }
            if (!items.TryGetValue(dto.ErpItemCode, out var item))
            { errors.Add($"{label}: unknown item"); continue; }
            if (dto.BasePrice <= 0 || dto.ExtraCharges < 0)
            { errors.Add($"{label}: base price must be > 0 and extra charges >= 0"); continue; }
            if (dto.ValidFrom == default || dto.ValidTo < dto.ValidFrom)
            { errors.Add($"{label}: invalid validity dates"); continue; }

            var key = (supplier.SupplierID, item.ItemID, dto.ValidFrom);
            if (!existing.TryGetValue(key, out var price))
            {
                price = new SupplierPriceList { SupplierID = supplier.SupplierID, ItemID = item.ItemID, ValidFrom = dto.ValidFrom };
                existing[key] = price;
                _db.SupplierPriceLists.Add(price);
            }
            price.BasePrice = dto.BasePrice;
            price.ExtraCharges = dto.ExtraCharges;
            price.Currency = dto.Currency;
            price.ValidTo = dto.ValidTo;
            count++;
        }
        return await FinishImportAsync("PurchasePricesImport", "SupplierPriceList", "purchase prices", count, errors);
    }

    public async Task<List<ErpSellingPriceExportDto>> ExportApprovedSellingPricesAsync(bool includeExported = false)
    {
        _user.Require(Roles.Erp);
        var approved = _db.PriceBatchLines
            .Where(l => l.LineStatus == LineStatus.Approved && l.Batch!.Status == BatchStatus.Approved);

        if (!includeExported)
        {
            var pendingItemIDs = await approved.Where(l => l.ExportedAt == null).Select(l => l.ItemID).Distinct().ToListAsync();
            approved = approved.Where(l => pendingItemIDs.Contains(l.ItemID));
        }

        var lines = await approved
            .Select(l => new { Line = l, l.Batch!.ApprovedAt, l.Item!.ERPItemCode, ChannelCode = l.Channel!.Code })
            .ToListAsync();

        // Only the latest approved price per item/channel is sent, so an older approval
        // can never overwrite a newer one in the ERP.
        var latest = lines
            .GroupBy(x => (x.Line.ItemID, x.Line.ChannelID))
            .Where(g => includeExported || g.Any(x => x.Line.ExportedAt == null))
            .Select(g => g.OrderByDescending(x => x.ApprovedAt).ThenByDescending(x => x.Line.LineID).First())
            .ToList();

        var now = DateTime.UtcNow;
        foreach (var x in lines.Where(x => x.Line.ExportedAt == null)) x.Line.ExportedAt = now;

        _db.ERPSyncLogs.Add(new ERPSyncLog
        {
            SyncType = "SellingPricesExport", Entity = "PriceBatchLines", Status = "Success",
            Message = $"{latest.Count} prices exported" + (includeExported ? " (full re-export)" : "")
        });
        await _db.SaveChangesAsync();

        return latest.Select(x => new ErpSellingPriceExportDto(
            x.ERPItemCode, x.ChannelCode, x.Line.NewPrice,
            BusinessClock.ToBusinessDate(x.ApprovedAt ?? now), x.Line.BatchID)).ToList();
    }

    public Task<List<ERPSyncLog>> GetSyncLogsAsync(int count = 50) =>
        _db.ERPSyncLogs.OrderByDescending(l => l.CreatedAt).Take(count).ToListAsync();

    private async Task<SyncResultDto> FinishImportAsync(string syncType, string entity, string noun, int processed, List<string> errors)
    {
        var status = errors.Count == 0 ? "Success" : processed > 0 ? "Partial" : "Failed";
        var message = $"{processed} {noun} imported, {errors.Count} skipped";
        if (errors.Count > 0)
            message += ": " + string.Join("; ", errors.Take(20)) + (errors.Count > 20 ? $"; … {errors.Count - 20} more" : "");

        _db.ERPSyncLogs.Add(new ERPSyncLog { SyncType = syncType, Entity = entity, Status = status, Message = message });
        await _db.SaveChangesAsync();
        return new SyncResultDto(errors.Count == 0, message, processed);
    }
}

// ── Dashboard ─────────────────────────────────────────────────────────────────

public class DashboardService : IDashboardService
{
    private readonly SmartPricingDbContext _db;
    public DashboardService(SmartPricingDbContext db) => _db = db;

    public async Task<DashboardKpiDto> GetKpisAsync()
    {
        var startOfToday = BusinessClock.StartOfTodayUtc;
        var totalBatches = await _db.PriceBatches.CountAsync();
        var pendingApproval = await _db.PriceBatches.CountAsync(b => b.Status == BatchStatus.Submitted);
        var approvedToday = await _db.PriceBatches.CountAsync(b => b.Status == BatchStatus.Approved && b.ApprovedAt >= startOfToday);

        // Aggregate in SQL instead of loading every calculated price
        var belowTarget = await _db.CalculatedSellingPrices.CountAsync(c => c.ColorIndicator == ColorIndicator.Red);
        var avgMargin = Math.Round(await _db.CalculatedSellingPrices.AverageAsync(c => (decimal?)c.GrossMarginPercent) ?? 0, 2);

        var recentBatches = await _db.PriceBatches.Include(b => b.Creator)
            .OrderByDescending(b => b.CreatedAt).Take(5)
            .Select(b => new RecentBatchDto(b.BatchID, b.BatchName, b.Status, b.CreatedAt, b.Creator!.DisplayName))
            .ToListAsync();

        var alerts = await _db.CalculatedSellingPrices
            .Where(c => c.ColorIndicator != ColorIndicator.Green)
            .OrderBy(c => c.GrossMarginPercent).Take(10)
            .Select(c => new MarginAlertDto(c.Item!.ItemName, c.Channel!.Name, c.GrossMarginPercent,
                c.Rule != null ? c.Rule.TargetMarginPercent ?? 0 : 0, c.ColorIndicator))
            .ToListAsync();

        return new DashboardKpiDto(totalBatches, pendingApproval, approvedToday, belowTarget, avgMargin, recentBatches, alerts);
    }
}

// ── User Service ──────────────────────────────────────────────────────────────

public class UserService : IUserService
{
    private const int MaxFailedLogins = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly SmartPricingDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;
    public UserService(SmartPricingDbContext db, IAuditService audit, ICurrentUser user) { _db = db; _audit = audit; _user = user; }

    private static UserDto Map(User u) => new(u.UserID, u.UserName, u.DisplayName, u.Email,
        u.UserRoles.Select(ur => ur.Role!.RoleName).ToList());

    private IQueryable<User> BaseQuery() => _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

    public async Task<AuthResultDto> AuthenticateAsync(LoginDto dto)
    {
        var user = await BaseQuery().FirstOrDefaultAsync(u => u.UserName == dto.UserName && u.IsActive);
        if (user is null) return new AuthResultDto(null, false, false);

        if (user.LockoutEnd > DateTime.UtcNow) return new AuthResultDto(null, true, false);

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedLogins)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginCount = 0;
            }
            _audit.Log(user.UserID, "LoginFailed", "User", user.UserID);
            await _db.SaveChangesAsync();
            return new AuthResultDto(null, user.LockoutEnd > DateTime.UtcNow, false);
        }

        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        _audit.Log(user.UserID, "Login", "User", user.UserID);
        await _db.SaveChangesAsync();
        return new AuthResultDto(Map(user), false, user.MustChangePassword);
    }

    public async Task<bool> ChangePasswordAsync(int userID, string currentPassword, string newPassword)
    {
        var user = await _db.Users.FindAsync(userID);
        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)) return false;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.MustChangePassword = false;
        _audit.Log(userID, "ChangePassword", "User", userID);
        await _db.SaveChangesAsync();
        return true;
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
        _user.Require(Roles.Admin);
        var user = new User
        {
            UserName = dto.UserName, DisplayName = dto.DisplayName, Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            MustChangePassword = true
        };
        await _db.InTransactionAsync(async () =>
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            _audit.Log(_user.UserID, "CreateUser", "User", user.UserID, null, dto.UserName);
            await UpdateRolesAsync(user.UserID, dto.Roles);
        });
        return user.UserID;
    }

    public async Task UpdateRolesAsync(int userID, List<string> roles)
    {
        _user.Require(Roles.Admin);
        var existing = _db.UserRoles.Where(ur => ur.UserID == userID);
        _db.UserRoles.RemoveRange(existing);
        foreach (var roleName in roles)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role is not null) _db.UserRoles.Add(new UserRole { UserID = userID, RoleID = role.RoleID });
        }
        _audit.Log(_user.UserID, "UpdateRoles", "User", userID, null, string.Join(",", roles));
        await _db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int userID)
    {
        _user.Require(Roles.Admin);
        if (userID == _user.UserID)
            throw new InvalidOperationException("Δεν μπορείτε να απενεργοποιήσετε τον εαυτό σας / You cannot deactivate yourself.");
        var user = await _db.Users.FindAsync(userID);
        if (user is null) return;
        user.IsActive = false;
        _audit.Log(_user.UserID, "DeactivateUser", "User", userID);
        await _db.SaveChangesAsync();
    }
}

// ── Audit Service ─────────────────────────────────────────────────────────────

public class AuditService : IAuditService
{
    private readonly SmartPricingDbContext _db;
    public AuditService(SmartPricingDbContext db) => _db = db;

    public void Log(int? userID, string action, string entityName, int? entityID, string? oldValue = null, string? newValue = null) =>
        _db.AuditLogs.Add(new AuditLog { UserID = userID, Action = action, EntityName = entityName, EntityID = entityID, OldValue = oldValue, NewValue = newValue });

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
