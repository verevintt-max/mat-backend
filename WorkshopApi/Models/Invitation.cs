using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopApi.Models;

/// <summary>
/// Статус приглашения
/// </summary>
public enum InvitationStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Expired = 4,
    Cancelled = 5
}

/// <summary>
/// Приглашение в организацию
/// </summary>
public class Invitation
{
    [Key]
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    // Email приглашенного пользователя
    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Уникальный токен приглашения
    [Required]
    [MaxLength(100)]
    public string Token { get; set; } = string.Empty;

    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    // Кто отправил приглашение
    public int InvitedById { get; set; }

    // Когда истекает приглашение
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? AcceptedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    // Навигационные свойства
    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }

    [ForeignKey("InvitedById")]
    public virtual User? InvitedBy { get; set; }

    // Проверка истечения срока
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    [NotMapped]
    public bool CanBeAccepted => Status == InvitationStatus.Pending && !IsExpired;
}
