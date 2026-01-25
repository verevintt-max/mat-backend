using System.ComponentModel.DataAnnotations;

namespace WorkshopApi.DTOs;

// ============ PRODUCTION DTOs ============

public class ProductionCreateDto
{
    [Required(ErrorMessage = "ID изделия обязателен")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Количество обязательно")]
    [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не менее 1")]
    public int Quantity { get; set; }

    public DateTime? ProductionDate { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    [MaxLength(500)]
    public string? PhotoPath { get; set; }
}

public class ProductionUpdateDto
{
    [Range(1, int.MaxValue)]
    public int? Quantity { get; set; }

    public DateTime? ProductionDate { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    [MaxLength(500)]
    public string? PhotoPath { get; set; }
}

public class ProductionResponseDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCategory { get; set; }
    public int Quantity { get; set; }
    public DateTime ProductionDate { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string? QrCode { get; set; }
    public decimal CostPerUnit { get; set; }
    public decimal TotalCost { get; set; }
    public decimal? RecommendedPricePerUnit { get; set; }
    public string? PhotoPath { get; set; }
    public string? Comment { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<MaterialWriteOffDto> MaterialWriteOffs { get; set; } = new();
    public int InStockCount { get; set; }
    public int SoldCount { get; set; }
    public int WrittenOffCount { get; set; }
}

public class MaterialWriteOffDto
{
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialUnit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class ProductionListItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ProductionDate { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public decimal CostPerUnit { get; set; }
    public decimal TotalCost { get; set; }
    public decimal? RecommendedPricePerUnit { get; set; }
    public bool IsCancelled { get; set; }
    public int InStockCount { get; set; }
    public int SoldCount { get; set; }
}

public class MaterialAvailabilityDto
{
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialUnit { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal Shortage { get; set; }
    public bool IsAvailable { get; set; }
}

public class ProductionCheckResultDto
{
    public bool CanProduce { get; set; }
    public decimal EstimatedCostPerUnit { get; set; }
    public decimal EstimatedTotalCost { get; set; }
    public List<MaterialAvailabilityDto> Materials { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
