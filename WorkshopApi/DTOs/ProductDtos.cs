using System.ComponentModel.DataAnnotations;

namespace WorkshopApi.DTOs;

// ============ PRODUCT (Recipe) DTOs ============

public class ProductCreateDto
{
    [Required(ErrorMessage = "Название обязательно")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Время должно быть неотрицательным")]
    public int ProductionTimeMinutes { get; set; }

    /// <summary>
    /// Вес изделия в килограммах
    /// </summary>
    [Range(0, 10000, ErrorMessage = "Вес должен быть от 0 до 10000 кг")]
    public decimal Weight { get; set; } = 0;

    [MaxLength(2000)]
    public string? FileLinks { get; set; }

    [Range(0, 1000, ErrorMessage = "Наценка должна быть от 0 до 1000%")]
    public decimal MarkupPercent { get; set; } = 100;

    public List<RecipeItemCreateDto> RecipeItems { get; set; } = new();
}

public class ProductUpdateDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0, int.MaxValue)]
    public int? ProductionTimeMinutes { get; set; }

    /// <summary>
    /// Вес изделия в килограммах
    /// </summary>
    [Range(0, 10000)]
    public decimal? Weight { get; set; }

    [MaxLength(2000)]
    public string? FileLinks { get; set; }

    [Range(0, 1000)]
    public decimal? MarkupPercent { get; set; }

    public bool? IsArchived { get; set; }

    public List<RecipeItemCreateDto>? RecipeItems { get; set; }
}

public class RecipeItemCreateDto
{
    [Required]
    public int MaterialId { get; set; }

    [Required]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
    public decimal Quantity { get; set; }
}

public class RecipeItemResponseDto
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialUnit { get; set; } = string.Empty;
    public string? MaterialColor { get; set; }
    public decimal Quantity { get; set; }
    public decimal MaterialAveragePrice { get; set; }
    public decimal ItemCost { get; set; }
}

public class ProductResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public int ProductionTimeMinutes { get; set; }
    public string ProductionTimeFormatted { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public string? FileLinks { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal MarkupPercent { get; set; }
    public decimal? RecommendedPrice { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<RecipeItemResponseDto> RecipeItems { get; set; } = new();
    public int ProducedCount { get; set; }
    public int InStockCount { get; set; }
}

public class ProductListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int ProductionTimeMinutes { get; set; }
    public decimal Weight { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? RecommendedPrice { get; set; }
    public bool IsArchived { get; set; }
    public int MaterialsCount { get; set; }
    public int ProducedCount { get; set; }
    public int InStockCount { get; set; }
}

public class ProductCopyDto
{
    [Required(ErrorMessage = "Название обязательно")]
    [MaxLength(200)]
    public string NewName { get; set; } = string.Empty;
}
