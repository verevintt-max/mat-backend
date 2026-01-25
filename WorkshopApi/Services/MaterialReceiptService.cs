using Microsoft.EntityFrameworkCore;
using WorkshopApi.Data;
using WorkshopApi.DTOs;
using WorkshopApi.Models;

namespace WorkshopApi.Services;

public class MaterialReceiptService
{
    private readonly WorkshopDbContext _context;
    private readonly OperationHistoryService _historyService;
    private readonly MaterialService _materialService;

    public MaterialReceiptService(
        WorkshopDbContext context,
        OperationHistoryService historyService,
        MaterialService materialService)
    {
        _context = context;
        _historyService = historyService;
        _materialService = materialService;
    }

    public async Task<List<MaterialReceiptListItemDto>> GetAllAsync(int? materialId = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var query = _context.MaterialReceipts
            .Include(r => r.Material)
            .Include(r => r.WriteOffs)
            .AsQueryable();

        if (materialId.HasValue)
            query = query.Where(r => r.MaterialId == materialId.Value);

        if (dateFrom.HasValue)
            query = query.Where(r => r.ReceiptDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(r => r.ReceiptDate <= dateTo.Value);

        return await query
            .OrderByDescending(r => r.ReceiptDate)
            .ThenByDescending(r => r.Id)
            .Select(r => new MaterialReceiptListItemDto
            {
                Id = r.Id,
                MaterialId = r.MaterialId,
                MaterialName = r.Material.Name,
                MaterialUnit = r.Material.Unit,
                Quantity = r.Quantity,
                ReceiptDate = r.ReceiptDate,
                UnitPrice = r.UnitPrice,
                TotalPrice = r.TotalPrice,
                BatchNumber = r.BatchNumber,
                RemainingQuantity = r.Quantity - r.WriteOffs.Sum(w => w.Quantity),
                HasUsedMaterials = r.WriteOffs.Any()
            })
            .ToListAsync();
    }

    public async Task<MaterialReceiptResponseDto?> GetByIdAsync(int id)
    {
        var receipt = await _context.MaterialReceipts
            .Include(r => r.Material)
            .Include(r => r.WriteOffs)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receipt == null) return null;

        var usedQuantity = receipt.WriteOffs.Sum(w => w.Quantity);

        return new MaterialReceiptResponseDto
        {
            Id = receipt.Id,
            MaterialId = receipt.MaterialId,
            MaterialName = receipt.Material.Name,
            MaterialUnit = receipt.Material.Unit,
            MaterialColor = receipt.Material.Color,
            Quantity = receipt.Quantity,
            ReceiptDate = receipt.ReceiptDate,
            UnitPrice = receipt.UnitPrice,
            TotalPrice = receipt.TotalPrice,
            BatchNumber = receipt.BatchNumber,
            PurchaseSource = receipt.PurchaseSource,
            Comment = receipt.Comment,
            CreatedAt = receipt.CreatedAt,
            RemainingQuantity = receipt.Quantity - usedQuantity,
            UsedQuantity = usedQuantity,
            HasUsedMaterials = usedQuantity > 0
        };
    }

    public async Task<MaterialReceiptResponseDto> CreateAsync(MaterialReceiptCreateDto dto)
    {
        int materialId = dto.MaterialId;

        // Если MaterialId = 0, создаем новый материал
        if (materialId == 0 && dto.NewMaterial != null)
        {
            var newMaterial = await _materialService.CreateAsync(dto.NewMaterial);
            materialId = newMaterial.Id;
        }

        // Проверка существования материала
        var material = await _context.Materials.FindAsync(materialId);
        if (material == null)
            throw new InvalidOperationException($"Материал с ID {materialId} не найден");

        var receipt = new MaterialReceipt
        {
            MaterialId = materialId,
            Quantity = dto.Quantity,
            ReceiptDate = dto.ReceiptDate ?? DateTime.UtcNow,
            UnitPrice = dto.UnitPrice,
            TotalPrice = dto.TotalPrice ?? dto.Quantity * dto.UnitPrice,
            BatchNumber = dto.BatchNumber,
            PurchaseSource = dto.PurchaseSource,
            Comment = dto.Comment
        };

        _context.MaterialReceipts.Add(receipt);
        await _context.SaveChangesAsync();

        await _historyService.LogAsync(
            OperationTypes.MaterialReceiptCreate,
            "MaterialReceipt",
            receipt.Id,
            material.Name,
            receipt.Quantity,
            receipt.TotalPrice,
            $"Поступление: {material.Name}, {receipt.Quantity} {material.Unit} по {receipt.UnitPrice} руб."
        );

        return (await GetByIdAsync(receipt.Id))!;
    }

