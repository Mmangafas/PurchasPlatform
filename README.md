# 🧩 Smart Pricing Control

**Έξυπνη Διαχείριση & Έγκριση Τιμών Προμηθευτών και Πώλησης**

A full-stack Blazor Web App (.NET 10) for managing supplier pricing, margin calculation, approval workflows, and ERP integration.

---

## 📋 Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | **10.0+** | https://dotnet.microsoft.com/download |
| SQL Server | 2019+ or LocalDB | https://aka.ms/sqllocaldb |
| Visual Studio | 2022 v17.12+ | or VS Code + C# Dev Kit |

---

## 🚀 Quick Start

### 1. Clone / Extract the project

```bash
cd SmartPricing
```

### 2. Configure the database connection

Edit `SmartPricing.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=(localdb)\\mssqllocaldb;Database=SmartPricingDb;Trusted_Connection=True;"
  }
}
```

For SQL Server Express:
```
Server=.\\SQLEXPRESS;Database=SmartPricingDb;Trusted_Connection=True;
```

### 3. Database migrations

Migrations live in `SmartPricing.Infrastructure/Migrations` and are applied automatically on startup.
A database created by an older version (with `EnsureCreated`) is detected and upgraded in place.

To add a migration after changing the model:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project SmartPricing.Infrastructure --startup-project SmartPricing.Web
```

### 4. Run the application

```bash
cd SmartPricing.Web
dotnet run
```

Open: **https://localhost:5001**

### 5. Default login

| Field | Value |
|-------|-------|
| Username | `admin` |
| Password | `Admin@123` |

---

## 🏗️ Solution Structure

```
SmartPricing/
├── SmartPricing.Domain/           # Entities, Enums (no dependencies)
│   ├── Entities/Entities.cs       # All EF Core entity classes
│   └── Enums/Enums.cs             # BatchStatus, LineStatus, ColorIndicator...
│
├── SmartPricing.Application/      # Business logic (depends on Domain)
│   ├── DTOs/DTOs.cs               # All Data Transfer Objects
│   ├── Helpers/PricingCalculator.cs  # GM%, Markup%, rounding, color indicator
│   └── Interfaces/IServices.cs    # Service contracts
│
├── SmartPricing.Infrastructure/   # EF Core, SQL Server, all service implementations
│   ├── Data/SmartPricingDbContext.cs  # DbContext + seed data
│   └── Services/Services.cs       # All service implementations
│
└── SmartPricing.Web/              # Blazor Server UI (.NET 10)
    ├── Components/
    │   ├── Pages/                 # All page components
    │   │   ├── Dashboard.razor
    │   │   ├── SupplierPrices.razor
    │   │   ├── SellingPrices.razor
    │   │   ├── Batches.razor
    │   │   ├── BatchDetail.razor
    │   │   ├── PendingApprovals.razor
    │   │   ├── PricingRules.razor
    │   │   ├── ErpSync.razor
    │   │   ├── UsersAdmin.razor
    │   │   ├── AuditLog.razor
    │   │   └── Login.razor
    │   ├── Shared/                # Reusable components + dialogs
    │   └── Layout/                # MainLayout, EmptyLayout
    ├── Services/AppStateService.cs
    ├── wwwroot/css/app.css
    └── Program.cs
```

---

## 🔐 Roles & Access

| Role | Greek | Access |
|------|-------|--------|
| **Requester** | Αιτών | Create batches, add lines, view prices |
| **Reviewer** | Εξετάζων | Review submitted batches |
| **Approver** | Εγκρίνων | Approve/reject lines and batches |
| **Admin** | Διαχειριστής | Full access + ERP sync + user management |

---

## 📄 Pages

| URL | Title | Min Role |
|-----|-------|----------|
| `/` | Dashboard | Any |
| `/supplier-prices` | Τιμές Προμηθευτών | Requester |
| `/selling-prices` | Τιμές Πώλησης & Margins | Requester |
| `/batches` | Batches Ανατίμησης | Requester |
| `/batches/{id}` | Batch Detail + Approve | Requester / Approver |
| `/batches/pending` | Εκκρεμείς Εγκρίσεις | Reviewer |
| `/pricing-rules` | Κανόνες Τιμολόγησης | Approver |
| `/erp-sync` | ERP Synchronization | Admin |
| `/admin/users` | Χρήστες & Ρόλοι | Admin |
| `/audit-log` | Audit Log | Approver |

---

## 🧮 Pricing Formulas

```
Gross Margin % = (SellPrice - Cost) / SellPrice × 100
Markup %       = (SellPrice - Cost) / Cost × 100

