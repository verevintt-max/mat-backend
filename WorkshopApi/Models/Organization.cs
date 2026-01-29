using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopApi.Models;

/// <summary>
/// Организация/команда
/// </summary>
public class Organization
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    // Владелец организации
    public int OwnerId { get; set; }

    // Является ли организация личной (создается автоматически при регистрации)
    public bool IsPersonal { get; set; } = false;

    // Уникальный код для присоединения
    [MaxLength(20)]
    public string? JoinCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("OwnerId")]
    public virtual User? Owner { get; set; }
    
    public virtual ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

    // Данные организации
    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
    public virtual ICollection<MaterialReceipt> MaterialReceipts { get; set; } = new List<MaterialReceipt>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<Production> Productions { get; set; } = new List<Production>();
    public virtual ICollection<FinishedProduct> FinishedProducts { get; set; } = new List<FinishedProduct>();
    public virtual ICollection<OperationHistory> OperationHistory { get; set; } = new List<OperationHistory>();
}
