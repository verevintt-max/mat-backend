using Microsoft.EntityFrameworkCore;
using WorkshopApi.Controllers;
using WorkshopApi.Data;
using WorkshopApi.DTOs;
using WorkshopApi.Models;

namespace WorkshopApi.Services;

public class FinishedProductService
{
    private readonly WorkshopDbContext _context;
    private readonly OperationHistoryService _historyService;

    public FinishedProductService(WorkshopDbContext context, OperationHistoryService historyService)
    {
        _context = context;
        _historyService = historyService;
    }

    public async Task<List<FinishedProductListItemDto>> GetAllAsync(int organizationId, string? status = null, int? productId = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var query = _context.FinishedProducts
            .Where(fp => fp.OrganizationId == organizationId)
            .Include(fp => fp.Production)
                .ThenInclude(p => p.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(fp => fp.Status == status);

        if (productId.HasValue)
            query = query.Where(fp => fp.Production.ProductId == productId.Value);

        if (dateFrom.HasValue)
            query = query.Where(fp => fp.Production.ProductionDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(fp => fp.Production.ProductionDate <= dateTo.Value);

        return await query
            .OrderByDescending(fp => fp.CreatedAt)
            .Select(fp => new FinishedProductListItemDto
            {
                Id = fp.Id,
                ProductionId = fp.ProductionId,
                ProductName = fp.Production.Product.Name,
                ProductCategory = fp.Production.Product.Category,
                BatchNumber = fp.Production.BatchNumber,
                ProductionDate = fp.Production.ProductionDate,
                Status = fp.Status,
                StatusDisplay = GetStatusDisplay(fp.Status),
                CostPerUnit = fp.CostPerUnit,
                RecommendedPrice = fp.RecommendedPrice,
                SalePrice = fp.SalePrice,
                Client = fp.Client,
                SaleDate = fp.SaleDate
            })
            .ToListAsync();
    }

    public async Task<FinishedProductResponseDto?> GetByIdAsync(int organizationId, int id)
    {
        var fp = await _context.FinishedProducts
            .Where(f => f.OrganizationId == organizationId)
            .Include(f => f.Production)
                .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fp == null) return null;

        return MapToResponseDto(fp);
    }

    /// <summary>
    /// Продажа изделия
    /// </summary>
    public async Task<FinishedProductResponseDto?> SellAsync(OrganizationContext ctx, int id, SellProductDto dto)
    {
        var fp = await _context.FinishedProducts
            .Where(f => f.OrganizationId == ctx.OrganizationId)
            .Include(f => f.Production)
                .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fp == null) return null;

        if (fp.Status != FinishedProductStatus.InStock)
            throw new InvalidOperationException($"Изделие не на складе (текущий статус: {GetStatusDisplay(fp.Status)})");

        fp.Status = FinishedProductStatus.Sold;
        fp.SalePrice = dto.SalePrice;
        fp.Client = dto.Client;
        fp.SaleDate = dto.SaleDate ?? DateTime.UtcNow;
        fp.Comment = dto.Comment;
        fp.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var profit = dto.SalePrice - fp.CostPerUnit;

        await _historyService.LogAsync(
            ctx,
            OperationTypes.Sale,
            "FinishedProduct",
            fp.Id,
            fp.Production.Product.Name,
            1,
            dto.SalePrice,
            $"Продажа: {fp.Production.Product.Name}, партия {fp.Production.BatchNumber}, цена {dto.SalePrice} руб., прибыль {profit} руб."
        );

        return MapToResponseDto(fp);
    }

    /// <summary>
    /// Списание изделия как брак
    /// </summary>
    public async Task<FinishedProductResponseDto?> WriteOffAsync(OrganizationContext ctx, int id, WriteOffProductDto dto)
    {
        var fp = await _context.FinishedProducts
            .Where(f => f.OrganizationId == ctx.OrganizationId)
            .Include(f => f.Production)
                .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fp == null) return null;

        if (fp.Status != FinishedProductStatus.InStock)
            throw new InvalidOperationException($"Изделие не на складе (текущий статус: {GetStatusDisplay(fp.Status)})");

        fp.Status = FinishedProductStatus.WrittenOff;
        fp.WriteOffReason = dto.Reason;
        fp.Comment = dto.Comment;
        fp.SaleDate = DateTime.UtcNow;
        fp.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _historyService.LogAsync(
            ctx,
            OperationTypes.WriteOff,
            "FinishedProduct",
            fp.Id,
            fp.Production.Product.Name,
            1,
            fp.CostPerUnit,
            $"Списание: {fp.Production.Product.Name}, партия {fp.Production.BatchNumber}, причина: {dto.Reason}"
        );