    public async Task<MaterialReceiptResponseDto?> UpdateAsync(int id, MaterialReceiptUpdateDto dto)
    {
        var receipt = await _context.MaterialReceipts
            .Include(r => r.WriteOffs)
            .Include(r => r.Material)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receipt == null) return null;

        var usedQuantity = receipt.WriteOffs.Sum(w => w.Quantity);

        // Проверка на изменение количества
        if (dto.Quantity.HasValue && dto.Quantity.Value < usedQuantity)
            throw new InvalidOperationException(
                $"Нельзя уменьшить количество ниже использованного ({usedQuantity} {receipt.Material.Unit})");

        if (dto.MaterialId.HasValue)
        {
            var material = await _context.Materials.FindAsync(dto.MaterialId.Value);
            if (material == null)
                throw new InvalidOperationException($"Материал с ID {dto.MaterialId.Value} не найден");

            if (receipt.WriteOffs.Any())
                throw new InvalidOperationException("Нельзя изменить материал в поступлении, если материалы уже использованы");

            receipt.MaterialId = dto.MaterialId.Value;
        }

        if (dto.Quantity.HasValue) receipt.Quantity = dto.Quantity.Value;
        if (dto.ReceiptDate.HasValue) receipt.ReceiptDate = dto.ReceiptDate.Value;
        if (dto.UnitPrice.HasValue) receipt.UnitPrice = dto.UnitPrice.Value;
        if (dto.BatchNumber != null) receipt.BatchNumber = dto.BatchNumber;
        if (dto.PurchaseSource != null) receipt.PurchaseSource = dto.PurchaseSource;
        if (dto.Comment != null) receipt.Comment = dto.Comment;

        // Пересчет общей стоимости
        if (dto.TotalPrice.HasValue)
            receipt.TotalPrice = dto.TotalPrice.Value;
        else
            receipt.TotalPrice = receipt.Quantity * receipt.UnitPrice;

        receipt.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _historyService.LogAsync(
            OperationTypes.MaterialReceiptUpdate,
            "MaterialReceipt",
            receipt.Id,
            receipt.Material.Name,
            receipt.Quantity,
            receipt.TotalPrice,
            $"Изменено поступление: {receipt.Material.Name}"
        );

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, bool force = false)
    {
        var receipt = await _context.MaterialReceipts
            .Include(r => r.WriteOffs)
            .Include(r => r.Material)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receipt == null) return false;

        if (receipt.WriteOffs.Any() && !force)
            throw new InvalidOperationException(
                "Материалы из этого поступления уже использованы в производстве. Удаление невозможно.");

        var materialName = receipt.Material.Name;
        var quantity = receipt.Quantity;
        var totalPrice = receipt.TotalPrice;

        // Удаляем связанные списания (если force)
        if (force && receipt.WriteOffs.Any())
        {
            _context.MaterialWriteOffs.RemoveRange(receipt.WriteOffs);
        }

        _context.MaterialReceipts.Remove(receipt);
        await _context.SaveChangesAsync();

        await _historyService.LogAsync(
            OperationTypes.MaterialReceiptDelete,
            "MaterialReceipt",
            id,
            materialName,
            quantity,
            totalPrice,
            $"Удалено поступление: {materialName}, {quantity}"
        );

        return true;
    }
}
