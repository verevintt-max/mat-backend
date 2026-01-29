using Microsoft.EntityFrameworkCore;
using WorkshopApi.Data;
using WorkshopApi.DTOs;
using WorkshopApi.Models;

namespace WorkshopApi.Services;

public class ReportService
{
    private readonly WorkshopDbContext _context;
    private readonly MaterialService _materialService;
    private readonly FinishedProductService _finishedProductService;
    private readonly OperationHistoryService _historyService;

    public ReportService(
        WorkshopDbContext context,
        MaterialService materialService,
        FinishedProductService finishedProductService,
        OperationHistoryService historyService)
    {
        _context = context;
        _materialService = materialService;
        _finishedProductService = finishedProductService;
        _historyService = historyService;
    }

    /// <summary>
    /// Получить данные для дашборда
    /// </summary>
    public async Task<DashboardDto> GetDashboardAsync(int organizationId)
    {
        // Сводка по материалам
        var materials = await _context.Materials
            .Where(m => m.OrganizationId == organizationId)
            .ToListAsync();
        var balances = await _materialService.GetAllBalancesAsync(organizationId, true);
        
        var materialsSummary = new MaterialsSummaryDto
        {
            TotalMaterials = materials.Count,
            ActiveMaterials = materials.Count(m => !m.IsArchived),
            TotalValue = balances.Sum(b => b.TotalValue),
            LowStockCount = balances.Count(b => b.IsBelowMinimum)
        };

        // Сводка по изделиям
        var products = await _context.Products
            .Where(p => p.OrganizationId == organizationId)
            .ToListAsync();
        var productions = await _context.Productions
            .Where(p => p.OrganizationId == organizationId && !p.IsCancelled)
            .ToListAsync();
        
        var productsSummary = new ProductsSummaryDto
        {
            TotalProducts = products.Count,
            ActiveProducts = products.Count(p => !p.IsArchived),
            TotalProduced = productions.Sum(p => p.Quantity)
        };

        // Сводка по готовой продукции
        var finishedProductsSummary = await _finishedProductService.GetSummaryAsync(organizationId);

        // Материалы с низким остатком
        var lowStockMaterials = balances.Where(b => b.IsBelowMinimum).ToList();

        // Последние операции
        var recentOperations = await _historyService.GetRecentAsync(organizationId, 10);

        return new DashboardDto
        {
            MaterialsSummary = materialsSummary,
            ProductsSummary = productsSummary,
            FinishedProductsSummary = finishedProductsSummary,
            LowStockMaterials = lowStockMaterials,
            RecentOperations = recentOperations
        };
    }

    /// <summary>
    /// Отчет о движении материала
    /// </summary>
    public async Task<MaterialMovementReportDto> GetMaterialMovementReportAsync(int organizationId, int materialId, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var material = await _context.Materials
            .Where(m => m.OrganizationId == organizationId)
            .FirstOrDefaultAsync(m => m.Id == materialId);
        if (material == null)
            throw new InvalidOperationException($"Материал с ID {materialId} не найден");

        var startDate = dateFrom ?? DateTime.MinValue;
        var endDate = dateTo ?? DateTime.MaxValue;

        // Получаем все поступления
        var receipts = await _context.MaterialReceipts
            .Where(r => r.MaterialId == materialId && r.ReceiptDate >= startDate && r.ReceiptDate <= endDate)
            .OrderBy(r => r.ReceiptDate)
            .ToListAsync();

        // Получаем все списания
        var writeOffs = await _context.MaterialWriteOffs
            .Where(w => w.MaterialId == materialId && w.CreatedAt >= startDate && w.CreatedAt <= endDate)
            .Include(w => w.Production)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync();

        // Начальный остаток (до периода)
        var receiptsBefore = await _context.MaterialReceipts
            .Where(r => r.MaterialId == materialId && r.ReceiptDate < startDate)
            .SumAsync(r => r.Quantity);
        var writeOffsBefore = await _context.MaterialWriteOffs
            .Where(w => w.MaterialId == materialId && w.CreatedAt < startDate)
            .SumAsync(w => w.Quantity);
        var openingBalance = receiptsBefore - writeOffsBefore;

        var movements = new List<MaterialMovementItemDto>();
        decimal balance = openingBalance;

        // Объединяем и сортируем по дате
        var allMovements = new List<(DateTime Date, string Type, decimal Qty, string? Ref)>();

        foreach (var r in receipts)
        {
            allMovements.Add((r.ReceiptDate, "Поступление", r.Quantity, r.BatchNumber));
        }

        foreach (var w in writeOffs)
        {
            allMovements.Add((w.CreatedAt, "Списание", -w.Quantity, $"Производство #{w.Production.BatchNumber}"));
        }

        foreach (var m in allMovements.OrderBy(m => m.Date))
        {
            balance += m.Qty;
            movements.Add(new MaterialMovementItemDto
            {
                Date = m.Date,
                OperationType = m.Type,
                Quantity = Math.Abs(m.Qty),
                Balance = balance,
                Reference = m.Ref
            });
        }

        return new MaterialMovementReportDto
        {
            MaterialId = materialId,
            MaterialName = material.Name,
            MaterialUnit = material.Unit,
            OpeningBalance = openingBalance,
            TotalReceipts = receipts.Sum(r => r.Quantity),
            TotalWriteOffs = writeOffs.Sum(w => w.Quantity),
            ClosingBalance = balance,
            Movements = movements
        };
    }

