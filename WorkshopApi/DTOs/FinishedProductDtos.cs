using System.ComponentModel.DataAnnotations;

namespace WorkshopApi.DTOs;

// ============ FINISHED PRODUCT DTOs ============

public class SellProductDto
{
    [Required(ErrorMessage = "Цена продажи обязательна")]
    [Range(0, double.MaxValue, ErrorMessage = "Цена должна быть неотрицательной")]
    public decimal SalePrice { get; set; }

    [MaxLength(200)]
    public string? Client { get; set; }

    public DateTime? SaleDate { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }
}

public class WriteOffProductDto
{
    [Required(ErrorMessage = "Причина списания обязательна")]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Comment { get; set; }
}

public class FinishedProductUpdateDto
{
    public decimal? SalePrice { get; set; }

    [MaxLength(200)]
    public string? Client { get; set; }

    public DateTime? SaleDate { get; set; }

    [MaxLength(500)]
    public string? WriteOffReason { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }
}

public class FinishedProductResponseDto
{
    public int Id { get; set; }
    public int ProductionId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCategory { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ProductionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public decimal CostPerUnit { get; set; }
    public decimal? RecommendedPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? Profit { get; set; }
    public string? Client { get; set; }
    public DateTime? SaleDate { get; set; }
    public string? WriteOffReason { get; set; }
    public string? Comment { get; set; }
    public string? QrCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FinishedProductListItemDto
{
    public int Id { get; set; }
    public int ProductionId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCategory { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ProductionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public decimal CostPerUnit { get; set; }
    public decimal? RecommendedPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public string? Client { get; set; }
    public DateTime? SaleDate { get; set; }
}

public class FinishedProductSummaryDto
{
    public int TotalInStock { get; set; }
    public int TotalSold { get; set; }
    public int TotalWrittenOff { get; set; }
    public decimal TotalInStockValue { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal TotalProfit { get; set; }
}
