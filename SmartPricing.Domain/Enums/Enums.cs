namespace SmartPricing.Domain.Enums;

public enum BatchStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Cancelled
}

public enum LineStatus
{
    Pending,
    Approved,
    Rejected
}

public enum ColorIndicator
{
    Green,   // >= target margin
    Orange,  // within 2% of target
    Red      // below target
}

public enum SyncType
{
    ItemsImport,
    SuppliersImport,
    PurchasePricesImport,
    SellingPricesExport
}

public enum SyncStatus
{
    Success,
    Failed,
    Partial
}
