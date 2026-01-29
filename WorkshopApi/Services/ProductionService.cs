using Microsoft.EntityFrameworkCore;
using WorkshopApi.Controllers;
using WorkshopApi.Data;
using WorkshopApi.DTOs;
using WorkshopApi.Models;

namespace WorkshopApi.Services;

public class ProductionService
{
    private readonly WorkshopDbContext _context;
    private readonly OperationHistoryService _historyService;
    private readonly MaterialService _materialService;

    public ProductionService(
        WorkshopDbContext context,
        OperationHistoryService historyService,
        MaterialService materialService)
    {
        _context = context;
        _historyService = historyService;
        _materialService = materialService;
    }

    public async Task<List<ProductionListItemDto>> GetAllAsync(int organizationId, int? productId = null, DateTime? dateFrom = null, DateTime? dateTo = null, bool includeCancelled = false)
    {
        var query = _context.Productions
            .Where(p => p.OrganizationId == organizationId)
            .Include(p => p.Product)
            .Include(p => p.FinishedProducts)
            .AsQueryable();

        if (!includeCancelled)
            query = query.Where(p => !p.IsCancelled);

        if (productId.HasValue)
            query = query.Where(p => p.ProductId == productId.Value);

        if (dateFrom.HasValue)
            query = query.Where(p => p.ProductionDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(p => p.ProductionDate <= dateTo.Value);

        return await query
            .OrderByDescending(p => p.ProductionDate)
            .ThenByDescending(p => p.Id)
            .Select(p => new ProductionListItemDto
            {
                Id = p.Id,
                ProductId = p.ProductId,
                ProductName = p.Product.Name,
                Quantity = p.Quantity,
                ProductionDate = p.ProductionDate,
                BatchNumber = p.BatchNumber,
                CostPerUnit = p.CostPerUnit,
                TotalCost = p.TotalCost,
                RecommendedPricePerUnit = p.RecommendedPricePerUnit,
                IsCancelled = p.IsCancelled,
                InStockCount = p.FinishedProducts.Count(fp => fp.Status == FinishedProductStatus.InStock),
                SoldCount = p.FinishedProducts.Count(fp => fp.Status == FinishedProductStatus.Sold)
            })
            .ToListAsync();
    }

    public async Task<ProductionResponseDto?> GetByIdAsync(int organizationId, int id)
    {
        var production = await _context.Productions
            .Where(p => p.OrganizationId == organizationId)
            .Include(p => p.Product)
            .Include(p => p.MaterialWriteOffs)
                .ThenInclude(w => w.Material)
            .Include(p => p.FinishedProducts)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (production == null) return null;

        return new ProductionResponseDto
        {
            Id = production.Id,
            ProductId = production.ProductId,
            ProductName = production.Product.Name,
            ProductCategory = production.Product.Category,
            Quantity = production.Quantity,
            ProductionDate = production.ProductionDate,
            BatchNumber = production.BatchNumber,
            QrCode = production.QrCode,
            CostPerUnit = production.CostPerUnit,
            TotalCost = production.TotalCost,
            RecommendedPricePerUnit = production.RecommendedPricePerUnit,
            PhotoPath = production.PhotoPath,
            Comment = production.Comment,
            IsCancelled = production.IsCancelled,
            CancelledAt = production.CancelledAt,
            CreatedAt = production.CreatedAt,
            MaterialWriteOffs = production.MaterialWriteOffs
                .GroupBy(w => w.MaterialId)
                .Select(g => new MaterialWriteOffDto
                {
                    MaterialId = g.Key,
                    MaterialName = g.First().Material.Name,
                    MaterialUnit = g.First().Material.Unit,
                    Quantity = g.Sum(w => w.Quantity),
                    UnitPrice = g.First().UnitPrice,
                    TotalPrice = g.Sum(w => w.Quantity * w.UnitPrice)
                })
                .ToList(),
            InStockCount = production.FinishedProducts.Count(fp => fp.Status == FinishedProductStatus.InStock),
            SoldCount = production.FinishedProducts.Count(fp => fp.Status == FinishedProductStatus.Sold),
            WrittenOffCount = production.FinishedProducts.Count(fp => fp.Status == FinishedProductStatus.WrittenOff)
        };
    }

    /// <summary>
    /// Проверка наличия материалов для производства
    /// </summary>
    public async Task<ProductionCheckResultDto> CheckAvailabilityAsync(int organizationId, int productId, int quantity)
    {
        var product = await _context.Products
            .Where(p => p.OrganizationId == organizationId)
            .Include(p => p.RecipeItems)
                .ThenInclude(r => r.Material)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            throw new InvalidOperationException($"Изделие с ID {productId} не найдено");

        var result = new ProductionCheckResultDto
        {
            CanProduce = true,
            Materials = new List<MaterialAvailabilityDto>()
        };

        foreach (var item in product.RecipeItems)
        {
            var balance = await _materialService.GetMaterialBalanceAsync(item.MaterialId);
            var requiredQuantity = item.Quantity * quantity;

            var availability = new MaterialAvailabilityDto
            {
                MaterialId = item.MaterialId,
                MaterialName = item.Material.Name,
                MaterialUnit = item.Material.Unit,
                RequiredQuantity = requiredQuantity,
                AvailableQuantity = balance.CurrentStock,
                Shortage = Math.Max(0, requiredQuantity - balance.CurrentStock),
                IsAvailable = balance.CurrentStock >= requiredQuantity
            };

            result.Materials.Add(availability);

            if (!availability.IsAvailable)
            {
                result.CanProduce = false;
                result.Warnings.Add($"Не хватает материала '{item.Material.Name}': нужно {requiredQuantity} {item.Material.Unit}, доступно {balance.CurrentStock} {item.Material.Unit}");
            }
        }

        // Используем себестоимость и рек. цену из карточки изделия (ручной ввод)
        var costPerUnit = product.EstimatedCost ?? 0;
        result.EstimatedCostPerUnit = costPerUnit;
        result.EstimatedTotalCost = costPerUnit * quantity;
        result.RecommendedPricePerUnit = product.RecommendedPrice;

        return result;
    }

    /// <summary>
    /// Создание производства с автоматическим списанием материалов (FIFO)
    /// </summary>
    public async Task<ProductionResponseDto> CreateAsync(OrganizationContext ctx, ProductionCreateDto dto)
    {
        // Проверяем доступность
        var check = await CheckAvailabilityAsync(ctx.OrganizationId, dto.ProductId, dto.Quantity);
        if (!check.CanProduce)
            throw new InvalidOperationException($"Недостаточно материалов: {string.Join("; ", check.Warnings)}");

        var product = await _context.Products
            .Where(p => p.OrganizationId == ctx.OrganizationId)
            .Include(p => p.RecipeItems)
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

        if (product == null)
            throw new InvalidOperationException($"Изделие с ID {dto.ProductId} не найдено");

        // Генерируем номер партии
        var batchNumber = await GenerateBatchNumberAsync(ctx.OrganizationId);

        // Используем себестоимость и рек. цену из карточки изделия
        var costPerUnit = product.EstimatedCost ?? 0;
        var recommendedPricePerUnit = product.RecommendedPrice;

        var production = new Production
        {
            OrganizationId = ctx.OrganizationId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            ProductionDate = dto.ProductionDate ?? DateTime.UtcNow,
            BatchNumber = batchNumber,
            CostPerUnit = costPerUnit,
            TotalCost = costPerUnit * dto.Quantity,
            RecommendedPricePerUnit = recommendedPricePerUnit,
            Comment = dto.Comment,
            PhotoPath = dto.PhotoPath
        };

        _context.Productions.Add(production);
        await _context.SaveChangesAsync();

        // Списываем материалы по FIFO
        await WriteOffMaterialsFifoAsync(ctx.OrganizationId, production, product.RecipeItems.ToList(), dto.Quantity);

        // Создаем готовую продукцию
        for (int i = 0; i < dto.Quantity; i++)
        {
            var finishedProduct = new FinishedProduct
            {
                OrganizationId = ctx.OrganizationId,
                ProductionId = production.Id,
                Status = FinishedProductStatus.InStock,
                CostPerUnit = production.CostPerUnit,
                RecommendedPrice = production.RecommendedPricePerUnit
            };
            _context.FinishedProducts.Add(finishedProduct);
        }

        // Генерируем QR-код
        production.QrCode = GenerateQrCode(production);

        await _context.SaveChangesAsync();

        await _historyService.LogAsync(
            ctx,
            OperationTypes.ProductionCreate,
            "Production",
            production.Id,
            product.Name,
            dto.Quantity,
            production.TotalCost,
            $"Произведено: {product.Name}, {dto.Quantity} шт, партия {batchNumber}"
        );

        return (await GetByIdAsync(ctx.OrganizationId, production.Id))!;
    }

    /// <summary>
    /// Отмена производства с возвратом материалов
    /// </summary>
    public async Task<bool> CancelAsync(OrganizationContext ctx, int id)
    {
        var production = await _context.Productions
            .Where(p => p.OrganizationId == ctx.OrganizationId)
            .Include(p => p.Product)
            .Include(p => p.MaterialWriteOffs)
            .Include(p => p.FinishedProducts)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (production == null) return false;

        if (production.IsCancelled)
            throw new InvalidOperationException("Производство уже отменено");

        // Проверяем, нет ли проданных или списанных изделий
        var nonInStockCount = production.FinishedProducts.Count(fp => fp.Status != FinishedProductStatus.InStock);
        if (nonInStockCount > 0)
            throw new InvalidOperationException($"Невозможно отменить производство: {nonInStockCount} изделий уже продано или списано");

        // Возвращаем материалы (удаляем записи списания)
        _context.MaterialWriteOffs.RemoveRange(production.MaterialWriteOffs);

        // Удаляем готовую продукцию
        _context.FinishedProducts.RemoveRange(production.FinishedProducts);

        production.IsCancelled = true;
        production.CancelledAt = DateTime.UtcNow;
        production.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _historyService.LogAsync(
            ctx,
            OperationTypes.ProductionCancel,
            "Production",
            production.Id,
            production.Product.Name,
            production.Quantity,
            production.TotalCost,
            $"Отменено производство: {production.Product.Name}, партия {production.BatchNumber}"
        );

        return true;
    }

    /// <summary>
    /// Полное удаление производства из базы данных
    /// </summary>
    public async Task<bool> DeleteAsync(OrganizationContext ctx, int id)
    {
        var production = await _context.Productions
            .Where(p => p.OrganizationId == ctx.OrganizationId)
            .Include(p => p.Product)
            .Include(p => p.MaterialWriteOffs)
            .Include(p => p.FinishedProducts)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (production == null) return false;

        // Проверяем, нет ли проданных или списанных изделий
        var nonInStockCount = production.FinishedProducts.Count(fp => fp.Status != FinishedProductStatus.InStock);
        if (nonInStockCount > 0)
            throw new InvalidOperationException($"Невозможно удалить производство: {nonInStockCount} изделий уже продано или списано");

        var productName = production.Product.Name;
        var batchNumber = production.BatchNumber;
        var quantity = production.Quantity;
        var totalCost = production.TotalCost;

        // Удаляем записи списания материалов
        _context.MaterialWriteOffs.RemoveRange(production.MaterialWriteOffs);

        // Удаляем готовую продукцию
        _context.FinishedProducts.RemoveRange(production.FinishedProducts);

        // Удаляем само производство
        _context.Productions.Remove(production);

        await _context.SaveChangesAsync();

        await _historyService.LogAsync(
            ctx,
            OperationTypes.ProductionCancel,
            "Production",
            id,
            productName,
            quantity,
            totalCost,
            $"Удалено производство: {productName}, партия {batchNumber}"
        );

        return true;
    }

    /// <summary>
    /// Списание материалов по методу FIFO
    /// </summary>
    private async Task WriteOffMaterialsFifoAsync(int organizationId, Production production, List<RecipeItem> recipeItems, int quantity)
    {
        foreach (var item in recipeItems)
        {
            var requiredQuantity = item.Quantity * quantity;

            // Получаем поступления с остатками по FIFO (сначала старые)
            var receipts = await _context.MaterialReceipts
                .Where(r => r.MaterialId == item.MaterialId && r.Material.OrganizationId == organizationId)
                .Include(r => r.WriteOffs)
                .OrderBy(r => r.ReceiptDate)
                .ThenBy(r => r.Id)
                .ToListAsync();

            decimal remainingToWriteOff = requiredQuantity;

            foreach (var receipt in receipts)
            {
                if (remainingToWriteOff <= 0) break;

                var usedFromReceipt = receipt.WriteOffs.Sum(w => w.Quantity);
                var availableInReceipt = receipt.Quantity - usedFromReceipt;

                if (availableInReceipt <= 0) continue;

                var toWriteOff = Math.Min(availableInReceipt, remainingToWriteOff);

                var writeOff = new MaterialWriteOff
                {
                    ProductionId = production.Id,
                    MaterialReceiptId = receipt.Id,
                    MaterialId = item.MaterialId,
                    Quantity = toWriteOff,
                    UnitPrice = receipt.UnitPrice
                };

                _context.MaterialWriteOffs.Add(writeOff);
                remainingToWriteOff -= toWriteOff;
            }

            if (remainingToWriteOff > 0)
                throw new InvalidOperationException($"Недостаточно материала '{item.Material.Name}' на складе");
        }

        await _context.SaveChangesAsync();
    }

    private async Task<string> GenerateBatchNumberAsync(int organizationId)
    {
        var today = DateTime.UtcNow.Date;
        var todayProductions = await _context.Productions
            .Where(p => p.OrganizationId == organizationId)
            .CountAsync(p => p.ProductionDate.Date == today);

        return $"P{DateTime.UtcNow:yyyyMMdd}-{(todayProductions + 1):D3}";
    }

    private static string GenerateQrCode(Production production)
    {
        // Формируем данные для QR-кода
        return $"PROD|{production.BatchNumber}|{production.ProductId}|{production.Quantity}|{production.ProductionDate:yyyyMMdd}";
    }
}
