using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopApi.Models;

/// <summary>
/// Роль пользователя в организации
/// </summary>
public enum OrganizationRole
{
    /// <summary>
    /// Владелец - полный доступ
    /// </summary>
    Owner = 1,

    /// <summary>
    /// Участник - базовый доступ
    /// </summary>
    Member = 2
}

/// <summary>
/// Членство пользователя в организации
/// </summary>
public class OrganizationMember
{
    [Key]
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public int UserId { get; set; }

    public OrganizationRole Role { get; set; } = OrganizationRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
