using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopApi.Models;

/// <summary>
/// Карточка изделия/рецепт (US#3)
/// </summary>
public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Время на изготовление в минутах
    /// </summary>
    public int ProductionTimeMinutes { get; set; }

    /// <summary>
    /// Ссылки на файлы (3D-модели, инструкции, фото) - JSON массив
    /// </summary>
    [MaxLength(2000)]
    public string? FileLinks { get; set; }

    /// <summary>
    /// Примерная себестоимость (авторасчет по средним ценам материалов)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? EstimatedCost { get; set; }

    /// <summary>
    /// Наценка в процентах для рекомендованной цены
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal MarkupPercent { get; set; } = 30;

    /// <summary>
    /// Рекомендованная цена продажи
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? RecommendedPrice { get; set; }

    public bool IsArchived { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    public virtual ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
    public virtual ICollection<Production> Productions { get; set; } = new List<Production>();
}
