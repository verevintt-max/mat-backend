using System.ComponentModel.DataAnnotations;

namespace WorkshopApi.DTOs;

// ==================== ORGANIZATION REQUESTS ====================

/// <summary>
/// Запрос на создание организации
/// </summary>
public class CreateOrganizationRequest
{
    [Required(ErrorMessage = "Название организации обязательно")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// Запрос на обновление организации
/// </summary>
public class UpdateOrganizationRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// Запрос на смену текущей организации
/// </summary>
public class SwitchOrganizationRequest
{
    [Required]
    public int OrganizationId { get; set; }
}

/// <summary>
/// Запрос на присоединение к организации по коду
/// </summary>
public class JoinOrganizationRequest
{
    [Required(ErrorMessage = "Код организации обязателен")]
    [MaxLength(40)]
    public string JoinCode { get; set; } = string.Empty;
}

// ==================== ORGANIZATION RESPONSES ====================

/// <summary>
/// Информация об организации
/// </summary>
public class OrganizationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPersonal { get; set; }
    public string? JoinCode { get; set; }
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int MembersCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Детальная информация об организации
/// </summary>
public class OrganizationDetailDto : OrganizationDto
{
    public List<OrganizationMemberDto> Members { get; set; } = new();
    public List<InvitationDto> PendingInvitations { get; set; } = new();
}

// ==================== MEMBER REQUESTS ====================

/// <summary>
/// Запрос на передачу владения
/// </summary>
public class TransferOwnershipRequest
{
    [Required]
    public int NewOwnerId { get; set; }
}

// ==================== MEMBER RESPONSES ====================

/// <summary>
/// Информация об участнике организации
/// </summary>
public class OrganizationMemberDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

// ==================== INVITATION REQUESTS ====================

/// <summary>
/// Запрос на создание приглашения
/// </summary>
public class CreateInvitationRequest
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    public string Email { get; set; } = string.Empty;
}

// ==================== INVITATION RESPONSES ====================

/// <summary>
/// Информация о приглашении
/// </summary>
public class InvitationDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string InvitedByName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsExpired { get; set; }
    public bool CanBeAccepted { get; set; }
}
