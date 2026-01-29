using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkshopApi.Models;

/// <summary>
/// Пользователь системы
/// </summary>
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EmailConfirmed { get; set; } = false;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Текущая активная организация пользователя
    public int? CurrentOrganizationId { get; set; }

    // Навигационные свойства
    public virtual Organization? CurrentOrganization { get; set; }
    public virtual ICollection<OrganizationMember> OrganizationMemberships { get; set; } = new List<OrganizationMember>();
    public virtual ICollection<Invitation> SentInvitations { get; set; } = new List<Invitation>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    // Полное имя
    [NotMapped]
    public string FullName => string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName) 
        ? Username 
        : $"{FirstName} {LastName}".Trim();
}
