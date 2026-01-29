using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopApi.Models;

/// <summary>
/// Refresh токен для обновления JWT
/// </summary>
public class RefreshToken
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAt { get; set; }

    [MaxLength(100)]
    public string? ReplacedByToken { get; set; }

    [MaxLength(100)]
    public string? CreatedByIp { get; set; }

    [MaxLength(100)]
    public string? RevokedByIp { get; set; }

    // Навигационные свойства
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    // Проверки
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    [NotMapped]
    public bool IsRevoked => RevokedAt != null;

    [NotMapped]
    public bool IsActive => !IsRevoked && !IsExpired;
}
