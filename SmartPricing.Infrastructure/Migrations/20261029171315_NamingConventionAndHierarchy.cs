using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPricing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NamingConventionAndHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalculatedSellingPrices_PricingRules_RuleID",
                table: "CalculatedSellingPrices");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_ItemGroups_ItemGroupID",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_ItemSubgroups_ItemSubgroupID",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemSubgroups_ItemGroups_ItemGroupID",
                table: "ItemSubgroups");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceBatches_Users_ApprovedBy",
                table: "PriceBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceBatches_Users_CreatedBy",
                table: "PriceBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceBatches_Users_SubmittedBy",
                table: "PriceBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceBatchLines_PriceBatches_BatchID",
                table: "PriceBatchLines");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceChangeHistories_Users_ChangedBy",
                table: "PriceChangeHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnitsOfMeasure",
                table: "UnitsOfMeasure");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemSubgroups",
                table: "ItemSubgroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemGroups",
                table: "ItemGroups");

            // The seeded pallet is no longer seeded; remove it only if no item uses it.
            migrationBuilder.Sql("""
                DELETE FROM [PackagingUnits] WHERE [PackagingUnitID] = 2
                AND NOT EXISTS (SELECT 1 FROM [ItemPackagings] WHERE [PackagingUnitID] = 2)
                """);

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ItemPackagings");

            migrationBuilder.RenameTable(
                name: "UnitsOfMeasure",
                newName: "Units");

            migrationBuilder.RenameTable(
                name: "ItemSubgroups",
                newName: "Subgroups");

            migrationBuilder.RenameTable(
                name: "ItemGroups",
                newName: "Groups");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "Users",
                newName: "UserPasswordHash");

            migrationBuilder.RenameColumn(
                name: "MustChangePassword",
                table: "Users",
                newName: "UserMustChangePassword");

            migrationBuilder.RenameColumn(
                name: "LockoutEnd",
                table: "Users",
                newName: "UserLockoutEnd");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Users",
                newName: "UserIsActive");

            migrationBuilder.RenameColumn(
                name: "FailedLoginCount",
                table: "Users",
                newName: "UserFailedLoginCount");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Users",
                newName: "UserEmail");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "Users",
                newName: "UserDisplayName");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Suppliers",
                newName: "SupplierIsActive");

            migrationBuilder.RenameColumn(
                name: "ERPSupplierCode",
                table: "Suppliers",
                newName: "SupplierCode");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Suppliers",
                newName: "SupplierCreatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Suppliers",
                newName: "SupplierCreatedAt");

            migrationBuilder.RenameColumn(
                name: "ValidTo",
                table: "SupplierPriceLists",
                newName: "SupplierPriceListValidTo");

            migrationBuilder.RenameColumn(
                name: "ValidFrom",
                table: "SupplierPriceLists",
                newName: "SupplierPriceListValidFrom");

            migrationBuilder.RenameColumn(
                name: "ExtraCharges",
                table: "SupplierPriceLists",
                newName: "SupplierPriceListExtraCharges");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "SupplierPriceLists",
                newName: "SupplierPriceListCurrency");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "SupplierPriceLists",
                newName: "SupplierPriceListCreatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "SupplierPriceLists",
                newName: "SupplierPriceListCreatedAt");

            migrationBuilder.RenameColumn(
                name: "BasePrice",
                table: "SupplierPriceLists",
                newName: "SupplierPriceListBasePrice");

            migrationBuilder.RenameColumn(
                name: "SupplierPriceID",
                table: "SupplierPriceLists",
                newName: "SupplierPriceListID");

            migrationBuilder.RenameColumn(
                name: "RuleName",
                table: "RoundingRules",
                newName: "RoundingRuleName");

            migrationBuilder.RenameColumn(
                name: "Pattern",
                table: "RoundingRules",
                newName: "RoundingRulePattern");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "RoundingRules",
                newName: "RoundingRuleIsActive");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "RoundingRules",
                newName: "RoundingRuleDescription");

            migrationBuilder.RenameColumn(
                name: "TargetMarginPercent",
                table: "PricingRules",
                newName: "PricingRuleTargetMarginPercent");

            migrationBuilder.RenameColumn(
                name: "MarkupPercent",
                table: "PricingRules",
                newName: "PricingRuleMarkupPercent");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "PricingRules",
                newName: "PricingRuleIsActive");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "PricingRules",
                newName: "PricingRuleCreatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "PricingRules",
                newName: "PricingRuleCreatedAt");

            migrationBuilder.RenameColumn(
                name: "RuleID",
                table: "PricingRules",
                newName: "PricingRuleID");

            migrationBuilder.RenameColumn(
                name: "OldPrice",
                table: "PriceChangeHistories",
                newName: "PriceChangeHistoryOldPrice");

            migrationBuilder.RenameColumn(
                name: "NewPrice",
                table: "PriceChangeHistories",
                newName: "PriceChangeHistoryNewPrice");

            migrationBuilder.RenameColumn(
                name: "ChangedBy",
                table: "PriceChangeHistories",
                newName: "PriceChangeHistoryChangedBy");

            migrationBuilder.RenameColumn(
                name: "ChangedAt",
                table: "PriceChangeHistories",
                newName: "PriceChangeHistoryChangedAt");

            migrationBuilder.RenameColumn(
                name: "ChangePercent",
                table: "PriceChangeHistories",
                newName: "PriceChangeHistoryChangePercent");

            migrationBuilder.RenameColumn(
                name: "HistoryID",
                table: "PriceChangeHistories",
                newName: "PriceChangeHistoryID");

            migrationBuilder.RenameIndex(
                name: "IX_PriceChangeHistories_ChangedBy",
                table: "PriceChangeHistories",
                newName: "IX_PriceChangeHistories_PriceChangeHistoryChangedBy");

            migrationBuilder.RenameColumn(
                name: "ReviewerComment",
                table: "PriceBatchLines",
                newName: "PriceBatchLineReviewerComment");

            migrationBuilder.RenameColumn(
                name: "OldPrice",
                table: "PriceBatchLines",
                newName: "PriceBatchLineOldPrice");

            migrationBuilder.RenameColumn(
                name: "NewPrice",
                table: "PriceBatchLines",
                newName: "PriceBatchLineNewPrice");

            migrationBuilder.RenameColumn(
                name: "MarginPercent",
                table: "PriceBatchLines",
                newName: "PriceBatchLineMarginPercent");

            migrationBuilder.RenameColumn(
                name: "LineStatus",
                table: "PriceBatchLines",
                newName: "PriceBatchLineStatus");

            migrationBuilder.RenameColumn(
                name: "ExportedAt",
                table: "PriceBatchLines",
                newName: "PriceBatchLineExportedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "PriceBatchLines",
                newName: "PriceBatchLineCreatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "PriceBatchLines",
                newName: "PriceBatchLineCreatedAt");

            migrationBuilder.RenameColumn(
                name: "BatchID",
                table: "PriceBatchLines",
                newName: "PriceBatchID");

            migrationBuilder.RenameColumn(
                name: "ApproverComment",
                table: "PriceBatchLines",
                newName: "PriceBatchLineApproverComment");

            migrationBuilder.RenameColumn(
                name: "LineID",
                table: "PriceBatchLines",
                newName: "PriceBatchLineID");

            migrationBuilder.RenameIndex(
                name: "IX_PriceBatchLines_BatchID",
                table: "PriceBatchLines",
                newName: "IX_PriceBatchLines_PriceBatchID");

            migrationBuilder.RenameColumn(
                name: "SubmittedBy",
                table: "PriceBatches",
                newName: "PriceBatchSubmittedBy");

            migrationBuilder.RenameColumn(
                name: "SubmittedAt",
                table: "PriceBatches",
                newName: "PriceBatchSubmittedAt");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "PriceBatches",
                newName: "PriceBatchStatus");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "PriceBatches",
                newName: "PriceBatchCreatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "PriceBatches",
                newName: "PriceBatchCreatedAt");

            migrationBuilder.RenameColumn(
                name: "BatchName",
                table: "PriceBatches",
                newName: "PriceBatchName");

            migrationBuilder.RenameColumn(
                name: "ApprovedBy",
                table: "PriceBatches",
                newName: "PriceBatchApprovedBy");

            migrationBuilder.RenameColumn(
                name: "ApprovedAt",
                table: "PriceBatches",
                newName: "PriceBatchApprovedAt");

            migrationBuilder.RenameColumn(
                name: "BatchID",
                table: "PriceBatches",
                newName: "PriceBatchID");

            migrationBuilder.RenameIndex(
                name: "IX_PriceBatches_SubmittedBy",
                table: "PriceBatches",
                newName: "IX_PriceBatches_PriceBatchSubmittedBy");

            migrationBuilder.RenameIndex(
                name: "IX_PriceBatches_CreatedBy",
                table: "PriceBatches",
                newName: "IX_PriceBatches_PriceBatchCreatedBy");

            migrationBuilder.RenameIndex(
                name: "IX_PriceBatches_ApprovedBy",
                table: "PriceBatches",
                newName: "IX_PriceBatches_PriceBatchApprovedBy");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "PackagingUnits",
                newName: "PackagingUnitName");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "PackagingUnits",
                newName: "PackagingUnitIsActive");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "PackagingUnits",
                newName: "PackagingUnitCode");

            migrationBuilder.RenameColumn(
                name: "ItemSubgroupID",
                table: "Items",
                newName: "SubgroupID");

            migrationBuilder.RenameColumn(
                name: "ItemGroupID",
                table: "Items",
                newName: "GroupID");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Items",
                newName: "ItemIsActive");

            migrationBuilder.RenameColumn(
                name: "ERPItemCode",
                table: "Items",
                newName: "ItemCode");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Items",
                newName: "ItemCreatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Items",
                newName: "ItemCreatedAt");

            migrationBuilder.RenameColumn(
                name: "BrandID",
                table: "Items",
                newName: "ItemBrandID");

            migrationBuilder.RenameIndex(
                name: "IX_Items_ItemSubgroupID",
                table: "Items",
                newName: "IX_Items_SubgroupID");

            migrationBuilder.RenameIndex(
                name: "IX_Items_ItemGroupID",
                table: "Items",
                newName: "IX_Items_GroupID");

            migrationBuilder.RenameColumn(
                name: "Barcode",
                table: "ItemPackagings",
                newName: "ItemPackagingBarcode");

            migrationBuilder.RenameColumn(
                name: "SyncType",
                table: "ERPSyncLogs",
                newName: "ERPSyncLogSyncType");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "ERPSyncLogs",
                newName: "ERPSyncLogStatus");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "ERPSyncLogs",
                newName: "ERPSyncLogMessage");

            migrationBuilder.RenameColumn(
                name: "Entity",
                table: "ERPSyncLogs",
                newName: "ERPSyncLogEntity");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ERPSyncLogs",
                newName: "ERPSyncLogCreatedAt");

            migrationBuilder.RenameColumn(
                name: "SyncID",
                table: "ERPSyncLogs",
                newName: "ERPSyncLogID");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Channels",
                newName: "ChannelName");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Channels",
                newName: "ChannelCode");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Categories",
                newName: "CategoryName");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Categories",
                newName: "CategoryIsActive");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Categories",
                newName: "CategoryCode");

            migrationBuilder.RenameColumn(
                name: "SuggestedPrice",
                table: "CalculatedSellingPrices",
                newName: "CalculatedSellingPriceSuggestedPrice");

            migrationBuilder.RenameColumn(
                name: "RuleID",
                table: "CalculatedSellingPrices",
                newName: "PricingRuleID");

            migrationBuilder.RenameColumn(
                name: "MarkupPercent",
                table: "CalculatedSellingPrices",
                newName: "CalculatedSellingPriceMarkupPercent");

            migrationBuilder.RenameColumn(
                name: "GrossMarginPercent",
                table: "CalculatedSellingPrices",
                newName: "CalculatedSellingPriceGrossMarginPercent");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "CalculatedSellingPrices",
                newName: "CalculatedSellingPriceCreatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CalculatedSellingPrices",
                newName: "CalculatedSellingPriceCreatedAt");

            migrationBuilder.RenameColumn(
                name: "Cost",
                table: "CalculatedSellingPrices",
                newName: "CalculatedSellingPriceCost");

            migrationBuilder.RenameColumn(
                name: "ColorIndicator",
                table: "CalculatedSellingPrices",
                newName: "CalculatedSellingPriceColorIndicator");

            migrationBuilder.RenameColumn(
                name: "CalcPriceID",
                table: "CalculatedSellingPrices",
                newName: "CalculatedSellingPriceID");

            migrationBuilder.RenameIndex(
                name: "IX_CalculatedSellingPrices_RuleID",
                table: "CalculatedSellingPrices",
                newName: "IX_CalculatedSellingPrices_PricingRuleID");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "AuditLogs",
                newName: "AuditLogTimestamp");

            migrationBuilder.RenameColumn(
                name: "OldValue",
                table: "AuditLogs",
                newName: "AuditLogOldValue");

            migrationBuilder.RenameColumn(
                name: "NewValue",
                table: "AuditLogs",
                newName: "AuditLogNewValue");

            migrationBuilder.RenameColumn(
                name: "EntityName",
                table: "AuditLogs",
                newName: "AuditLogEntityName");

            migrationBuilder.RenameColumn(
                name: "EntityID",
                table: "AuditLogs",
                newName: "AuditLogEntityID");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "AuditLogs",
                newName: "AuditLogAction");

            migrationBuilder.RenameColumn(
                name: "AuditID",
                table: "AuditLogs",
                newName: "AuditLogID");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Units",
                newName: "UnitName");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Units",
                newName: "UnitIsActive");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Units",
                newName: "UnitCode");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Subgroups",
                newName: "SubgroupName");

            migrationBuilder.RenameColumn(
                name: "ItemGroupID",
                table: "Subgroups",
                newName: "GroupID");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Subgroups",
                newName: "SubgroupIsActive");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Subgroups",
                newName: "SubgroupCode");

            migrationBuilder.RenameColumn(
                name: "ItemSubgroupID",
                table: "Subgroups",
                newName: "SubgroupID");

            migrationBuilder.RenameIndex(
                name: "IX_ItemSubgroups_ItemGroupID",
                table: "Subgroups",
                newName: "IX_Subgroups_GroupID");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Groups",
                newName: "GroupName");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Groups",
                newName: "GroupIsActive");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Groups",
                newName: "GroupCode");

            migrationBuilder.RenameColumn(
                name: "ItemGroupID",
                table: "Groups",
                newName: "GroupID");

            migrationBuilder.AddColumn<decimal>(
                name: "PackagingUnitFactor",
                table: "PackagingUnits",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UnitID",
                table: "PackagingUnits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ItemBarcode",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnitID",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CategoryID",
                table: "Groups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Units",
                table: "Units",
                column: "UnitID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subgroups",
                table: "Subgroups",
                column: "SubgroupID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Groups",
                table: "Groups",
                column: "GroupID");

            migrationBuilder.UpdateData(
                table: "PackagingUnits",
                keyColumn: "PackagingUnitID",
                keyValue: 1,
                columns: new[] { "PackagingUnitCode", "PackagingUnitFactor", "PackagingUnitName", "UnitID" },
                values: new object[] { "BOX24", 24m, "Κιβώτιο 24 τεμ. / Box of 24", 1 });

            // Existing data for the new required links, before the foreign keys are added:
            // - items: the unit code they stored (UoM) becomes a UnitID; unknown codes are added to Units
            // - packaging units: default to 1 × PCS until edited
            // - groups: go under a "GENERAL" category until moved
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Units] WHERE [UnitCode] = 'PCS')
                    INSERT INTO [Units] ([UnitCode], [UnitName], [UnitIsActive]) VALUES ('PCS', N'Τεμάχιο / Piece', 1);
                DECLARE @pcs int = (SELECT TOP 1 [UnitID] FROM [Units] WHERE [UnitCode] = 'PCS' ORDER BY [UnitID]);

                INSERT INTO [Units] ([UnitCode], [UnitName], [UnitIsActive])
                SELECT DISTINCT [UoM], [UoM], 1 FROM [Items]
                WHERE [UoM] <> '' AND [UoM] NOT IN (SELECT [UnitCode] FROM [Units]);

                UPDATE i SET [UnitID] = u.[UnitID] FROM [Items] i JOIN [Units] u ON u.[UnitCode] = i.[UoM];
                UPDATE [Items] SET [UnitID] = @pcs WHERE [UnitID] = 0;

                UPDATE [PackagingUnits] SET [UnitID] = @pcs WHERE [UnitID] = 0;
                UPDATE [PackagingUnits] SET [PackagingUnitFactor] = 1 WHERE [PackagingUnitFactor] = 0;

                -- A group goes under the category most of its items have, otherwise under GENERAL.
                UPDATE g SET [CategoryID] = x.[CategoryID] FROM [Groups] g
                CROSS APPLY (SELECT TOP 1 i.[CategoryID] FROM [Items] i
                             WHERE i.[GroupID] = g.[GroupID] AND i.[CategoryID] IS NOT NULL
                             GROUP BY i.[CategoryID] ORDER BY COUNT(*) DESC) x
                WHERE g.[CategoryID] = 0;

                IF EXISTS (SELECT 1 FROM [Groups] WHERE [CategoryID] = 0)
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM [Categories] WHERE [CategoryCode] = 'GENERAL')
                        INSERT INTO [Categories] ([CategoryCode], [CategoryName], [CategoryIsActive]) VALUES ('GENERAL', N'Γενική / General', 1);
                    UPDATE [Groups] SET [CategoryID] = (SELECT TOP 1 [CategoryID] FROM [Categories] WHERE [CategoryCode] = 'GENERAL')
                    WHERE [CategoryID] = 0;
                END

                -- Items follow their group's category.
                UPDATE i SET [CategoryID] = g.[CategoryID] FROM [Items] i JOIN [Groups] g ON g.[GroupID] = i.[GroupID]
                WHERE i.[CategoryID] IS NULL OR i.[CategoryID] <> g.[CategoryID];
                """);

            migrationBuilder.DropColumn(
                name: "UoM",
                table: "Items");

            migrationBuilder.CreateIndex(
                name: "IX_PackagingUnits_UnitID",
                table: "PackagingUnits",
                column: "UnitID");

            migrationBuilder.CreateIndex(
                name: "IX_Items_UnitID",
                table: "Items",
                column: "UnitID");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_CategoryID",
                table: "Groups",
                column: "CategoryID");

            migrationBuilder.AddForeignKey(
                name: "FK_CalculatedSellingPrices_PricingRules_PricingRuleID",
                table: "CalculatedSellingPrices",
                column: "PricingRuleID",
                principalTable: "PricingRules",
                principalColumn: "PricingRuleID");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Categories_CategoryID",
                table: "Groups",
                column: "CategoryID",
                principalTable: "Categories",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Groups_GroupID",
                table: "Items",
                column: "GroupID",
                principalTable: "Groups",
                principalColumn: "GroupID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Subgroups_SubgroupID",
                table: "Items",
                column: "SubgroupID",
                principalTable: "Subgroups",
                principalColumn: "SubgroupID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Units_UnitID",
                table: "Items",
                column: "UnitID",
                principalTable: "Units",
                principalColumn: "UnitID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PackagingUnits_Units_UnitID",
                table: "PackagingUnits",
                column: "UnitID",
                principalTable: "Units",
                principalColumn: "UnitID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceBatches_Users_PriceBatchApprovedBy",
                table: "PriceBatches",
                column: "PriceBatchApprovedBy",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceBatches_Users_PriceBatchCreatedBy",
                table: "PriceBatches",
                column: "PriceBatchCreatedBy",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceBatches_Users_PriceBatchSubmittedBy",
                table: "PriceBatches",
                column: "PriceBatchSubmittedBy",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceBatchLines_PriceBatches_PriceBatchID",
                table: "PriceBatchLines",
                column: "PriceBatchID",
                principalTable: "PriceBatches",
                principalColumn: "PriceBatchID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceChangeHistories_Users_PriceChangeHistoryChangedBy",
                table: "PriceChangeHistories",
                column: "PriceChangeHistoryChangedBy",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subgroups_Groups_GroupID",
                table: "Subgroups",
                column: "GroupID",
                principalTable: "Groups",
                principalColumn: "GroupID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalculatedSellingPrices_PricingRules_PricingRuleID",
                table: "CalculatedSellingPrices");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Categories_CategoryID",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Groups_GroupID",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Subgroups_SubgroupID",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Units_UnitID",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_PackagingUnits_Units_UnitID",
                table: "PackagingUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceBatches_Users_PriceBatchApprovedBy",
                table: "PriceBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceBatches_Users_PriceBatchCreatedBy",
                table: "PriceBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceBatches_Users_PriceBatchSubmittedBy",
                table: "PriceBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceBatchLines_PriceBatches_PriceBatchID",
                table: "PriceBatchLines");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceChangeHistories_Users_PriceChangeHistoryChangedBy",
                table: "PriceChangeHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Subgroups_Groups_GroupID",
                table: "Subgroups");

            migrationBuilder.DropIndex(
                name: "IX_PackagingUnits_UnitID",
                table: "PackagingUnits");

            migrationBuilder.DropIndex(
                name: "IX_Items_UnitID",
                table: "Items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Units",
                table: "Units");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Subgroups",
                table: "Subgroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Groups",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_CategoryID",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "PackagingUnitFactor",
                table: "PackagingUnits");

            migrationBuilder.DropColumn(
                name: "UnitID",
                table: "PackagingUnits");

            migrationBuilder.DropColumn(
                name: "ItemBarcode",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "UnitID",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CategoryID",
                table: "Groups");

            migrationBuilder.RenameTable(
                name: "Units",
                newName: "UnitsOfMeasure");

            migrationBuilder.RenameTable(
                name: "Subgroups",
                newName: "ItemSubgroups");

            migrationBuilder.RenameTable(
                name: "Groups",
                newName: "ItemGroups");

            migrationBuilder.RenameColumn(
                name: "UserPasswordHash",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "UserMustChangePassword",
                table: "Users",
                newName: "MustChangePassword");

            migrationBuilder.RenameColumn(
                name: "UserLockoutEnd",
                table: "Users",
                newName: "LockoutEnd");

            migrationBuilder.RenameColumn(
                name: "UserIsActive",
                table: "Users",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "UserFailedLoginCount",
                table: "Users",
                newName: "FailedLoginCount");

            migrationBuilder.RenameColumn(
                name: "UserEmail",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "UserDisplayName",
                table: "Users",
                newName: "DisplayName");

            migrationBuilder.RenameColumn(
                name: "SupplierIsActive",
                table: "Suppliers",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "SupplierCreatedBy",
                table: "Suppliers",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "SupplierCreatedAt",
                table: "Suppliers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "SupplierCode",
                table: "Suppliers",
                newName: "ERPSupplierCode");

            migrationBuilder.RenameColumn(
                name: "SupplierPriceListValidTo",
                table: "SupplierPriceLists",
                newName: "ValidTo");

            migrationBuilder.RenameColumn(
                name: "SupplierPriceListValidFrom",
                table: "SupplierPriceLists",
                newName: "ValidFrom");

            migrationBuilder.RenameColumn(
                name: "SupplierPriceListExtraCharges",
                table: "SupplierPriceLists",
                newName: "ExtraCharges");

            migrationBuilder.RenameColumn(
                name: "SupplierPriceListCurrency",
                table: "SupplierPriceLists",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "SupplierPriceListCreatedBy",
                table: "SupplierPriceLists",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "SupplierPriceListCreatedAt",
                table: "SupplierPriceLists",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "SupplierPriceListBasePrice",
                table: "SupplierPriceLists",
                newName: "BasePrice");

            migrationBuilder.RenameColumn(
                name: "SupplierPriceListID",
                table: "SupplierPriceLists",
                newName: "SupplierPriceID");

            migrationBuilder.RenameColumn(
                name: "RoundingRulePattern",
                table: "RoundingRules",
                newName: "Pattern");

            migrationBuilder.RenameColumn(
                name: "RoundingRuleName",
                table: "RoundingRules",
                newName: "RuleName");

            migrationBuilder.RenameColumn(
                name: "RoundingRuleIsActive",
                table: "RoundingRules",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "RoundingRuleDescription",
                table: "RoundingRules",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "PricingRuleTargetMarginPercent",
                table: "PricingRules",
                newName: "TargetMarginPercent");

            migrationBuilder.RenameColumn(
                name: "PricingRuleMarkupPercent",
                table: "PricingRules",
                newName: "MarkupPercent");

            migrationBuilder.RenameColumn(
                name: "PricingRuleIsActive",
                table: "PricingRules",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "PricingRuleCreatedBy",
                table: "PricingRules",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "PricingRuleCreatedAt",
                table: "PricingRules",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "PricingRuleID",
                table: "PricingRules",
                newName: "RuleID");

            migrationBuilder.RenameColumn(
                name: "PriceChangeHistoryOldPrice",
                table: "PriceChangeHistories",
                newName: "OldPrice");

            migrationBuilder.RenameColumn(
                name: "PriceChangeHistoryNewPrice",
                table: "PriceChangeHistories",
                newName: "NewPrice");

            migrationBuilder.RenameColumn(
                name: "PriceChangeHistoryChangedBy",
                table: "PriceChangeHistories",
                newName: "ChangedBy");

            migrationBuilder.RenameColumn(
                name: "PriceChangeHistoryChangedAt",
                table: "PriceChangeHistories",
                newName: "ChangedAt");

            migrationBuilder.RenameColumn(
                name: "PriceChangeHistoryChangePercent",
                table: "PriceChangeHistories",
                newName: "ChangePercent");

            migrationBuilder.RenameColumn(
                name: "PriceChangeHistoryID",
                table: "PriceChangeHistories",
                newName: "HistoryID");

            migrationBuilder.RenameIndex(
                name: "IX_PriceChangeHistories_PriceChangeHistoryChangedBy",
                table: "PriceChangeHistories",
                newName: "IX_PriceChangeHistories_ChangedBy");

            migrationBuilder.RenameColumn(
                name: "PriceBatchLineStatus",
                table: "PriceBatchLines",
                newName: "LineStatus");

            migrationBuilder.RenameColumn(
                name: "PriceBatchLineReviewerComment",
                table: "PriceBatchLines",
                newName: "ReviewerComment");

            migrationBuilder.RenameColumn(
                name: "PriceBatchLineOldPrice",
                table: "PriceBatchLines",
                newName: "OldPrice");

            migrationBuilder.RenameColumn(
                name: "PriceBatchLineNewPrice",
                table: "PriceBatchLines",
                newName: "NewPrice");

            migrationBuilder.RenameColumn(
                name: "PriceBatchLineMarginPercent",
                table: "PriceBatchLines",
                newName: "MarginPercent");

            migrationBuilder.RenameColumn(
                name: "PriceBatchLineExportedAt",
                table: "PriceBatchLines",
                newName: "ExportedAt");

            migrationBuilder.RenameColumn(
                name: "PriceBatchLineCreatedBy",
                table: "PriceBatchLines",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "PriceBatchLineCreatedAt",
                table: "PriceBatchLines",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "PriceBatchLineApproverComment",
                table: "PriceBatchLines",
                newName: "ApproverComment");

            migrationBuilder.RenameColumn(
                name: "PriceBatchID",
                table: "PriceBatchLines",
                newName: "BatchID");

            migrationBuilder.RenameColumn(
                name: "PriceBatchLineID",
                table: "PriceBatchLines",
                newName: "LineID");

            migrationBuilder.RenameIndex(
                name: "IX_PriceBatchLines_PriceBatchID",
                table: "PriceBatchLines",
                newName: "IX_PriceBatchLines_BatchID");

            migrationBuilder.RenameColumn(
                name: "PriceBatchSubmittedBy",
                table: "PriceBatches",
                newName: "SubmittedBy");

            migrationBuilder.RenameColumn(
                name: "PriceBatchSubmittedAt",
                table: "PriceBatches",
                newName: "SubmittedAt");

            migrationBuilder.RenameColumn(
                name: "PriceBatchStatus",
                table: "PriceBatches",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "PriceBatchName",
                table: "PriceBatches",
                newName: "BatchName");

            migrationBuilder.RenameColumn(
                name: "PriceBatchCreatedBy",
                table: "PriceBatches",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "PriceBatchCreatedAt",
                table: "PriceBatches",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "PriceBatchApprovedBy",
                table: "PriceBatches",
                newName: "ApprovedBy");

            migrationBuilder.RenameColumn(
                name: "PriceBatchApprovedAt",
                table: "PriceBatches",
                newName: "ApprovedAt");

            migrationBuilder.RenameColumn(
                name: "PriceBatchID",
                table: "PriceBatches",
                newName: "BatchID");

            migrationBuilder.RenameIndex(
                name: "IX_PriceBatches_PriceBatchSubmittedBy",
                table: "PriceBatches",
                newName: "IX_PriceBatches_SubmittedBy");

            migrationBuilder.RenameIndex(
                name: "IX_PriceBatches_PriceBatchCreatedBy",
                table: "PriceBatches",
                newName: "IX_PriceBatches_CreatedBy");

            migrationBuilder.RenameIndex(
                name: "IX_PriceBatches_PriceBatchApprovedBy",
                table: "PriceBatches",
                newName: "IX_PriceBatches_ApprovedBy");

            migrationBuilder.RenameColumn(
                name: "PackagingUnitName",
                table: "PackagingUnits",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "PackagingUnitIsActive",
                table: "PackagingUnits",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "PackagingUnitCode",
                table: "PackagingUnits",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "SubgroupID",
                table: "Items",
                newName: "ItemSubgroupID");

            migrationBuilder.RenameColumn(
                name: "ItemIsActive",
                table: "Items",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "ItemCreatedBy",
                table: "Items",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "ItemCreatedAt",
                table: "Items",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ItemCode",
                table: "Items",
                newName: "ERPItemCode");

            migrationBuilder.RenameColumn(
                name: "ItemBrandID",
                table: "Items",
                newName: "BrandID");

            migrationBuilder.RenameColumn(
                name: "GroupID",
                table: "Items",
                newName: "ItemGroupID");

            migrationBuilder.RenameIndex(
                name: "IX_Items_SubgroupID",
                table: "Items",
                newName: "IX_Items_ItemSubgroupID");

            migrationBuilder.RenameIndex(
                name: "IX_Items_GroupID",
                table: "Items",
                newName: "IX_Items_ItemGroupID");

            migrationBuilder.RenameColumn(
                name: "ItemPackagingBarcode",
                table: "ItemPackagings",
                newName: "Barcode");

            migrationBuilder.RenameColumn(
                name: "ERPSyncLogSyncType",
                table: "ERPSyncLogs",
                newName: "SyncType");

            migrationBuilder.RenameColumn(
                name: "ERPSyncLogStatus",
                table: "ERPSyncLogs",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "ERPSyncLogMessage",
                table: "ERPSyncLogs",
                newName: "Message");

            migrationBuilder.RenameColumn(
                name: "ERPSyncLogEntity",
                table: "ERPSyncLogs",
                newName: "Entity");

            migrationBuilder.RenameColumn(
                name: "ERPSyncLogCreatedAt",
                table: "ERPSyncLogs",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ERPSyncLogID",
                table: "ERPSyncLogs",
                newName: "SyncID");

            migrationBuilder.RenameColumn(
                name: "ChannelName",
                table: "Channels",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "ChannelCode",
                table: "Channels",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "CategoryName",
                table: "Categories",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "CategoryIsActive",
                table: "Categories",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "CategoryCode",
                table: "Categories",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "PricingRuleID",
                table: "CalculatedSellingPrices",
                newName: "RuleID");

            migrationBuilder.RenameColumn(
                name: "CalculatedSellingPriceSuggestedPrice",
                table: "CalculatedSellingPrices",
                newName: "SuggestedPrice");

            migrationBuilder.RenameColumn(
                name: "CalculatedSellingPriceMarkupPercent",
                table: "CalculatedSellingPrices",
                newName: "MarkupPercent");

            migrationBuilder.RenameColumn(
                name: "CalculatedSellingPriceGrossMarginPercent",
                table: "CalculatedSellingPrices",
                newName: "GrossMarginPercent");

            migrationBuilder.RenameColumn(
                name: "CalculatedSellingPriceCreatedBy",
                table: "CalculatedSellingPrices",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "CalculatedSellingPriceCreatedAt",
                table: "CalculatedSellingPrices",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CalculatedSellingPriceCost",
                table: "CalculatedSellingPrices",
                newName: "Cost");

            migrationBuilder.RenameColumn(
                name: "CalculatedSellingPriceColorIndicator",
                table: "CalculatedSellingPrices",
                newName: "ColorIndicator");

            migrationBuilder.RenameColumn(
                name: "CalculatedSellingPriceID",
                table: "CalculatedSellingPrices",
                newName: "CalcPriceID");

            migrationBuilder.RenameIndex(
                name: "IX_CalculatedSellingPrices_PricingRuleID",
                table: "CalculatedSellingPrices",
                newName: "IX_CalculatedSellingPrices_RuleID");

            migrationBuilder.RenameColumn(
                name: "AuditLogTimestamp",
                table: "AuditLogs",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "AuditLogOldValue",
                table: "AuditLogs",
                newName: "OldValue");

            migrationBuilder.RenameColumn(
                name: "AuditLogNewValue",
                table: "AuditLogs",
                newName: "NewValue");

            migrationBuilder.RenameColumn(
                name: "AuditLogEntityName",
                table: "AuditLogs",
                newName: "EntityName");

            migrationBuilder.RenameColumn(
                name: "AuditLogEntityID",
                table: "AuditLogs",
                newName: "EntityID");

            migrationBuilder.RenameColumn(
                name: "AuditLogAction",
                table: "AuditLogs",
                newName: "Action");

            migrationBuilder.RenameColumn(
                name: "AuditLogID",
                table: "AuditLogs",
                newName: "AuditID");

            migrationBuilder.RenameColumn(
                name: "UnitName",
                table: "UnitsOfMeasure",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "UnitIsActive",
                table: "UnitsOfMeasure",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "UnitCode",
                table: "UnitsOfMeasure",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "SubgroupName",
                table: "ItemSubgroups",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "SubgroupIsActive",
                table: "ItemSubgroups",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "SubgroupCode",
                table: "ItemSubgroups",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "GroupID",
                table: "ItemSubgroups",
                newName: "ItemGroupID");

            migrationBuilder.RenameColumn(
                name: "SubgroupID",
                table: "ItemSubgroups",
                newName: "ItemSubgroupID");

            migrationBuilder.RenameIndex(
                name: "IX_Subgroups_GroupID",
                table: "ItemSubgroups",
                newName: "IX_ItemSubgroups_ItemGroupID");

            migrationBuilder.RenameColumn(
                name: "GroupName",
                table: "ItemGroups",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "GroupIsActive",
                table: "ItemGroups",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "GroupCode",
                table: "ItemGroups",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "GroupID",
                table: "ItemGroups",
                newName: "ItemGroupID");

            migrationBuilder.AddColumn<string>(
                name: "UoM",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "ItemPackagings",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnitsOfMeasure",
                table: "UnitsOfMeasure",
                column: "UnitID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemSubgroups",
                table: "ItemSubgroups",
                column: "ItemSubgroupID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemGroups",
                table: "ItemGroups",
                column: "ItemGroupID");

            migrationBuilder.UpdateData(
                table: "PackagingUnits",
                keyColumn: "PackagingUnitID",
                keyValue: 1,
                columns: new[] { "Code", "Name" },
                values: new object[] { "BOX", "Κιβώτιο / Box" });

            migrationBuilder.InsertData(
                table: "PackagingUnits",
                columns: new[] { "PackagingUnitID", "Code", "IsActive", "Name" },
                values: new object[] { 2, "PAL", true, "Παλέτα / Pallet" });

            migrationBuilder.AddForeignKey(
                name: "FK_CalculatedSellingPrices_PricingRules_RuleID",
                table: "CalculatedSellingPrices",
                column: "RuleID",
                principalTable: "PricingRules",
                principalColumn: "RuleID");

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

            migrationBuilder.AddForeignKey(
                name: "FK_ItemSubgroups_ItemGroups_ItemGroupID",
                table: "ItemSubgroups",
                column: "ItemGroupID",
                principalTable: "ItemGroups",
                principalColumn: "ItemGroupID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceBatches_Users_ApprovedBy",
                table: "PriceBatches",
                column: "ApprovedBy",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceBatches_Users_CreatedBy",
                table: "PriceBatches",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceBatches_Users_SubmittedBy",
                table: "PriceBatches",
                column: "SubmittedBy",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceBatchLines_PriceBatches_BatchID",
                table: "PriceBatchLines",
                column: "BatchID",
                principalTable: "PriceBatches",
                principalColumn: "BatchID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceChangeHistories_Users_ChangedBy",
                table: "PriceChangeHistories",
                column: "ChangedBy",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
