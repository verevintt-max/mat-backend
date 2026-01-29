using System.ComponentModel.DataAnnotations;

namespace WorkshopApi.DTOs;

// ==================== AUTH REQUESTS ====================

/// <summary>
/// Запрос на регистрацию
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [MinLength(3, ErrorMessage = "Имя пользователя должно быть не менее 3 символов")]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(6, ErrorMessage = "Пароль должен быть не менее 6 символов")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    /// <summary>
    /// Код организации для присоединения при регистрации (опционально)
    /// </summary>
    [MaxLength(20)]
    public string? JoinCode { get; set; }
}

/// <summary>
/// Запрос на вход
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на обновление токена
/// </summary>
public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на сброс пароля
/// </summary>
public class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на изменение пароля
/// </summary>
public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

// ==================== AUTH RESPONSES ====================

/// <summary>
/// Ответ с токенами
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
    public List<OrganizationMembershipDto> Organizations { get; set; } = new();
}

/// <summary>
/// Информация о пользователе
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int? CurrentOrganizationId { get; set; }
    public string? CurrentOrganizationName { get; set; }
    public string? CurrentOrganizationRole { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Членство пользователя в организации
/// </summary>
public class OrganizationMembershipDto
{
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public bool IsPersonal { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}
