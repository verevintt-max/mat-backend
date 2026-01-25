using Microsoft.EntityFrameworkCore;
using WorkshopApi.Data;
using WorkshopApi.DTOs;
using WorkshopApi.Models;
using System.Text.Json;

namespace WorkshopApi.Services;

public class OperationHistoryService
{
    private readonly WorkshopDbContext _context;

    public OperationHistoryService(WorkshopDbContext context)
    {
        _context = context;
    }

    public async Task<int> LogAsync(
        string operationType,
        string? entityType = null,
        int? entityId = null,
        string? entityName = null,
        decimal? quantity = null,
        decimal? amount = null,
        string? description = null,
        object? details = null,
        int? relatedOperationId = null)
    {
        var history = new OperationHistory
        {
            OperationType = operationType,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            Quantity = quantity,
            Amount = amount,
            Description = description,
            Details = details != null ? JsonSerializer.Serialize(details) : null,
            RelatedOperationId = relatedOperationId
        };

        _context.OperationHistory.Add(history);
        await _context.SaveChangesAsync();

        return history.Id;
    }

    public async Task<PagedResultDto<OperationHistoryItemDto>> GetHistoryAsync(OperationHistoryFilterDto filter)
    {
        var query = _context.OperationHistory.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.OperationType))
            query = query.Where(h => h.OperationType.Contains(filter.OperationType));

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(h => h.EntityType == filter.EntityType);

        if (filter.EntityId.HasValue)
            query = query.Where(h => h.EntityId == filter.EntityId);

        if (filter.DateFrom.HasValue)
            query = query.Where(h => h.CreatedAt >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(h => h.CreatedAt <= filter.DateTo.Value);

        if (filter.IncludeCancelled != true)
            query = query.Where(h => !h.IsCancelled);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(h => h.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(h => new OperationHistoryItemDto
            {
                Id = h.Id,
                OperationType = h.OperationType,
                OperationTypeDisplay = GetOperationTypeDisplay(h.OperationType),
                EntityType = h.EntityType,
                EntityId = h.EntityId,
                EntityName = h.EntityName,
                Quantity = h.Quantity,
                Amount = h.Amount,
                Description = h.Description,
                IsCancelled = h.IsCancelled,
                CancelledAt = h.CancelledAt,
                CreatedAt = h.CreatedAt,
                CanCancel = !h.IsCancelled && CanCancelOperation(h.OperationType),
                CanRestore = h.IsCancelled
            })
            .ToListAsync();

        return new PagedResultDto<OperationHistoryItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
            HasPreviousPage = filter.Page > 1,
            HasNextPage = filter.Page * filter.PageSize < totalCount
        };
    }

    public async Task<List<OperationHistoryItemDto>> GetRecentAsync(int count = 10)
    {
        return await _context.OperationHistory
            .Where(h => !h.IsCancelled)
            .OrderByDescending(h => h.CreatedAt)
            .Take(count)
            .Select(h => new OperationHistoryItemDto
            {
                Id = h.Id,
                OperationType = h.OperationType,
                OperationTypeDisplay = GetOperationTypeDisplay(h.OperationType),
                EntityType = h.EntityType,
                EntityId = h.EntityId,
                EntityName = h.EntityName,
                Quantity = h.Quantity,
                Amount = h.Amount,
                Description = h.Description,
                IsCancelled = h.IsCancelled,
                CreatedAt = h.CreatedAt,
                CanCancel = !h.IsCancelled && CanCancelOperation(h.OperationType),
                CanRestore = h.IsCancelled
            })
            .ToListAsync();
    }

    public async Task<bool> MarkAsCancelledAsync(int id)
    {
        var history = await _context.OperationHistory.FindAsync(id);
        if (history == null) return false;

        history.IsCancelled = true;
        history.CancelledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    private static string GetOperationTypeDisplay(string operationType)
    {
        return operationType switch
        {
            OperationTypes.MaterialReceiptCreate => "Поступление материала",
            OperationTypes.MaterialReceiptUpdate => "Изменение поступления",
            OperationTypes.MaterialReceiptDelete => "Удаление поступления",
            OperationTypes.ProductionCreate => "Производство",
            OperationTypes.ProductionCancel => "Отмена производства",
            OperationTypes.Sale => "Продажа",
            OperationTypes.WriteOff => "Списание",
            OperationTypes.ReturnToStock => "Возврат на склад",
            OperationTypes.MaterialCreate => "Создание материала",
            OperationTypes.MaterialUpdate => "Изменение материала",
            OperationTypes.MaterialDelete => "Удаление материала",
            OperationTypes.ProductCreate => "Создание изделия",
            OperationTypes.ProductUpdate => "Изменение изделия",
            OperationTypes.ProductDelete => "Удаление изделия",
            _ => operationType
        };
    }

    private static bool CanCancelOperation(string operationType)
    {
        return operationType switch
        {
            OperationTypes.MaterialReceiptCreate => true,
            OperationTypes.ProductionCreate => true,
            OperationTypes.Sale => true,
            OperationTypes.WriteOff => true,
            _ => false
        };
    }
}
