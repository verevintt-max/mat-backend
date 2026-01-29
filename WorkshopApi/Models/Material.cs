using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopApi.Models;

/// <summary>
/// Справочник материалов (US#1)
/// </summary>
public class Material
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID организации, которой принадлежит материал
    /// </summary>
    [Required]
    public int OrganizationId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty; // шт, кг, м, л и т.д.

    [MaxLength(100)]
    public string? Color { get; set; } // Цвет (может быть несколько через запятую)

    [MaxLength(100)]
    public string? Category { get; set; } // Категория

    [MaxLength(500)]
    public string? Description { get; set; }

    public decimal? MinimumStock { get; set; } // Минимальный остаток для напоминаний

    public bool IsArchived { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }
    
    public virtual ICollection<MaterialReceipt> Receipts { get; set; } = new List<MaterialReceipt>();
    public virtual ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
}