        return MapToResponseDto(fp);
    }

    /// <summary>
    /// Возврат на склад
    /// </summary>
    public async Task<FinishedProductResponseDto?> ReturnToStockAsync(OrganizationContext ctx, int id)
    {
        var fp = await _context.FinishedProducts
            .Where(f => f.OrganizationId == ctx.OrganizationId)
            .Include(f => f.Production)
                .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fp == null) return null;

        if (fp.Status == FinishedProductStatus.InStock)
            throw new InvalidOperationException("Изделие уже на складе");

        var previousStatus = fp.Status;

        fp.Status = FinishedProductStatus.InStock;
        fp.SalePrice = null;
        fp.Client = null;
        fp.SaleDate = null;
        fp.WriteOffReason = null;
        fp.Comment = null;
        fp.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _historyService.LogAsync(
            ctx,
            OperationTypes.ReturnToStock,
            "FinishedProduct",
            fp.Id,
            fp.Production.Product.Name,
            1,
            fp.CostPerUnit,
            $"Возврат на склад: {fp.Production.Product.Name}, партия {fp.Production.BatchNumber} (был статус: {GetStatusDisplay(previousStatus)})"
        );

        return MapToResponseDto(fp);
    }

    /// <summary>
    /// Обновление данных о продаже/списании
    /// </summary>
    public async Task<FinishedProductResponseDto?> UpdateAsync(OrganizationContext ctx, int id, FinishedProductUpdateDto dto)
    {
        var fp = await _context.FinishedProducts
            .Where(f => f.OrganizationId == ctx.OrganizationId)
            .Include(f => f.Production)
                .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fp == null) return null;

        if (dto.SalePrice.HasValue) fp.SalePrice = dto.SalePrice.Value;
        if (dto.Client != null) fp.Client = dto.Client;
        if (dto.SaleDate.HasValue) fp.SaleDate = dto.SaleDate.Value;
        if (dto.WriteOffReason != null) fp.WriteOffReason = dto.WriteOffReason;
        if (dto.Comment != null) fp.Comment = dto.Comment;

        fp.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToResponseDto(fp);
    }

    /// <summary>
    /// Получить сводку по готовой продукции
    /// </summary>
    public async Task<FinishedProductSummaryDto> GetSummaryAsync(int organizationId)
    {
        var products = await _context.FinishedProducts
            .Where(fp => fp.OrganizationId == organizationId)
            .ToListAsync();

        var inStock = products.Where(fp => fp.Status == FinishedProductStatus.InStock).ToList();
        var sold = products.Where(fp => fp.Status == FinishedProductStatus.Sold).ToList();
        var writtenOff = products.Where(fp => fp.Status == FinishedProductStatus.WrittenOff).ToList();

        return new FinishedProductSummaryDto
        {
            TotalInStock = inStock.Count,
            TotalSold = sold.Count,
            TotalWrittenOff = writtenOff.Count,
            TotalInStockValue = inStock.Sum(fp => fp.CostPerUnit),
            TotalSalesAmount = sold.Sum(fp => fp.SalePrice ?? 0),
            TotalProfit = sold.Sum(fp => (fp.SalePrice ?? 0) - fp.CostPerUnit)
        };
    }

    // Backward compatibility for services without org context
    public async Task<FinishedProductSummaryDto> GetSummaryAsync()
    {
        var products = await _context.FinishedProducts.ToListAsync();

        var inStock = products.Where(fp => fp.Status == FinishedProductStatus.InStock).ToList();
        var sold = products.Where(fp => fp.Status == FinishedProductStatus.Sold).ToList();
        var writtenOff = products.Where(fp => fp.Status == FinishedProductStatus.WrittenOff).ToList();

        return new FinishedProductSummaryDto
        {
            TotalInStock = inStock.Count,
            TotalSold = sold.Count,
            TotalWrittenOff = writtenOff.Count,
            TotalInStockValue = inStock.Sum(fp => fp.CostPerUnit),
            TotalSalesAmount = sold.Sum(fp => fp.SalePrice ?? 0),
            TotalProfit = sold.Sum(fp => (fp.SalePrice ?? 0) - fp.CostPerUnit)
        };
    }

    private static string GetStatusDisplay(string status)
    {
        return status switch
        {
            FinishedProductStatus.InStock => "На складе",
            FinishedProductStatus.Sold => "Продано",
            FinishedProductStatus.WrittenOff => "Списано",
            _ => status
        };
    }

    private static FinishedProductResponseDto MapToResponseDto(FinishedProduct fp)
    {
        return new FinishedProductResponseDto
        {
            Id = fp.Id,
            ProductionId = fp.ProductionId,
            ProductName = fp.Production.Product.Name,
            ProductCategory = fp.Production.Product.Category,
            BatchNumber = fp.Production.BatchNumber,
            ProductionDate = fp.Production.ProductionDate,
            Status = fp.Status,
            StatusDisplay = GetStatusDisplay(fp.Status),
            CostPerUnit = fp.CostPerUnit,
            RecommendedPrice = fp.RecommendedPrice,
            SalePrice = fp.SalePrice,
            Profit = fp.Status == FinishedProductStatus.Sold ? (fp.SalePrice ?? 0) - fp.CostPerUnit : null,
            Client = fp.Client,
            SaleDate = fp.SaleDate,
            WriteOffReason = fp.WriteOffReason,
            Comment = fp.Comment,
            QrCode = fp.Production.QrCode,
            CreatedAt = fp.CreatedAt,
            UpdatedAt = fp.UpdatedAt
        };
    }
}
