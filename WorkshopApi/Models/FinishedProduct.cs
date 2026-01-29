using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopApi.Models;

/// <summary>
/// Готовая продукция на складе (US#5)
/// </summary>
public class FinishedProduct
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID организации
    /// </summary>
    [Required]
    public int OrganizationId { get; set; }

    [Required]
    public int ProductionId { get; set; }

    /// <summary>
    /// Статус: InStock, Sold, WrittenOff
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = FinishedProductStatus.InStock;

    /// <summary>
    /// Себестоимость единицы
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPerUnit { get; set; }

    /// <summary>
    /// Рекомендованная цена (из карточки изделия на момент производства)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? RecommendedPrice { get; set; }

    /// <summary>
    /// Цена продажи (если продано)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? SalePrice { get; set; }

    /// <summary>
    /// Клиент (если продано)
    /// </summary>
    [MaxLength(200)]
    public string? Client { get; set; }

    /// <summary>
    /// Дата продажи/списания
    /// </summary>
    public DateTime? SaleDate { get; set; }

    /// <summary>
    /// Причина списания (если списано как брак)
    /// </summary>
    [MaxLength(500)]
    public string? WriteOffReason { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }

    [ForeignKey(nameof(ProductionId))]
    public virtual Production Production { get; set; } = null!;
}

/// <summary>
/// Статусы готовой продукции
/// </summary>
public static class FinishedProductStatus
{
    public const string InStock = "InStock";
    public const string Sold = "Sold";
    public const string WrittenOff = "WrittenOff";
}
