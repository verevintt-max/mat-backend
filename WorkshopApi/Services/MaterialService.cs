using Microsoft.EntityFrameworkCore;
using WorkshopApi.Data;
using WorkshopApi.DTOs;
using WorkshopApi.Models;

namespace WorkshopApi.Services;

public class MaterialService
{
    private readonly WorkshopDbContext _context;
    private readonly OperationHistoryService _historyService;

    public MaterialService(WorkshopDbContext context, OperationHistoryService historyService)
    {
        _context = context;
        _historyService = historyService;
    }

    public async Task<List<MaterialListItemDto>> GetAllAsync(string? search = null, string? category = null, bool includeArchived = false)
    {
        var query = _context.Materials.AsQueryable();

        if (!includeArchived)
            query = query.Where(m => !m.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Name.ToLower().Contains(search.ToLower()) ||
                                     (m.Category != null && m.Category.ToLower().Contains(search.ToLower())));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(m => m.Category == category);

        var materials = await query.OrderBy(m => m.Name).ToListAsync();

        var result = new List<MaterialListItemDto>();
        foreach (var material in materials)
        {
            var balance = await GetMaterialBalanceAsync(material.Id);
            result.Add(new MaterialListItemDto
            {
                Id = material.Id,
                Name = material.Name,
                Unit = material.Unit,
                Color = material.Color,
                Category = material.Category,
                CurrentStock = balance.CurrentStock,
                AveragePrice = balance.AveragePrice,
                IsBelowMinimum = material.MinimumStock.HasValue && balance.CurrentStock < material.MinimumStock.Value,
                IsArchived = material.IsArchived
            });
        }

        return result;
    }

    public async Task<MaterialResponseDto?> GetByIdAsync(int id)
    {
        var material = await _context.Materials
            .Include(m => m.RecipeItems)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (material == null) return null;

        var balance = await GetMaterialBalanceAsync(id);

        return new MaterialResponseDto
        {
            Id = material.Id,
            Name = material.Name,
            Unit = material.Unit,
            Color = material.Color,
            Category = material.Category,
            Description = material.Description,
            MinimumStock = material.MinimumStock,
            IsArchived = material.IsArchived,
            CreatedAt = material.CreatedAt,
            UpdatedAt = material.UpdatedAt,
            CurrentStock = balance.CurrentStock,
            AveragePrice = balance.AveragePrice,
            TotalValue = balance.TotalValue,
            IsBelowMinimum = balance.IsBelowMinimum,
            UsedInProductsCount = material.RecipeItems.Select(r => r.ProductId).Distinct().Count()
        };
    }

    public async Task<MaterialResponseDto> CreateAsync(MaterialCreateDto dto)
    {
        // Проверка на дубликат
        var exists = await _context.Materials.AnyAsync(m =>
            m.Name.ToLower() == dto.Name.ToLower() &&
            (m.Color ?? "") == (dto.Color ?? ""));

        if (exists)
            throw new InvalidOperationException($"Материал с названием '{dto.Name}' и цветом '{dto.Color ?? "без цвета"}' уже существует");

        var material = new Material
        {
            Name = dto.Name,
            Unit = dto.Unit,
            Color = dto.Color,
            Category = dto.Category,
            Description = dto.Description,
            MinimumStock = dto.MinimumStock
        };

        _context.Materials.Add(material);
        await _context.SaveChangesAsync();

        await _historyService.LogAsync(
            OperationTypes.MaterialCreate,
            "Material",
            material.Id,
            material.Name,
            description: $"Создан материал: {material.Name}"
        );

        return (await GetByIdAsync(material.Id))!;
    }

