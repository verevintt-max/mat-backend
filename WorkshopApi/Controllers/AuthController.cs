using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopApi.DTOs;
using WorkshopApi.Services;

namespace WorkshopApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var ipAddress = GetIpAddress();
            var result = await _authService.RegisterAsync(request, ipAddress);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Вход в систему
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var ipAddress = GetIpAddress();
            var result = await _authService.LoginAsync(request, ipAddress);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Обновление токенов
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest? request = null)
    {
        try
        {
            // Пробуем получить refresh token из cookie или из тела запроса
            var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { message = "Refresh token не предоставлен" });
            }

            var ipAddress = GetIpAddress();
            var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Выход из системы
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request = null)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var ipAddress = GetIpAddress();
            await _authService.LogoutAsync(refreshToken, ipAddress);
        }

        // Удаляем cookie
        Response.Cookies.Delete("refreshToken");

        return Ok(new { message = "Успешный выход из системы" });
    }

    /// <summary>
    /// Получить информацию о текущем пользователе
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Не авторизован" });
        }

        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user == null)
        {
            return Unauthorized(new { message = "Пользователь не найден" });
        }

        var currentMembership = user.CurrentOrganizationId.HasValue
            ? await _authService.GetUserMembershipAsync(userId.Value, user.CurrentOrganizationId.Value)
            : null;

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            CurrentOrganizationId = user.CurrentOrganizationId,
            CurrentOrganizationName = currentMembership?.Organization?.Name,
            CurrentOrganizationRole = currentMembership?.Role.ToString(),
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Получить список организаций текущего пользователя
    /// </summary>
    [HttpGet("organizations")]
    [Authorize]
    [ProducesResponseType(typeof(List<OrganizationMembershipDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrganizationMembershipDto>>> GetUserOrganizations()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Не авторизован" });
        }

        var organizations = await _authService.GetUserOrganizationsAsync(userId.Value);
        return Ok(organizations);
    }

    /// <summary>
    /// Сменить текущую организацию
    /// </summary>
    [HttpPost("switch-organization")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> SwitchOrganization([FromBody] SwitchOrganizationRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Не авторизован" });
            }

            var ipAddress = GetIpAddress();
            var result = await _authService.SwitchOrganizationAsync(userId.Value, request.OrganizationId, ipAddress);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==================== HELPER METHODS ====================

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].FirstOrDefault();
        }
        return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // В production
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}
