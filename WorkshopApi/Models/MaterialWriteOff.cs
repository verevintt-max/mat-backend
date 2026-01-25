using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopApi.Models;

/// <summary>
/// Списание материалов при производстве (FIFO)
/// </summary>
public class MaterialWriteOff
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductionId { get; set; }

    [Required]
    public int MaterialReceiptId { get; set; }

    [Required]
    public int MaterialId { get; set; }

    /// <summary>
    /// Списанное количество из конкретного поступления
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Цена за единицу на момент списания
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey(nameof(ProductionId))]
    public virtual Production Production { get; set; } = null!;

    [ForeignKey(nameof(MaterialReceiptId))]
    public virtual MaterialReceipt MaterialReceipt { get; set; } = null!;

    [ForeignKey(nameof(MaterialId))]
    public virtual Material Material { get; set; } = null!;
}