    /// <summary>
    /// Отчет о производстве за период
    /// </summary>
    public async Task<ProductionReportDto> GetProductionReportAsync(int organizationId, DateTime dateFrom, DateTime dateTo)
    {
        var productions = await _context.Productions
            .Where(p => p.OrganizationId == organizationId && !p.IsCancelled && p.ProductionDate >= dateFrom && p.ProductionDate <= dateTo)
            .Include(p => p.Product)
            .ToListAsync();

        var items = productions
            .GroupBy(p => p.ProductId)
            .Select(g => new ProductionReportItemDto
            {
                ProductId = g.Key,
                ProductName = g.First().Product.Name,
                Quantity = g.Sum(p => p.Quantity),
                TotalCost = g.Sum(p => p.TotalCost),
                AverageCostPerUnit = g.Sum(p => p.Quantity) > 0 
                    ? Math.Round(g.Sum(p => p.TotalCost) / g.Sum(p => p.Quantity), 2) 
                    : 0
            })
            .OrderByDescending(i => i.Quantity)
            .ToList();

        return new ProductionReportDto
        {
            StartDate = dateFrom,
            EndDate = dateTo,
            TotalProductions = productions.Count,
            TotalQuantity = productions.Sum(p => p.Quantity),
            TotalCost = productions.Sum(p => p.TotalCost),
            Items = items
        };
    }

    /// <summary>
    /// Отчет о продажах за период
    /// </summary>
    public async Task<SalesReportDto> GetSalesReportAsync(int organizationId, DateTime dateFrom, DateTime dateTo)
    {
        var sales = await _context.FinishedProducts
            .Where(fp => fp.OrganizationId == organizationId && 
                        fp.Status == FinishedProductStatus.Sold && 
                        fp.SaleDate >= dateFrom && fp.SaleDate <= dateTo)
            .Include(fp => fp.Production)
                .ThenInclude(p => p.Product)
            .ToListAsync();

        var items = sales
            .GroupBy(fp => fp.Production.ProductId)
            .Select(g => new SalesReportItemDto
            {
                ProductId = g.Key,
                ProductName = g.First().Production.Product.Name,
                Quantity = g.Count(),
                Revenue = g.Sum(fp => fp.SalePrice ?? 0),
                Cost = g.Sum(fp => fp.CostPerUnit),
                Profit = g.Sum(fp => (fp.SalePrice ?? 0) - fp.CostPerUnit)
            })
            .OrderByDescending(i => i.Revenue)
            .ToList();

        var totalRevenue = sales.Sum(fp => fp.SalePrice ?? 0);
        var totalCost = sales.Sum(fp => fp.CostPerUnit);

        return new SalesReportDto
        {
            StartDate = dateFrom,
            EndDate = dateTo,
            TotalSales = sales.Count,
            TotalRevenue = totalRevenue,
            TotalCost = totalCost,
            TotalProfit = totalRevenue - totalCost,
            ProfitMargin = totalRevenue > 0 ? Math.Round((totalRevenue - totalCost) / totalRevenue * 100, 2) : 0,
            Items = items
        };
    }

    /// <summary>
    /// Финансовая сводка
    /// </summary>
    public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(int organizationId, DateTime dateFrom, DateTime dateTo)
    {
        // Затраты на материалы за период
        var materialCosts = await _context.MaterialReceipts
            .Where(r => r.Material.OrganizationId == organizationId && r.ReceiptDate >= dateFrom && r.ReceiptDate <= dateTo)
            .SumAsync(r => r.TotalPrice);

        // Выручка от продаж
        var sales = await _context.FinishedProducts
            .Where(fp => fp.OrganizationId == organizationId && 
                        fp.Status == FinishedProductStatus.Sold && 
                        fp.SaleDate >= dateFrom && fp.SaleDate <= dateTo)
            .ToListAsync();

        var salesRevenue = sales.Sum(fp => fp.SalePrice ?? 0);

        // Стоимость материалов на складе
        var balances = await _materialService.GetAllBalancesAsync(organizationId, true);
        var materialsOnHandValue = balances.Sum(b => b.TotalValue);

        // Стоимость готовой продукции на складе
        var finishedGoodsValue = await _context.FinishedProducts
            .Where(fp => fp.OrganizationId == organizationId && fp.Status == FinishedProductStatus.InStock)
            .SumAsync(fp => fp.CostPerUnit);

        var grossProfit = salesRevenue - materialCosts;

        return new FinancialSummaryDto
        {
            StartDate = dateFrom,
            EndDate = dateTo,
            TotalMaterialCosts = materialCosts,
            TotalSalesRevenue = salesRevenue,
            GrossProfit = grossProfit,
            ProfitMargin = salesRevenue > 0 ? Math.Round(grossProfit / salesRevenue * 100, 2) : 0,
            MaterialsOnHandValue = materialsOnHandValue,
            FinishedGoodsValue = finishedGoodsValue,
            TotalInventoryValue = materialsOnHandValue + finishedGoodsValue
        };
    }
}
