namespace WorkshopApi.DTOs;

// ============ REPORT & DASHBOARD DTOs ============

public class DashboardDto
{
    public MaterialsSummaryDto MaterialsSummary { get; set; } = new();
    public ProductsSummaryDto ProductsSummary { get; set; } = new();
    public FinishedProductSummaryDto FinishedProductsSummary { get; set; } = new();
    public List<MaterialBalanceDto> LowStockMaterials { get; set; } = new();
    public List<OperationHistoryItemDto> RecentOperations { get; set; } = new();
}

public class MaterialsSummaryDto
{
    public int TotalMaterials { get; set; }
    public int ActiveMaterials { get; set; }
    public decimal TotalValue { get; set; }
    public int LowStockCount { get; set; }
}

public class ProductsSummaryDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int TotalProduced { get; set; }
}

public class MaterialMovementReportDto
{
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialUnit { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal TotalReceipts { get; set; }
    public decimal TotalWriteOffs { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<MaterialMovementItemDto> Movements { get; set; } = new();
}

public class MaterialMovementItemDto
{
    public DateTime Date { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Balance { get; set; }
    public string? Reference { get; set; }
}

public class ProductionReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalProductions { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public List<ProductionReportItemDto> Items { get; set; } = new();
}

public class ProductionReportItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AverageCostPerUnit { get; set; }
}

public class SalesReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public List<SalesReportItemDto> Items { get; set; } = new();
}

public class SalesReportItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
}

public class FinancialSummaryDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalMaterialCosts { get; set; }
    public decimal TotalSalesRevenue { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal MaterialsOnHandValue { get; set; }
    public decimal FinishedGoodsValue { get; set; }
    public decimal TotalInventoryValue { get; set; }
}
