/**
 * M2 — Interfaces TypeScript que reflejan los DTOs del backend de Planificación.
 * Plantillas, Códigos unificados y Categorías de apoyo.
 */

// ─────────────────────── Funding Sources ───────────────────────

export interface FundingSourceDto {
  id: string;
  organizationId: number;
  code: string;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateFundingSourceDto {
  code: string;
  name: string;
  description?: string | null;
  isActive?: boolean;
}

export interface UpdateFundingSourceDto {
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
}

// ─────────────────────── Planning Templates ───────────────────────

/** Resumen de plantilla para listados. */
export interface PlanningTemplateDto {
  id: string;
  name: string;
  description: string | null;
  version: number;
  isActive: boolean;
  columnCount: number;
  createdAt: string;
}

/** Detalle de plantilla con sus columnas. */
export interface PlanningTemplateDetailDto {
  id: string;
  name: string;
  description: string | null;
  version: number;
  isActive: boolean;
  createdAt: string;
  columns: PlanningTemplateColumnDto[];
}

/** Columna de una plantilla. */
export interface PlanningTemplateColumnDto {
  id: string;
  visibleName: string;
  technicalName: string;
  type: string; // Text | Number | Currency | Select
  required: boolean;
  visible: boolean;
  origin: string; // Base | Custom
  optionsJson: string | null;
  order: number;
  version: number;
  createdBy: string | null;
  createdByName: string | null;
  createdAt: string;
}

export interface CreatePlanningTemplateDto {
  name: string;
  description?: string;
}

export interface UpdatePlanningTemplateDto {
  name: string;
  description?: string;
}

export interface CreatePlanningTemplateColumnDto {
  visibleName: string;
  technicalName: string;
  type: string; // Text | Number | Currency | Select
  required: boolean;
  visible: boolean;
  origin: string; // Base | Custom
  optionsJson?: string;
  order: number;
}

export interface UpdatePlanningTemplateColumnDto {
  visibleName: string;
  type: string;
  required: boolean;
  visible: boolean;
  optionsJson?: string;
  order: number;
}

// ─────────────────────── Códigos unificados (UNSPSC · DGCP) ───────────────────────
//
// Catálogo regulatorio de solo lectura: se carga con el importador de la Maestra.
// Los atributos de gestión (área consolidadora, precio, unidad) viven en el artículo.

export interface UnifiedCodeDto {
  id: string;
  level: string; // Segment | Family | Class | Material
  code: string;
  name: string;
  parentId: string | null;
  englishName: string | null;
  institutionallyEnabled: boolean;
  childCount: number;
}

export interface UnifiedCodeDetailDto {
  id: string;
  level: string;
  code: string;
  name: string;
  parentId: string | null;
  englishName: string | null;
  definition: string | null;
  sourceRole: string | null;
  includedInMaster: boolean | null;
  legacyExpenseObjectCode: string | null;
  legacyExpenseObjectName: string | null;
  source: string | null;
  institutionallyEnabled: boolean;
  aliases: UnifiedCodeAliasDto[];
}

export interface UnifiedCodeAliasDto {
  id: string;
  system: string;
  code: string;
  name: string | null;
}

// ─────────────────────── Planning Cycles ───────────────────────

export interface PlanningCycleDto {
  id: string;
  name: string;
  year: number;
  endYear: number | null;
  status: 'Draft' | 'Published' | 'Closed';
  strategicPlanId: string;
  strategicPlanName: string | null;
  templateId: string;
  templateName: string | null;
  publishedAt: string | null;
  areaCount: number;
  planCountsByStatus: Record<string, number>;
}

export interface PlanningCycleDetailDto extends PlanningCycleDto {
  createdBy: string;
  createdByName: string | null;
  createdAt: string;
  milestones: PlanningCycleMilestoneDto[];
  areas: PlanningCycleAreaDto[];
  budgetCeilings?: BudgetCeilingDto[];
  amendments?: PlanningCycleAmendmentLogDto[];
}

export interface PlanningCycleAmendmentLogDto {
  id: string;
  amendmentType: string;
  justification: string;
  previousValuesJson?: string | null;
  newValuesJson?: string | null;
  createdBy: string;
  createdByName?: string | null;
  createdAt: string;
}

export interface PlanningCycleMilestoneDto {
  id: string;
  name: string;
  date: string;
  kind: 'FormulationDeadline' | 'Review' | 'Approval' | 'Other';
}

export interface PlanningCycleAreaDto {
  id: string;
  areaId: string;
  areaName: string;
}

export interface CreatePlanningCycleDto {
  name: string;
  year: number;
  endYear?: number | null;
  strategicPlanId: string;
  templateId: string;
  milestones: { name: string; date: string; kind: string }[];
  areaIds: string[];
}

export interface UpdatePlanningCycleDto {
  name: string;
  year: number;
  endYear?: number | null;
  strategicPlanId: string;
  templateId: string;
  justification?: string | null;
  milestones: { name: string; date: string; kind: string }[];
  areaIds: string[];
}

export interface EligibleAreaDto {
  areaId: string;
  areaName: string;
}

// ─────────────────────── Requirements & Consolidation (M7) ───────────────────────

export interface PagedResponseDto<T> {
  value: T[];
  items?: T[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalCount?: number;
  totalPages: number;
  firstPage: string | null;
  lastPage: string | null;
  nextPage: string | null;
  previousPage: string | null;
}

export interface RequirementDto {
  id: string;
  code: string;
  cycleId: string;
  planId: string;
  poaProductId: string;
  poaTaskId: string;
  originAreaId: string;
  originAreaName: string;
  consolidatorAreaId: string;
  consolidatorAreaName: string;
  appliedRule: string | null;
  masterProductId: string;
  masterProductCode: string | null;
  masterProductName: string;
  unspscCode: string | null;
  unspscName: string | null;
  expenseObjectCode: string | null;
  expenseObjectName: string | null;
  accountingAccountCode: string | null;
  accountingAccountName: string | null;
  itemType: string | null;
  description: string;
  quantity: number;
  unitOfMeasure: string;
  estimatedUnitPrice: number;
  priceConfirmed: boolean;
  quarter: string;
  requiredDate: string | null;
  fundingSource: string | null;
  priority: string;
  status: string;
  approvedAmount: number | null;
  mergedIntoId: string | null;
  createdAt: string;
  updatedAt: string;
  calculatedTotal: number;
}

export interface RequirementDetailDto extends RequirementDto {
  comments: RequirementCommentDto[];
  decisions: PrioritizationDecisionDto[];
}

export interface RequirementCommentDto {
  id: string;
  requirementId: string;
  userId: string;
  userName: string | null;
  areaName: string | null;
  date: string;
  text: string;
}

export interface PrioritizationDecisionDto {
  id: string;
  requirementId: string;
  requirementCode: string;
  masterProductId: string | null;
  type: string;
  requestedAmount: number;
  approvedAmount: number | null;
  reason: string;
  decidedBy: string;
  decidedByName: string | null;
  decidingAreaId: string;
  date: string;
  level: string;
}

export interface CreateRequirementDto {
  poaTaskId: string;
  masterProductId: string;
  description: string;
  quantity: number;
  unitOfMeasure: string;
  estimatedUnitPrice?: number;
  quarter: string;
  requiredDate?: string | null;
  fundingSource?: string | null;
  priority: string;
}

export interface UpdateRequirementDto {
  masterProductId: string;
  description: string;
  quantity: number;
  unitOfMeasure: string;
  estimatedUnitPrice: number;
  quarter: string;
  requiredDate?: string | null;
  fundingSource?: string | null;
  priority: string;
}

export interface RequirementDecisionDto {
  action: 'Approve' | 'Return' | 'Reschedule';
  approvedAmount?: number | null;
  quarter?: string | null;
  comment: string;
}

export interface TaskRequirementsDto {
  id: string;
  code: string;
  name: string;
  productId: string;
  productCode: string;
  productName: string;
  planId: string;
  cycleId: string;
  areaId: string;
  areaName: string;
}

export interface ConfirmPriceDto {
  price: number;
}

export interface ReduceRequirementDto {
  approvedAmount: number;
  reason: string;
}

export interface MergeRequirementsDto {
  requirementIds: string[];
  newDescription: string;
  reason?: string | null;
}

export interface AddRequirementCommentDto {
  text: string;
}

export interface ConsolidationBoardDto {
  cycleId: string;
  cycleName: string;
  cycleYear: number;
  plansByStatus: Record<string, number>;
  totalApprovedAmount: number;
  consolidatorGroups: ConsolidatorGroupDto[];
}

export interface ConsolidatorGroupDto {
  consolidatorAreaId: string;
  consolidatorAreaName: string;
  requirementCount: number;
  totalAmount: number;
  requirementsByStatus: Record<string, number>;
  requirements: RequirementDto[];
}

export interface ExpenseObjectsBoardPagedDto {
  cycleId: string;
  cycleName: string;
  cycleYear: number;
  totalAmount: number;
  totalApprovedAmount: number;
  totalRequirements: number;
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  expenseObjectGroups: ExpenseObjectDemandGroupDto[];
}

export interface ProductsBoardPagedDto {
  cycleId: string;
  cycleName: string;
  cycleYear: number;
  totalAmount: number;
  totalApprovedAmount: number;
  totalRequirements: number;
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  productGroups: ProductDemandGroupDto[];
}

export interface ConsolidatorAreasBoardPagedDto {
  cycleId: string;
  cycleName: string;
  cycleYear: number;
  plansByStatus: Record<string, number>;
  totalApprovedAmount: number;
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  consolidatorGroups: ConsolidatorGroupDto[];
}

// ─────────────────────── Master Products & Mappings (M8/M9) ───────────────────────

/** Estados de gobierno del artículo. Coinciden con los que valida el backend. */
export type MasterProductState = 'Pending' | 'Active' | 'Requested' | 'Disabled';

/** Origen del área consolidadora. 'Manual' congela la fila frente al motor de reglas. */
export type MasterProductAreaSource = 'None' | 'Imported' | 'Rule' | 'Manual';

export interface MasterProductDto {
  id: string;
  organizationId: number;
  sourceId: string | null;
  code: string;
  name: string;
  originalDescription: string | null;
  englishName: string | null;
  definition: string | null;
  unifiedCodeId: string | null;
  unspscCode: string | null;
  unspscName: string | null;
  itemType: string | null;
  itemSubtype: string | null;
  assetTreatment: string | null;
  measureUnit: string | null;
  referencePrice: number | null;
  currency: string | null;
  priceSource: string | null;
  expenseObjectId: string | null;
  expenseObjectCode: string | null;
  expenseObjectName: string | null;
  accountingAccountId: string | null;
  accountingAccountCode: string | null;
  accountingAccountName: string | null;
  mastersoftAccountCode?: string | null;
  mastersoftAccountName?: string | null;
  consolidatorAreaId: string | null;
  consolidatorAreaName: string | null;
  consolidatorAreaSource: MasterProductAreaSource;
  consolidatorRuleId: string | null;
  consolidatorRuleKey: string | null;
  consolidatorRuleVersion: number | null;
  consolidatorAreaConfidence: number | null;
  areaAmbiguity: boolean;
  alternativeAreasJson: string | null;
  requiresReview: boolean;
  reviewReason: string | null;
  notes: string | null;
  state: MasterProductState;
  duplicateOfMasterProductId: string | null;
  requestedBy: string | null;
  requestedByName: string | null;
  requestedAreaId: string | null;
  requestedAt: string | null;
  requestJustification: string | null;
  version: number;
  createdAt: string;
  updatedAt: string;
}

export interface MasterProductDetailDto extends MasterProductDto {
  unifiedCodeId: string | null;
  expenseObjectId: string | null;
  accountingAccountId: string | null;
  suggestedUnit: string | null;
  referencePrice: number | null;
  aliases: MasterProductAliasDto[];
}

export interface MasterProductAliasDto {
  id: string;
  alias: string;
}

export interface MasterProductSearchItemDto {
  id: string;
  code: string;
  name: string;
  itemType: string | null;
  unspscCode: string | null;
  unspscName: string | null;
  expenseObjectCode: string | null;
  accountingAccountCode: string | null;
  consolidatorAreaId: string | null;
  consolidatorAreaName: string | null;
  suggestedUnit: string | null;
  referencePrice: number | null;
}

export interface CreateMasterProductDto {
  name: string;
  itemType?: string;
  unifiedCodeId?: string;
  expenseObjectId?: string;
  accountingAccountId?: string;
  consolidatorAreaId?: string;
  aliases?: string[];
}

export interface UpdateMasterProductDto {
  name: string;
  itemType?: string;
  suggestedUnit?: string;
  referencePrice?: number;
  aliases?: string[];
}

export interface UpdateMasterProductMappingDto {
  itemType?: string;
  unifiedCodeId?: string;
  expenseObjectId?: string;
  accountingAccountId?: string;
  consolidatorAreaId?: string;
  ratifyConsolidatorArea?: boolean;
  state?: MasterProductState;
  measureUnit?: string;
  referencePrice?: number;
  notes?: string;
}

export interface RatifyConsolidatorAreaDto {
  consolidatorAreaId: string;
}

export interface BulkRatifyByUnspscDto {
  unspscPrefix: string;
  consolidatorAreaId: string;
}

export interface RequestMasterProductDto {
  name: string;
  itemType?: string;
  description?: string;
  suggestedConsolidatorAreaId?: string;
}

export interface MasterProductMappingSummaryDto {
  totalProducts: number;
  activeProducts: number;
  pendingProducts: number;
  requestedProducts: number;
  disabledProducts: number;
  missingExpenseObject: number;
  missingAccountingAccount: number;
  missingConsolidatorArea: number;
  requiresReviewCount: number;
  areaAmbiguityCount: number;
  completeMappingCount: number;
}

// ─────────────────────── Regulatory Catalogs ───────────────────────

export interface ExpenseObjectDto {
  id: string;
  code: string;
  name: string;
  description: string | null;
  level: number;
  parentCode: string | null;
  parentId: string | null;
  isDetail: boolean;
}

export interface AccountingAccountDto {
  id: string;
  code: string;
  name: string;
  isPostable: boolean;
  level: number;
  parentCode: string | null;
  parentId: string | null;
  extendedDescription: string | null;
  mastersoftAccountCode?: string | null;
  mastersoftAccountName?: string | null;
  isMastersoftInherited?: boolean;
  inheritedFromDigecobCode?: string | null;
}

export interface MastersoftAccountDto {
  id: string;
  code: string;
  name: string;
  controlAccountCode?: string | null;
  controlAccountId?: string | null;
  level: number;
  isPostable: boolean;
}

export interface MastersoftDigecobMappingDto {
  id: string;
  digecobAccountId: string;
  digecobAccountCode: string;
  digecobAccountName: string;
  digecobIsPostable: boolean;
  digecobLevel: number;
  mastersoftAccountId?: string | null;
  mastersoftAccountCode?: string | null;
  mastersoftAccountName?: string | null;
  isInherited: boolean;
  inheritedFromDigecobCode?: string | null;
}

export interface CreateMastersoftMappingDto {
  digecobAccountId: string;
  mastersoftAccountId: string;
}

export interface MastersoftImportSummaryDto {
  digecobAccountsProcessed: number;
  mastersoftAccountsCreated: number;
  mastersoftAccountsUpdated: number;
  mappingsCreated: number;
  mappingsUpdated: number;
  warnings: string[];
}

export interface ConsolidatorAreaProfileDto {
  id: string;
  areaCode: string;
  organicGroup: string | null;
  areaName: string;
  organizationalStructureId: string | null;
  mandateSummary: string | null;
  consolidatorKind: string | null;
}

// ─────────────────────── Consolidator Routing Rules Engine ───────────────────────

export interface ConsolidatorRoutingRuleDto {
  id: string;
  ruleKey: string;
  version: number;
  author: string | null;
  ruleDate: string | null;
  supersedesVersion: number | null;
  reversible: boolean;
  priority: number;
  keywords: string | null;
  exclusionKeywords: string | null;
  unspscPrefixes: string | null;
  itemType: string | null;
  consolidatorAreaName: string;
  consolidatorAreaId: string | null;
  justification: string | null;
  isActive: boolean;
  institutionallyValidated: boolean;
  baseConfidence: number;
  forceReview: boolean;
  ruleState: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateConsolidatorRoutingRuleDto {
  ruleKey: string;
  priority?: number;
  keywords?: string;
  exclusionKeywords?: string;
  unspscPrefixes?: string;
  itemType?: string;
  consolidatorAreaName: string;
  consolidatorAreaId?: string;
  justification?: string;
  institutionallyValidated?: boolean;
  baseConfidence?: number;
  forceReview?: boolean;
}

export interface UpdateConsolidatorRoutingRuleMetadataDto {
  justification?: string;
  isActive: boolean;
  ruleState?: string;
}

export interface CreateNewRuleVersionDto extends CreateConsolidatorRoutingRuleDto {}

export interface RulePreviewResultDto {
  ruleId: string;
  ruleKey: string;
  matchedProductsCount: number;
  sampleMatchedProducts: MasterProductDto[];
}

export interface ConsolidatorUnitImpactDto {
  unitId: string;
  unitName: string;
  code: string;
  isConsolidatorArea: boolean;
  affectedRoutingRulesCount: number;
  affectedRuleKeys: string[];
  affectedMasterProductsCount: number;
  hasImpact: boolean;
}

export interface StructureVersionImpactReportDto {
  versionId: string;
  versionNumber: number | null;
  versionName: string;
  description: string;
  totalConsolidatorAreasAffected: number;
  totalRoutingRulesAffected: number;
  totalMasterProductsAffected: number;
  affectedUnits: ConsolidatorUnitImpactDto[];
  requiresReevaluation: boolean;
}

// ─────────────────────── Master Data Import & Export ───────────────────────

export interface MasterDataImportResultDto {
  processedRows: number;
  newProductsCount: number;
  updatedProductsCount: number;
  newExpenseObjectsCount: number;
  newAccountingAccountsCount: number;
  newUnspscCodesCount: number;
  errorMessages: string[];
  warningMessages: string[];
  isDryRun: boolean;
}

// ─────────────────────── Demand Consolidation Board ───────────────────────

export interface ProductDemandGroupDto {
  masterProductId: string;
  masterProductCode: string | null;
  masterProductName: string;
  unspscCode: string | null;
  expenseObjectCode: string | null;
  expenseObjectName: string | null;
  accountingAccountCode: string | null;
  totalQuantity: number;
  requirementCount: number;
  requestingAreaCount: number;
  totalAmount: number;
  totalApprovedAmount: number;
  ceilingInitialAmount?: number | null;
  ceilingCurrentAmount?: number | null;
  ceilingAvailableAmount?: number | null;
  ceilingUtilizationPercentage?: number | null;
  ceilingStatus?: string | null;
  requirements: RequirementDto[];
}

export interface ExpenseObjectDemandGroupDto {
  expenseObjectCode: string;
  expenseObjectName: string | null;
  totalQuantity: number;
  requirementCount: number;
  requestingAreaCount: number;
  totalAmount: number;
  totalApprovedAmount: number;
  ceilingInitialAmount?: number | null;
  ceilingCurrentAmount?: number | null;
  ceilingAvailableAmount?: number | null;
  ceilingUtilizationPercentage?: number | null;
  ceilingStatus?: string | null;
  productGroups: ProductDemandGroupDto[];
  requirements: RequirementDto[];
}

export interface ProductDemandBoardDto {
  cycleId: string;
  cycleName: string;
  cycleYear: number;
  totalProducts: number;
  totalRequirements: number;
  totalAmount: number;
  totalApprovedAmount: number;
  expenseObjectGroups?: ExpenseObjectDemandGroupDto[];
  productGroups: ProductDemandGroupDto[];
}

// ─────────────────────── Budget Ceilings (Techos Presupuestarios - M10) ───────────────────────

export interface BudgetCeilingDto {
  id: string;
  organizationId: number;
  fiscalYear: number;
  expenseObjectId: string;
  expenseObjectCode: string;
  expenseObjectName: string;
  expenseObjectLevel?: number;
  expenseObjectLevelName?: string;
  fundingSource: string;
  budgetProgram: string | null;
  initialAmount: number;
  currentAmount: number;
  reservedAmount: number;
  plannedAmount: number;
  committedAmount: number;
  executedAmount: number;
  availableAmount: number;
  utilizationPercentage: number;
  thresholdPercentage: number;
  validationBehavior: string;
  status: string; // Green | Yellow | Red
  createdAt: string;
  updatedAt: string;
}

export interface BudgetAdjustmentDto {
  id: string;
  budgetCeilingId: string;
  adjustmentType: string; // Increase | Reduction
  amount: number;
  reason: string;
  documentUrl: string | null;
  createdBy: string;
  createdByName: string | null;
  createdAt: string;
}

export interface ProductConsumptionDto {
  masterProductId: string;
  masterProductCode: string;
  masterProductName: string;
  totalAmount: number;
  percentageOfCeiling: number;
}

export interface AreaConsumptionDto {
  areaId: string;
  areaName: string;
  requirementCount: number;
  totalAmount: number;
  percentageOfCeiling: number;
}

export interface ChildExpenseObjectConsumptionDto {
  expenseObjectCode: string;
  expenseObjectName: string;
  level: number;
  totalAmount: number;
  percentageOfCeiling: number;
}

export interface BudgetCeilingDetailDto extends BudgetCeilingDto {
  totalAdjustments: number;
  unconfirmedAmount: number;
  adjustments: BudgetAdjustmentDto[];
  consumptionByProduct: ProductConsumptionDto[];
  consumptionByArea: AreaConsumptionDto[];
  consumptionByChildExpenseObject?: ChildExpenseObjectConsumptionDto[];
  recentRequirements: RequirementDto[];
}

export interface CreateBudgetCeilingDto {
  fiscalYear: number;
  expenseObjectId: string;
  fundingSource: string;
  budgetProgram?: string;
  initialAmount: number;
  thresholdPercentage?: number;
  validationBehavior?: string;
}

export interface UpdateBudgetCeilingDto {
  thresholdPercentage: number;
  validationBehavior: string;
  budgetProgram?: string;
}

export interface CreateBudgetAdjustmentDto {
  adjustmentType: string; // Increase | Reduction
  amount: number;
  reason: string;
  documentUrl?: string;
}

export interface BudgetValidationRequestDto {
  masterProductId: string;
  requiredAmount: number;
  fundingSource?: string;
  fiscalYear?: number;
}

export interface BudgetValidationResultDto {
  isValid: boolean;
  exceedsCeiling: boolean;
  ceilingId: string | null;
  expenseObjectCode: string | null;
  expenseObjectName: string | null;
  currentCeiling: number;
  availableBudget: number;
  requestedAmount: number;
  exceededAmount: number;
  validationBehavior: string;
  message: string;
}

export interface BudgetImpactRequirementItemDto {
  requirementId: string;
  requirementCode: string;
  productName: string;
  areaName: string;
  amount: number;
  status: string;
}

export interface BudgetImpactPreviewDto {
  ceilingId: string;
  expenseObjectCode: string;
  expenseObjectName: string;
  currentCeiling: number;
  proposedCeiling: number;
  reductionAmount: number;
  newAvailableBudget: number;
  affectedRequirementsCount: number;
  affectedAreasCount: number;
  totalExceededAmount: number;
  affectedRequirements: BudgetImpactRequirementItemDto[];
}

export interface BudgetMonitoringDto {
  id: string;
  expenseObjectCode: string;
  expenseObjectName: string;
  fundingSource: string;
  initialAmount: number;
  totalAdjustments: number;
  currentAmount: number;
  reservedAmount: number;
  plannedAmount: number;
  committedAmount: number;
  executedAmount: number;
  availableAmount: number;
  utilizationPercentage: number;
  status: string;
}

export interface AreaConsumptionReportDto {
  areaId: string;
  areaName: string;
  requirementCount: number;
  reservedAmount: number;
  plannedAmount: number;
  committedAmount: number;
  executedAmount: number;
  totalConsumption: number;
}

export interface CrossExecutionReportDto {
  consolidatorAreaId: string;
  consolidatorAreaName: string;
  originAreaId: string;
  originAreaName: string;
  requirementCount: number;
  totalAmount: number;
  requirements: RequirementDto[];
}