Suggested Price (from GM%) = Cost / (1 - TargetGM% / 100)
Suggested Price (from Markup%) = Cost × (1 + Markup% / 100)
```

**Color Indicators:**
- 🟢 **Green** — Actual GM% ≥ Target GM%
- 🟡 **Orange** — Within 2% below Target
- 🔴 **Red** — More than 2% below Target

---

## 🔗 ERP API Contract

The system exposes/consumes these endpoints:

| Method | Endpoint | Direction |
|--------|----------|-----------|
| `POST` | `/api/erp/items/import` | ERP → SmartPricing |
| `POST` | `/api/erp/suppliers/import` | ERP → SmartPricing |
| `POST` | `/api/erp/purchase-prices/import` | ERP → SmartPricing |
| `GET` | `/api/erp/selling-prices/export` | SmartPricing → ERP |
| `GET` | `/api/erp/sync-logs` | Monitoring |

All requests are authenticated with `Authorization: Bearer {Erp:ApiKey}`. Set the key (at least 32 characters) outside `appsettings.json`, for example:

```bash
dotnet user-secrets set "Erp:ApiKey" "<long random key>" --project SmartPricing.Web
```

or the `Erp__ApiKey` environment variable on the server. While no key is set, the API rejects every call.

- **Imports** validate every row. Invalid rows, unknown suppliers or items, and duplicates in the payload are skipped and listed in the response and the sync log (status `Partial` or `Failed`). Re-importing a purchase price with the same supplier, item and `validFrom` updates it instead of adding a duplicate.
- **Export** returns only the latest approved price per item/channel that has not been exported yet, and marks it as exported. Add `?includeExported=true` to re-send all current prices.

---

## 📦 Key NuGet Packages

| Package | Purpose |
|---------|---------|
| `MudBlazor 8.4.0` | UI Component library |
| `Microsoft.EntityFrameworkCore.SqlServer 10.x` | ORM + SQL Server |
| `BCrypt.Net-Next 4.0.3` | Password hashing |
| `Microsoft.AspNetCore.Authentication.Cookies` | Cookie auth |

---

## 🗄️ Database Tables

| Table | Purpose |
|-------|---------|
| `Items` | Master product catalog |
| `Suppliers` | Supplier master data |
| `Categories` | Item categories |
| `Channels` | Sales channels (Retail, HoReCa, Wholesale) |
| `SupplierPriceList` | Purchase prices per supplier/item |
| `PricingRules` | Target margin rules per category/channel |
| `RoundingRules` | Price rounding patterns (.99, .50, .05) |
| `CalculatedSellingPrice` | Computed selling prices with GM% and color |
| `PriceBatch` | Price change batch header |
| `PriceBatchLines` | Individual item price changes within a batch |
| `PriceChangeHistory` | Approved price change audit trail |
| `Users` / `Roles` / `UserRoles` | Authentication & RBAC |
| `AuditLog` | Full action audit trail |
| `ERPSyncLog` | ERP synchronization history |

---

## 🔧 Extending the App

### Add a new ERP integration
Implement `IErpIntegrationService` in `SmartPricing.Infrastructure/Services/Services.cs` and wire up a real HTTP client using the bearer token from `appsettings.json`.

### Add email notifications
Register `IEmailSender` in `Program.cs` and call it from `BatchService.SubmitAsync()` and `ApproveBatchAsync()`.

### Add promos / recipe costing
The architecture is designed for new modules — add new entities to the Domain, new services to Application/Infrastructure, and new pages to the Web project.

---

## 📝 Notes

- The app runs `MigrateAsync()` on startup; it never deletes an existing database.
- The **ERP Sync** page allows pasting raw JSON payloads for testing without a live ERP system.
- The default admin must choose a new password at first login. Accounts lock for 15 minutes after 5 failed logins.
- A SQL password saved from **Settings** is stored encrypted (ASP.NET Data Protection, keys in `%LOCALAPPDATA%\SmartPricing\keys`).

---

*Built with ❤️ using Blazor Server (.NET 10) + MudBlazor + EF Core + SQL Server*
