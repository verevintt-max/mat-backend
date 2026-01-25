using System.ComponentModel.DataAnnotations;

namespace WorkshopApi.DTOs;

// ============ MATERIAL RECEIPT DTOs ============

public class MaterialReceiptCreateDto
{
    [Required(ErrorMessage = "ID материала обязателен")]
    public int MaterialId { get; set; }

    [Required(ErrorMessage = "Количество обязательно")]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
    public decimal Quantity { get; set; }

    public DateTime? ReceiptDate { get; set; }

    [Required(ErrorMessage = "Цена за единицу обязательна")]
    [Range(0, double.MaxValue, ErrorMessage = "Цена должна быть неотрицательной")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Общая стоимость. Если не указана, рассчитывается автоматически.
    /// </summary>
    public decimal? TotalPrice { get; set; }

    [MaxLength(100)]
    public string? BatchNumber { get; set; }

    [MaxLength(200)]
    public string? PurchaseSource { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    /// <summary>
    /// Данные для создания нового материала (если MaterialId = 0)
    /// </summary>
    public MaterialCreateDto? NewMaterial { get; set; }
}

public class MaterialReceiptUpdateDto
{
    public int? MaterialId { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
    public decimal? Quantity { get; set; }

    public DateTime? ReceiptDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Цена должна быть неотрицательной")]
    public decimal? UnitPrice { get; set; }

    public decimal? TotalPrice { get; set; }

    [MaxLength(100)]
    public string? BatchNumber { get; set; }

    [MaxLength(200)]
    public string? PurchaseSource { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }
}

public class MaterialReceiptResponseDto
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialUnit { get; set; } = string.Empty;
    public string? MaterialColor { get; set; }
    public decimal Quantity { get; set; }
    public DateTime ReceiptDate { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? BatchNumber { get; set; }
    public string? PurchaseSource { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    // Дополнительные данные
    public decimal RemainingQuantity { get; set; }
    public decimal UsedQuantity { get; set; }
    public bool HasUsedMaterials { get; set; }
}

public class MaterialReceiptListItemDto
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialUnit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime ReceiptDate { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? BatchNumber { get; set; }
    public decimal RemainingQuantity { get; set; }
    public bool HasUsedMaterials { get; set; }
}
