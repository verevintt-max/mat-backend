using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopApi.Models;

/// <summary>
/// Производство изделий (US#4)
/// </summary>
public class Production
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public DateTime ProductionDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Уникальный номер партии
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>
    /// QR-код для идентификации
    /// </summary>
    [MaxLength(500)]
    public string? QrCode { get; set; }

    /// <summary>
    /// Себестоимость на момент производства (за единицу)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPerUnit { get; set; }

    /// <summary>
    /// Общая себестоимость партии
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Рекомендованная цена за единицу (из карточки изделия на момент производства)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? RecommendedPricePerUnit { get; set; }

    /// <summary>
    /// Путь к фото готового изделия
    /// </summary>
    [MaxLength(500)]
    public string? PhotoPath { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public bool IsCancelled { get; set; } = false;

    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<MaterialWriteOff> MaterialWriteOffs { get; set; } = new List<MaterialWriteOff>();
    public virtual ICollection<FinishedProduct> FinishedProducts { get; set; } = new List<FinishedProduct>();
}
