using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopApi.Models;

/// <summary>
/// Поступление материалов на склад (US#2)
/// </summary>
public class MaterialReceipt
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID организации
    /// </summary>
    [Required]
    public int OrganizationId { get; set; }

    [Required]
    public int MaterialId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Required]
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; } // Авторасчет: Quantity * UnitPrice

    [MaxLength(100)]
    public string? BatchNumber { get; set; } // Номер партии

    [MaxLength(200)]
    public string? PurchaseSource { get; set; } // Место покупки

    [MaxLength(500)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }

    [ForeignKey(nameof(MaterialId))]
    public virtual Material Material { get; set; } = null!;

    public virtual ICollection<MaterialWriteOff> WriteOffs { get; set; } = new List<MaterialWriteOff>();
}
