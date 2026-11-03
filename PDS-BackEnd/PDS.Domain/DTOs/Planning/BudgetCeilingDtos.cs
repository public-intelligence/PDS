using System;
using System.Collections.Generic;
using PDS.Domain.DTOs.Pagination;

namespace PDS.Domain.DTOs.Planning;

public class BudgetCeilingDto
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }
    public int FiscalYear { get; set; }
    public Guid? CycleId { get; set; }
    public Guid ExpenseObjectId { get; set; }
    public string ExpenseObjectCode { get; set; } = string.Empty;
    public string ExpenseObjectName { get; set; } = string.Empty;
    public string FundingSource { get; set; } = string.Empty;
    public string? BudgetProgram { get; set; }
    public int ExpenseObjectLevel { get; set; } = 4;
    public string ExpenseObjectLevelName { get; set; } = "Auxiliar";
    public decimal InitialAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal ReservedAmount { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal ExecutedAmount { get; set; }
    public decimal AvailableAmount { get; set; }
    public decimal UtilizationPercentage { get; set; }
    public decimal ThresholdPercentage { get; set; }
    public string ValidationBehavior { get; set; } = "Block";
    public string Status { get; set; } = "Green";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BudgetCeilingDetailDto : BudgetCeilingDto
{
    public decimal TotalAdjustments { get; set; }
    public decimal UnconfirmedAmount { get; set; }
    public List<BudgetAdjustmentDto> Adjustments { get; set; } = [];
    public List<ProductConsumptionDto> ConsumptionByProduct { get; set; } = [];
    public List<AreaConsumptionDto> ConsumptionByArea { get; set; } = [];
    public List<ChildExpenseObjectConsumptionDto> ConsumptionByChildExpenseObject { get; set; } = [];
    public List<RequirementDto> RecentRequirements { get; set; } = [];
}

public class ChildExpenseObjectConsumptionDto
{
    public string ExpenseObjectCode { get; set; } = string.Empty;
    public string ExpenseObjectName { get; set; } = string.Empty;
    public int Level { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PercentageOfCeiling { get; set; }
}

public class CreateBudgetCeilingDto
{
    public int FiscalYear { get; set; }
    public Guid ExpenseObjectId { get; set; }
    public required string FundingSource { get; set; }
    public string? BudgetProgram { get; set; }
    public decimal InitialAmount { get; set; }
    public decimal ThresholdPercentage { get; set; } = 10.00m;
    public string ValidationBehavior { get; set; } = "Block";
}

public class UpdateBudgetCeilingDto
{
    public decimal ThresholdPercentage { get; set; }
    public string ValidationBehavior { get; set; } = "Block";
    public string? BudgetProgram { get; set; }
}

public class CreateBudgetAdjustmentDto
{
    public required string AdjustmentType { get; set; } // Increase | Reduction
    public decimal Amount { get; set; }
    public required string Reason { get; set; }
    public string? DocumentUrl { get; set; }
}

public class BudgetAdjustmentDto
{
    public Guid Id { get; set; }
    public Guid BudgetCeilingId { get; set; }
    public string AdjustmentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? DocumentUrl { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductConsumptionDto
{
    public Guid MasterProductId { get; set; }
    public string MasterProductCode { get; set; } = string.Empty;
    public string MasterProductName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PercentageOfCeiling { get; set; }
}

public class AreaConsumptionDto
{
    public Guid AreaId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public int RequirementCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PercentageOfCeiling { get; set; }
}

public class BudgetValidationRequestDto
{
    public Guid MasterProductId { get; set; }
    public decimal RequiredAmount { get; set; }
    public string? FundingSource { get; set; }
    public int? FiscalYear { get; set; }
}

public class BudgetValidationResultDto
{
    public bool IsValid { get; set; }
    public bool ExceedsCeiling { get; set; }
    public Guid? CeilingId { get; set; }
    public string? ExpenseObjectCode { get; set; }
    public string? ExpenseObjectName { get; set; }
    public decimal CurrentCeiling { get; set; }
    public decimal AvailableBudget { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal ExceededAmount { get; set; }
    public string ValidationBehavior { get; set; } = "Block";
    public string Message { get; set; } = string.Empty;
}

public class BudgetImpactPreviewDto
{
    public Guid CeilingId { get; set; }
    public string ExpenseObjectCode { get; set; } = string.Empty;
    public string ExpenseObjectName { get; set; } = string.Empty;
    public decimal CurrentCeiling { get; set; }
    public decimal ProposedCeiling { get; set; }
    public decimal ReductionAmount { get; set; }
    public decimal NewAvailableBudget { get; set; }
    public int AffectedRequirementsCount { get; set; }
    public int AffectedAreasCount { get; set; }
    public decimal TotalExceededAmount { get; set; }
    public List<BudgetImpactRequirementItemDto> AffectedRequirements { get; set; } = [];
}

public class BudgetImpactRequirementItemDto
{
    public Guid RequirementId { get; set; }
    public string RequirementCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class BudgetMonitoringDto
{
    public Guid Id { get; set; }
    public string ExpenseObjectCode { get; set; } = string.Empty;
    public string ExpenseObjectName { get; set; } = string.Empty;
    public string FundingSource { get; set; } = string.Empty;
    public decimal InitialAmount { get; set; }
    public decimal TotalAdjustments { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal ReservedAmount { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal ExecutedAmount { get; set; }
    public decimal AvailableAmount { get; set; }
    public decimal UtilizationPercentage { get; set; }
    public string Status { get; set; } = "Green";
}

public class AreaConsumptionReportDto
{
    public Guid AreaId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public int RequirementCount { get; set; }
    public decimal ReservedAmount { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal ExecutedAmount { get; set; }
    public decimal TotalConsumption { get; set; }
}

public class CrossExecutionReportDto
{
    public Guid ConsolidatorAreaId { get; set; }
    public string ConsolidatorAreaName { get; set; } = string.Empty;
    public Guid OriginAreaId { get; set; }
    public string OriginAreaName { get; set; } = string.Empty;
    public int RequirementCount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<RequirementDto> Requirements { get; set; } = [];
}