    public async Task<MaterialResponseDto?> UpdateAsync(int id, MaterialUpdateDto dto)
    {
        var material = await _context.Materials.FindAsync(id);
        if (material == null) return null;

        // Проверка на дубликат при изменении имени или цвета
        if (dto.Name != null || dto.Color != null)
        {
            var newName = dto.Name ?? material.Name;
            var newColor = dto.Color ?? material.Color;

            var exists = await _context.Materials.AnyAsync(m =>
                m.Id != id &&
                m.Name.ToLower() == newName.ToLower() &&
                (m.Color ?? "") == (newColor ?? ""));

            if (exists)
                throw new InvalidOperationException($"Материал с названием '{newName}' и цветом '{newColor ?? "без цвета"}' уже существует");
        }

        if (dto.Name != null) material.Name = dto.Name;
        if (dto.Unit != null) material.Unit = dto.Unit;
        if (dto.Color != null) material.Color = dto.Color;
        if (dto.Category != null) material.Category = dto.Category;
        if (dto.Description != null) material.Description = dto.Description;
        if (dto.MinimumStock.HasValue) material.MinimumStock = dto.MinimumStock;
        if (dto.IsArchived.HasValue) material.IsArchived = dto.IsArchived.Value;

        material.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _historyService.LogAsync(
            OperationTypes.MaterialUpdate,
            "Material",
            material.Id,
            material.Name,
            description: $"Обновлен материал: {material.Name}"
        );

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var material = await _context.Materials
            .Include(m => m.Receipts)
            .Include(m => m.RecipeItems)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (material == null) return false;

        // Проверка на использование
        if (material.Receipts.Any() || material.RecipeItems.Any())
            throw new InvalidOperationException("Невозможно удалить материал, который используется в поступлениях или рецептах. Используйте архивирование.");

        await _historyService.LogAsync(
            OperationTypes.MaterialDelete,
            "Material",
            material.Id,
            material.Name,
            description: $"Удален материал: {material.Name}"
        );

        _context.Materials.Remove(material);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        return await _context.Materials
            .Where(m => m.Category != null && m.Category != "")
            .Select(m => m.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<List<MaterialBalanceDto>> GetAllBalancesAsync(bool includeZeroStock = false)
    {
        var materials = await _context.Materials
            .Where(m => !m.IsArchived)
            .OrderBy(m => m.Name)
            .ToListAsync();

        var result = new List<MaterialBalanceDto>();
        foreach (var material in materials)
        {
            var balance = await GetMaterialBalanceAsync(material.Id);
            if (includeZeroStock || balance.CurrentStock > 0)
            {
                result.Add(balance);
            }
        }

        return result;
    }

    public async Task<MaterialBalanceDto> GetMaterialBalanceAsync(int materialId)
    {
        var material = await _context.Materials.FindAsync(materialId);
        if (material == null)
            throw new InvalidOperationException($"Материал с ID {materialId} не найден");

        // Получаем все поступления
        var receipts = await _context.MaterialReceipts
            .Where(r => r.MaterialId == materialId)
            .Include(r => r.WriteOffs)
            .ToListAsync();

        decimal totalReceived = receipts.Sum(r => r.Quantity);
        decimal totalWrittenOff = receipts.SelectMany(r => r.WriteOffs).Sum(w => w.Quantity);
        decimal currentStock = totalReceived - totalWrittenOff;

        // Расчет средней цены (по остаткам на складе методом FIFO)
        decimal averagePrice = 0;
        decimal totalValue = 0;

        foreach (var receipt in receipts.OrderBy(r => r.ReceiptDate))
        {
            decimal usedFromReceipt = receipt.WriteOffs.Sum(w => w.Quantity);
            decimal remainingInReceipt = receipt.Quantity - usedFromReceipt;

            if (remainingInReceipt > 0)
            {
                totalValue += remainingInReceipt * receipt.UnitPrice;
            }
        }

        if (currentStock > 0)
            averagePrice = totalValue / currentStock;

        return new MaterialBalanceDto
        {
            MaterialId = materialId,
            MaterialName = material.Name,
            Unit = material.Unit,
            Color = material.Color,
            Category = material.Category,
            CurrentStock = currentStock,
            AveragePrice = Math.Round(averagePrice, 2),
            TotalValue = Math.Round(totalValue, 2),
            MinimumStock = material.MinimumStock,
            IsBelowMinimum = material.MinimumStock.HasValue && currentStock < material.MinimumStock.Value
        };
    }

    /// <summary>
    /// Получить список изделий, в которых используется материал
    /// </summary>
    public async Task<List<ProductListItemDto>> GetProductsUsingMaterialAsync(int materialId)
    {
        var products = await _context.RecipeItems
            .Where(r => r.MaterialId == materialId)
            .Include(r => r.Product)
            .Select(r => r.Product)
            .Distinct()
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Category = p.Category,
                ProductionTimeMinutes = p.ProductionTimeMinutes,
                EstimatedCost = p.EstimatedCost,
                RecommendedPrice = p.RecommendedPrice,
                IsArchived = p.IsArchived,
                MaterialsCount = p.RecipeItems.Count,
                ProducedCount = p.Productions.Sum(pr => pr.Quantity),
                InStockCount = p.Productions
                    .SelectMany(pr => pr.FinishedProducts)
                    .Count(fp => fp.Status == FinishedProductStatus.InStock)
            })
            .ToListAsync();

        return products;
    }
}
