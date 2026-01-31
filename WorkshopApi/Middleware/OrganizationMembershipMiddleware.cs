using Microsoft.EntityFrameworkCore;
using WorkshopApi.Data;

namespace WorkshopApi.Middleware;

/// <summary>
/// Middleware для проверки членства пользователя в организации.
/// Если пользователь больше не является членом организации из его токена,
/// возвращается ошибка 403 и клиент должен обновить данные.
/// </summary>
public class OrganizationMembershipMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OrganizationMembershipMiddleware> _logger;

    public OrganizationMembershipMiddleware(RequestDelegate next, ILogger<OrganizationMembershipMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, WorkshopDbContext dbContext)
    {
        // Пропускаем анонимные эндпоинты и auth эндпоинты
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/auth/") || path.Contains("/swagger") || path.Contains("/health"))
        {
            await _next(context);
            return;
        }

        // Проверяем только авторизованные запросы
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst("userId")?.Value;
            var orgIdClaim = context.User.FindFirst("organizationId")?.Value;

            if (int.TryParse(userIdClaim, out var userId) && int.TryParse(orgIdClaim, out var orgId))
            {
                // Проверяем, является ли пользователь членом организации
                var isMember = await dbContext.OrganizationMembers
                    .AnyAsync(m => m.UserId == userId && m.OrganizationId == orgId);

                if (!isMember)
                {
                    _logger.LogWarning(
                        "Пользователь {UserId} пытается получить доступ к организации {OrgId}, членом которой не является",
                        userId, orgId);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "Вы больше не являетесь членом этой организации. Пожалуйста, обновите страницу.",
                        code = "MEMBERSHIP_REVOKED"
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method для регистрации middleware
/// </summary>
public static class OrganizationMembershipMiddlewareExtensions
{
    public static IApplicationBuilder UseOrganizationMembershipValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OrganizationMembershipMiddleware>();
    }
}
