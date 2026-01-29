using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopApi.Models;

/// <summary>
/// История операций (US#7)
/// </summary>
public class OperationHistory
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID организации
    /// </summary>
    [Required]
    public int OrganizationId { get; set; }

    /// <summary>
    /// ID пользователя, выполнившего операцию
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Тип операции: MaterialReceipt, Production, Sale, WriteOff, Cancel, Restore
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Тип сущности: Material, Product, Production, FinishedProduct
    /// </summary>
    [MaxLength(50)]
    public string? EntityType { get; set; }

    /// <summary>
    /// ID сущности
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Название сущности для отображения
    /// </summary>
    [MaxLength(200)]
    public string? EntityName { get; set; }

    /// <summary>
    /// Количество (если применимо)
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Сумма (если применимо)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Описание операции
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Детали операции в JSON формате (для возможности отмены)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// ID связанной операции (для отмены/восстановления)
    /// </summary>
    public int? RelatedOperationId { get; set; }

    public bool IsCancelled { get; set; } = false;

    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

/// <summary>
/// Типы операций
/// </summary>
public static class OperationTypes
{
    public const string MaterialReceiptCreate = "MaterialReceiptCreate";
    public const string MaterialReceiptUpdate = "MaterialReceiptUpdate";
    public const string MaterialReceiptDelete = "MaterialReceiptDelete";
    public const string ProductionCreate = "ProductionCreate";
    public const string ProductionCancel = "ProductionCancel";
    public const string Sale = "Sale";
    public const string WriteOff = "WriteOff";
    public const string ReturnToStock = "ReturnToStock";
    public const string MaterialCreate = "MaterialCreate";
    public const string MaterialUpdate = "MaterialUpdate";
    public const string MaterialDelete = "MaterialDelete";
    public const string ProductCreate = "ProductCreate";
    public const string ProductUpdate = "ProductUpdate";
    public const string ProductDelete = "ProductDelete";
}
