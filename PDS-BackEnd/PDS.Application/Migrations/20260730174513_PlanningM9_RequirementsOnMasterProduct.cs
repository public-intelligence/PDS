using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class PlanningM9_RequirementsOnMasterProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnifiedCodeId",
                schema: "Planning",
                table: "PrioritizationDecisions",
                newName: "MasterProductId");

            migrationBuilder.AlterColumn<Guid>(
                name: "UnifiedCodeId",
                schema: "Planning",
                table: "Requirements",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "AccountingAccountCode",
                schema: "Planning",
                table: "Requirements",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountingAccountName",
                schema: "Planning",
                table: "Requirements",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpenseObjectCode",
                schema: "Planning",
                table: "Requirements",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpenseObjectName",
                schema: "Planning",
                table: "Requirements",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                schema: "Planning",
                table: "Requirements",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MasterProductCode",
                schema: "Planning",
                table: "Requirements",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MasterProductId",
                schema: "Planning",
                table: "Requirements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MasterProductName",
                schema: "Planning",
                table: "Requirements",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnspscCode",
                schema: "Planning",
                table: "Requirements",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnspscName",
                schema: "Planning",
                table: "Requirements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql(@"
-- Backfill de Requerimientos a MasterProducts
UPDATE r
SET r.MasterProductId = mp.Id,
    r.MasterProductCode = mp.Code,
    r.MasterProductName = mp.Name,
    r.UnspscCode = mp.UnspscCode,
    r.UnspscName = mp.UnspscName,
    r.ExpenseObjectCode = mp.ExpenseObjectCode,
    r.ExpenseObjectName = mp.ExpenseObjectName,
    r.AccountingAccountCode = mp.AccountingAccountCode,
    r.AccountingAccountName = mp.AccountingAccountName,
    r.ItemType = mp.ItemType
FROM Planning.Requirements r
CROSS APPLY (
    SELECT TOP 1 Id, Code, Name, UnspscCode, UnspscName, ExpenseObjectCode, ExpenseObjectName, AccountingAccountCode, AccountingAccountName, ItemType
    FROM Planning.MasterProducts mp
    WHERE mp.OrganizationId = r.OrganizationId AND mp.UnifiedCodeId = r.UnifiedCodeId AND mp.DeletedAt IS NULL
    ORDER BY mp.CreatedAt ASC
) mp
WHERE r.MasterProductId IS NULL;

INSERT INTO Planning.MasterProducts (
    Id, OrganizationId, Code, Name, SearchText, UnifiedCodeId, UnspscCode, UnspscName,
    ConsolidatorAreaId, ConsolidatorAreaName, ConsolidatorAreaSource, State, RequiresReview, ReviewReason, Version, CreatedAt, UpdatedAt
)
SELECT
    NEWID(),
    r.OrganizationId,
    'ART-MIGRATED-' + SUBSTRING(CAST(r.Id AS VARCHAR(36)), 1, 8),
    ISNULL(r.UnifiedCodeName, 'Artículo Migrado'),
    LOWER(ISNULL(r.UnifiedCodeName, 'Artículo Migrado')),
    r.UnifiedCodeId,
    uc.Code,
    uc.Name,
    r.ConsolidatorAreaId,
    r.ConsolidatorAreaName,
    'Imported',
    'Active',
    1,
    'Creado al vuelo por migración M9 desde Requerimiento huérfano',
    1,
    GETDATE(),
    GETDATE()
FROM Planning.Requirements r
LEFT JOIN Planning.UnifiedCodes uc ON uc.Id = r.UnifiedCodeId
WHERE r.MasterProductId IS NULL
GROUP BY r.OrganizationId, r.UnifiedCodeId, r.UnifiedCodeName, r.ConsolidatorAreaId, r.ConsolidatorAreaName, uc.Code, uc.Name, r.Id;

UPDATE r
SET r.MasterProductId = mp.Id,
    r.MasterProductCode = mp.Code,
    r.MasterProductName = mp.Name,
    r.UnspscCode = mp.UnspscCode,
    r.UnspscName = mp.UnspscName,
    r.ExpenseObjectCode = mp.ExpenseObjectCode,
    r.ExpenseObjectName = mp.ExpenseObjectName,
    r.AccountingAccountCode = mp.AccountingAccountCode,
    r.AccountingAccountName = mp.AccountingAccountName,
    r.ItemType = mp.ItemType
FROM Planning.Requirements r
CROSS APPLY (
    SELECT TOP 1 Id, Code, Name, UnspscCode, UnspscName, ExpenseObjectCode, ExpenseObjectName, AccountingAccountCode, AccountingAccountName, ItemType
    FROM Planning.MasterProducts mp
    WHERE mp.OrganizationId = r.OrganizationId AND mp.UnifiedCodeId = r.UnifiedCodeId AND mp.DeletedAt IS NULL
    ORDER BY mp.CreatedAt ASC
) mp
WHERE r.MasterProductId IS NULL;

IF EXISTS (SELECT 1 FROM Planning.Requirements WHERE MasterProductId IS NULL)
    THROW 51000, 'Error en migración M9: Existen requerimientos sin MasterProductId resuelto.', 1;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountingAccountCode",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "AccountingAccountName",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "ExpenseObjectCode",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "ExpenseObjectName",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "ItemType",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "MasterProductCode",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "MasterProductId",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "MasterProductName",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "UnspscCode",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "UnspscName",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.RenameColumn(
                name: "MasterProductId",
                schema: "Planning",
                table: "PrioritizationDecisions",
                newName: "UnifiedCodeId");

            migrationBuilder.AlterColumn<Guid>(
                name: "UnifiedCodeId",
                schema: "Planning",
                table: "Requirements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
