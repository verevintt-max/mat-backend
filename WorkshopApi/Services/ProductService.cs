using Microsoft.EntityFrameworkCore;
using WorkshopApi.Data;
using WorkshopApi.DTOs;
using WorkshopApi.Models;

namespace WorkshopApi.Services;

public class ProductService
{
    private readonly WorkshopDbContext _context;
    private readonly OperationHistoryService _historyService;
    private readonly MaterialService _materialService;

    public ProductService(
        WorkshopDbContext context,
        OperationHistoryService historyService,
        MaterialService materialService)
    {
        _context = context;
        _historyService = historyService;
        _materialService = materialService;
    }

    public async Task<List<ProductListItemDto>> GetAllAsync(string? search = null, string? category = null, bool includeArchived = false)
    {
        var query = _context.Products
            .Include(p => p.RecipeItems)
            .Include(p => p.Productions)
                .ThenInclude(pr => pr.FinishedProducts)
            .AsQueryable();

        if (!includeArchived)
            query = query.Where(p => !p.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()) ||
                                     (p.Category != null && p.Category.ToLower().Contains(search.ToLower())));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        return await query
            .OrderBy(p => p.Name)
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
                ProducedCount = p.Productions.Where(pr => !pr.IsCancelled).Sum(pr => pr.Quantity),
                InStockCount = p.Productions
                    .Where(pr => !pr.IsCancelled)
                    .SelectMany(pr => pr.FinishedProducts)
                    .Count(fp => fp.Status == FinishedProductStatus.InStock)
            })
            .ToListAsync();
    }

    public async Task<ProductResponseDto?> GetByIdAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.RecipeItems)
                .ThenInclude(r => r.Material)
            .Include(p => p.Productions)
                .ThenInclude(pr => pr.FinishedProducts)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        var recipeItems = new List<RecipeItemResponseDto>();
        foreach (var item in product.RecipeItems)
        {
            var balance = await _materialService.GetMaterialBalanceAsync(item.MaterialId);
            recipeItems.Add(new RecipeItemResponseDto
            {
                Id = item.Id,
                MaterialId = item.MaterialId,
                MaterialName = item.Material.Name,
                MaterialUnit = item.Material.Unit,
                MaterialColor = item.Material.Color,
                Quantity = item.Quantity,
                MaterialAveragePrice = balance.AveragePrice,
                ItemCost = Math.Round(item.Quantity * balance.AveragePrice, 2)
            });
        }

        var activeProductions = product.Productions.Where(pr => !pr.IsCancelled).ToList();

        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Category = product.Category,
            Description = product.Description,
            ProductionTimeMinutes = product.ProductionTimeMinutes,
            ProductionTimeFormatted = FormatProductionTime(product.ProductionTimeMinutes),
            FileLinks = product.FileLinks,
            EstimatedCost = product.EstimatedCost,
            MarkupPercent = product.MarkupPercent,
            RecommendedPrice = product.RecommendedPrice,
            IsArchived = product.IsArchived,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            RecipeItems = recipeItems,
            ProducedCount = activeProductions.Sum(pr => pr.Quantity),
            InStockCount = activeProductions
                .SelectMany(pr => pr.FinishedProducts)
                .Count(fp => fp.Status == FinishedProductStatus.InStock)
        };
    }

    public async Task<ProductResponseDto> CreateAsync(ProductCreateDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Category = dto.Category,
            Description = dto.Description,
            ProductionTimeMinutes = dto.ProductionTimeMinutes,
            FileLinks = dto.FileLinks,
            MarkupPercent = dto.MarkupPercent
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Добавляем материалы в рецепт
        foreach (var item in dto.RecipeItems)
        {
            var recipeItem = new RecipeItem
            {
                ProductId = product.Id,
                MaterialId = item.MaterialId,
                Quantity = item.Quantity
            };
            _context.RecipeItems.Add(recipeItem);
        }

        await _context.SaveChangesAsync();

        // Пересчитываем себестоимость
        await RecalculateCostAsync(product.Id);

        await _historyService.LogAsync(
            OperationTypes.ProductCreate,
            "Product",
            product.Id,
            product.Name,
            description: $"Создано изделие: {product.Name}"
        );

        return (await GetByIdAsync(product.Id))!;
    }

    public async Task<ProductResponseDto?> UpdateAsync(int id, ProductUpdateDto dto)
    {
        var product = await _context.Products
            .Include(p => p.RecipeItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        if (dto.Name != null) product.Name = dto.Name;
        if (dto.Category != null) product.Category = dto.Category;
        if (dto.Description != null) product.Description = dto.Description;
        if (dto.ProductionTimeMinutes.HasValue) product.ProductionTimeMinutes = dto.ProductionTimeMinutes.Value;
        if (dto.FileLinks != null) product.FileLinks = dto.FileLinks;
        if (dto.MarkupPercent.HasValue) product.MarkupPercent = dto.MarkupPercent.Value;
        if (dto.IsArchived.HasValue) product.IsArchived = dto.IsArchived.Value;

        // Обновляем рецепт, если указан
        if (dto.RecipeItems != null)
        {
            // Удаляем старые
            _context.RecipeItems.RemoveRange(product.RecipeItems);

            // Добавляем новые
            foreach (var item in dto.RecipeItems)
            {
                var recipeItem = new RecipeItem
                {
                    ProductId = product.Id,
                    MaterialId = item.MaterialId,
                    Quantity = item.Quantity
                };
                _context.RecipeItems.Add(recipeItem);
            }
        }

        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Пересчитываем себестоимость
        await RecalculateCostAsync(product.Id);

        await _historyService.LogAsync(
            OperationTypes.ProductUpdate,
            "Product",
            product.Id,
            product.Name,
            description: $"Обновлено изделие: {product.Name}"
        );

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.Productions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return false;

        if (product.Productions.Any())
            throw new InvalidOperationException(
                "Невозможно удалить изделие с существующими записями о производстве. Используйте архивирование.");

        await _historyService.LogAsync(
            OperationTypes.ProductDelete,
            "Product",
            product.Id,
            product.Name,
            description: $"Удалено изделие: {product.Name}"
        );

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<ProductResponseDto?> CopyAsync(int id, ProductCopyDto dto)
    {
        var original = await _context.Products
            .Include(p => p.RecipeItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (original == null) return null;

        var copy = new Product
        {
            Name = dto.NewName,
            Category = original.Category,
            Description = original.Description,
            ProductionTimeMinutes = original.ProductionTimeMinutes,
            FileLinks = original.FileLinks,
            MarkupPercent = original.MarkupPercent
        };

        _context.Products.Add(copy);
        await _context.SaveChangesAsync();

        // Копируем рецепт
        foreach (var item in original.RecipeItems)
        {
            var recipeItem = new RecipeItem
            {
                ProductId = copy.Id,
                MaterialId = item.MaterialId,
                Quantity = item.Quantity
            };
            _context.RecipeItems.Add(recipeItem);
        }

        await _context.SaveChangesAsync();

        // Пересчитываем себестоимость
        await RecalculateCostAsync(copy.Id);

        await _historyService.LogAsync(
            OperationTypes.ProductCreate,
            "Product",
            copy.Id,
            copy.Name,
            description: $"Создана копия изделия: {copy.Name} (из {original.Name})"
        );

        return await GetByIdAsync(copy.Id);
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        return await _context.Products
            .Where(p => p.Category != null && p.Category != "")
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task RecalculateCostAsync(int productId)
    {
        var product = await _context.Products
            .Include(p => p.RecipeItems)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null) return;

        decimal estimatedCost = 0;

        foreach (var item in product.RecipeItems)
        {
            var balance = await _materialService.GetMaterialBalanceAsync(item.MaterialId);
            estimatedCost += item.Quantity * balance.AveragePrice;
        }

        product.EstimatedCost = Math.Round(estimatedCost, 2);
        product.RecommendedPrice = Math.Round(estimatedCost * (1 + product.MarkupPercent / 100), 2);
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private static string FormatProductionTime(int minutes)
    {
        if (minutes < 60)
            return $"{minutes} мин";

        var hours = minutes / 60;
        var mins = minutes % 60;

        if (mins == 0)
            return $"{hours} ч";

        return $"{hours} ч {mins} мин";
    }
}
