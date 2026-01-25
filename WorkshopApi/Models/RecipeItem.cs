using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopApi.Models;

/// <summary>
/// Позиция рецепта (BOM - Bill of Materials)
/// Связь материала с изделием с указанием количества
/// </summary>
public class RecipeItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    public int MaterialId { get; set; }

    /// <summary>
    /// Количество материала на одно изделие
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey(nameof(MaterialId))]
    public virtual Material Material { get; set; } = null!;
}
