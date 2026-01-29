namespace WorkshopApi.DTOs;

// ============ OPERATION HISTORY DTOs ============

public class OperationHistoryItemDto
{
    public int Id { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string OperationTypeDisplay { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? EntityName { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool CanCancel { get; set; }
    public bool CanRestore { get; set; }
    public string? UserName { get; set; }
}

public class OperationHistoryFilterDto
{
    public string? OperationType { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool? IncludeCancelled { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

public class CancelOperationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Details { get; set; } = new();
}
