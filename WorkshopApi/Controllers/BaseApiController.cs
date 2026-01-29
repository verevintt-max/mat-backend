using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WorkshopApi.Controllers;

/// <summary>
/// Базовый контроллер с поддержкой контекста организации
/// </summary>
[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Получить ID текущего пользователя
    /// </summary>
    protected int? UserId
    {
        get
        {
            var userIdClaim = User.FindFirst("userId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    /// <summary>
    /// Получить ID текущей организации из JWT токена
    /// </summary>
    protected int? OrganizationId
    {
        get
        {
            var orgIdClaim = User.FindFirst("organizationId")?.Value;
            return int.TryParse(orgIdClaim, out var orgId) ? orgId : null;
        }
    }

    /// <summary>
    /// Получить роль пользователя в текущей организации
    /// </summary>
    protected string? OrganizationRole => User.FindFirst("role")?.Value;

    /// <summary>
    /// Проверить, является ли пользователь владельцем текущей организации
    /// </summary>
    protected bool IsOwner => OrganizationRole?.Equals("Owner", StringComparison.OrdinalIgnoreCase) ?? false;

    /// <summary>
    /// Проверить, что пользователь авторизован и имеет текущую организацию.
    /// Возвращает true если контекст валиден, false и устанавливает errorResult если нет.
    /// </summary>
    protected bool TryValidateOrganizationContext(out ActionResult? errorResult)
    {
        if (UserId == null)
        {
            errorResult = Unauthorized(new { message = "Не авторизован" });
            return false;
        }

        if (OrganizationId == null)
        {
            errorResult = BadRequest(new { message = "Не выбрана активная организация" });
            return false;
        }

        errorResult = null;
        return true;
    }

    /// <summary>
    /// Получить контекст организации
    /// </summary>
    protected OrganizationContext GetOrganizationContext()
    {
        return new OrganizationContext
        {
            UserId = UserId!.Value,
            OrganizationId = OrganizationId!.Value,
            Role = OrganizationRole ?? "Member"
        };
    }
}

/// <summary>
/// Контекст организации для сервисов
/// </summary>
public class OrganizationContext
{
    public int UserId { get; set; }
    public int OrganizationId { get; set; }
    public string Role { get; set; } = "Member";
    public bool IsOwner => Role.Equals("Owner", StringComparison.OrdinalIgnoreCase);
}
