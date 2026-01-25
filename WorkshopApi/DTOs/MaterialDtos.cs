using System.ComponentModel.DataAnnotations;

namespace WorkshopApi.DTOs;

// ============ MATERIAL DTOs ============

public class MaterialCreateDto
{
    [Required(ErrorMessage = "Название обязательно")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Единица измерения обязательна")]
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Color { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public decimal? MinimumStock { get; set; }
}

public class MaterialUpdateDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    [MaxLength(100)]
    public string? Color { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public decimal? MinimumStock { get; set; }

    public bool? IsArchived { get; set; }
}

public class MaterialResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public decimal? MinimumStock { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Расчетные поля
    public decimal CurrentStock { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal TotalValue { get; set; }
    public bool IsBelowMinimum { get; set; }
    public int UsedInProductsCount { get; set; }
}

public class MaterialListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Category { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal AveragePrice { get; set; }
    public bool IsBelowMinimum { get; set; }
    public bool IsArchived { get; set; }
}

public class MaterialBalanceDto
{
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Category { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal TotalValue { get; set; }
    public decimal? MinimumStock { get; set; }
    public bool IsBelowMinimum { get; set; }
}
