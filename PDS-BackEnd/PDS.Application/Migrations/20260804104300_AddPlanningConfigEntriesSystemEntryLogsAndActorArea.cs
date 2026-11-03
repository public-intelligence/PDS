using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningConfigEntriesSystemEntryLogsAndActorArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Planning].[PlanningActivityLogs]') AND name = N'ActorAreaId')
BEGIN
    ALTER TABLE [Planning].[PlanningActivityLogs] ADD [ActorAreaId] uniqueidentifier NULL;
END
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Planning].[PlanningActivityLogs]') AND name = N'ActorAreaName')
BEGIN
    ALTER TABLE [Planning].[PlanningActivityLogs] ADD [ActorAreaName] nvarchar(250) NULL;
END

IF OBJECT_ID(N'[Planning].[BudgetCeilings]', N'U') IS NULL
BEGIN
    CREATE TABLE [Planning].[BudgetCeilings] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] int NOT NULL,
        [FiscalYear] int NOT NULL,
        [CycleId] uniqueidentifier NULL,
        [ExpenseObjectId] uniqueidentifier NOT NULL,
        [ExpenseObjectCode] nvarchar(20) NOT NULL,
        [ExpenseObjectName] nvarchar(300) NOT NULL,
        [FundingSource] nvarchar(100) NOT NULL,
        [BudgetProgram] nvarchar(100) NULL,
        [InitialAmount] decimal(18,2) NOT NULL,
        [CurrentAmount] decimal(18,2) NOT NULL,
        [ReservedAmount] decimal(18,2) NOT NULL,
        [PlannedAmount] decimal(18,2) NOT NULL,
        [CommittedAmount] decimal(18,2) NOT NULL,
        [ExecutedAmount] decimal(18,2) NOT NULL,
        [AvailableAmount] decimal(18,2) NOT NULL,
        [ThresholdPercentage] decimal(5,2) NOT NULL DEFAULT 10.0,
        [ValidationBehavior] nvarchar(30) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_BudgetCeilings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BudgetCeilings_ExpenseObjects_ExpenseObjectId] FOREIGN KEY ([ExpenseObjectId]) REFERENCES [Planning].[ExpenseObjects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BudgetCeilings_PlanningCycles_CycleId] FOREIGN KEY ([CycleId]) REFERENCES [Planning].[PlanningCycles] ([Id]) ON DELETE NO ACTION
    );
END

IF OBJECT_ID(N'[Planning].[FundingSources]', N'U') IS NULL
BEGIN
    CREATE TABLE [Planning].[FundingSources] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] int NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_FundingSources] PRIMARY KEY ([Id])
    );
END

IF OBJECT_ID(N'[Planning].[PlanningConfigEntries]', N'U') IS NULL
BEGIN
    CREATE TABLE [Planning].[PlanningConfigEntries] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWID()),
        [OrganizationId] int NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        [JsonValue] nvarchar(max) NOT NULL,
        [UpdatedBy] uniqueidentifier NOT NULL,
        [UpdatedByName] nvarchar(250) NULL,
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
        CONSTRAINT [PK_PlanningConfigEntries] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [UX_PlanningConfigEntries_KeyPerOrg] ON [Planning].[PlanningConfigEntries] ([OrganizationId], [Key]);
END

IF OBJECT_ID(N'[Planning].[PlanningCycleAmendmentLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [Planning].[PlanningCycleAmendmentLogs] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] int NOT NULL,
        [CycleId] uniqueidentifier NOT NULL,
        [AmendmentType] nvarchar(50) NOT NULL,
        [Justification] nvarchar(2000) NOT NULL,
        [PreviousValuesJson] nvarchar(max) NULL,
        [NewValuesJson] nvarchar(max) NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedByName] nvarchar(250) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
        CONSTRAINT [PK_PlanningCycleAmendmentLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlanningCycleAmendmentLogs_PlanningCycles_CycleId] FOREIGN KEY ([CycleId]) REFERENCES [Planning].[PlanningCycles] ([Id]) ON DELETE CASCADE
    );
END

IF OBJECT_ID(N'[Planning].[SystemEntryLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [Planning].[SystemEntryLogs] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWID()),
        [OrganizationId] int NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [UserName] nvarchar(250) NULL,
        [SessionId] nvarchar(100) NULL,
        [AreaId] uniqueidentifier NULL,
        [AreaName] nvarchar(250) NULL,
        [PositionName] nvarchar(250) NULL,
        [IpAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        [EnteredAtUtc] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_SystemEntryLogs] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_SystemEntryLogs_OrganizationId_EnteredAtUtc] ON [Planning].[SystemEntryLogs] ([OrganizationId], [EnteredAtUtc]);
    CREATE INDEX [IX_SystemEntryLogs_UserId_EnteredAtUtc] ON [Planning].[SystemEntryLogs] ([UserId], [EnteredAtUtc]);
END

IF OBJECT_ID(N'[Planning].[BudgetAdjustments]', N'U') IS NULL
BEGIN
    CREATE TABLE [Planning].[BudgetAdjustments] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] int NOT NULL,
        [BudgetCeilingId] uniqueidentifier NOT NULL,
        [AdjustmentType] nvarchar(20) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [DocumentUrl] nvarchar(1000) NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedByName] nvarchar(250) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BudgetAdjustments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BudgetAdjustments_BudgetCeilings_BudgetCeilingId] FOREIGN KEY ([BudgetCeilingId]) REFERENCES [Planning].[BudgetCeilings] ([Id]) ON DELETE CASCADE
    );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetAdjustments",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "FundingSources",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "PlanningConfigEntries",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "PlanningCycleAmendmentLogs",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "SystemEntryLogs",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "BudgetCeilings",
                schema: "Planning");

            migrationBuilder.DropColumn(
                name: "ActorAreaId",
                schema: "Planning",
                table: "PlanningActivityLogs");

            migrationBuilder.DropColumn(
                name: "ActorAreaName",
                schema: "Planning",
                table: "PlanningActivityLogs");
        }
    }
}
