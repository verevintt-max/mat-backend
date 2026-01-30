using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WorkshopApi.Data;
using WorkshopApi.DTOs;
using WorkshopApi.Models;

namespace WorkshopApi.Services;

public class AuthService
{
    private readonly WorkshopDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        WorkshopDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress = null)
    {
        // Проверка существующего пользователя
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
        {
            throw new InvalidOperationException("Пользователь с таким email уже существует");
        }

        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower()))
        {
            throw new InvalidOperationException("Пользователь с таким именем уже существует");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Создание пользователя
            var user = new User
            {
                Email = request.Email.ToLower(),
                Username = request.Username,
                PasswordHash = HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            Organization? joinedOrganization = null;

            // Если указан код организации - пытаемся присоединиться
            if (!string.IsNullOrEmpty(request.JoinCode))
            {
                joinedOrganization = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.JoinCode == request.JoinCode.Trim().ToUpper());

                if (joinedOrganization == null)
                {
                    throw new InvalidOperationException("Организация с указанным кодом не найдена");
                }

                // Добавляем пользователя в организацию
                var membership = new OrganizationMember
                {
                    OrganizationId = joinedOrganization.Id,
                    UserId = user.Id,
                    Role = OrganizationRole.Member,
                    JoinedAt = DateTime.UtcNow
                };
                _context.OrganizationMembers.Add(membership);

                // Устанавливаем эту организацию как текущую
                user.CurrentOrganizationId = joinedOrganization.Id;
            }
            else
            {
                // Код не указан - создаём личную организацию
                var personalOrg = new Organization
                {
                    Name = $"Личное пространство {user.Username}",
                    Description = "Личная организация пользователя",
                    OwnerId = user.Id,
                    IsPersonal = true,
                    JoinCode = GenerateJoinCode(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Organizations.Add(personalOrg);
                await _context.SaveChangesAsync();

                // Добавление пользователя как владельца личной организации
                var personalMembership = new OrganizationMember
                {
                    OrganizationId = personalOrg.Id,
                    UserId = user.Id,
                    Role = OrganizationRole.Owner,
                    JoinedAt = DateTime.UtcNow
                };
                _context.OrganizationMembers.Add(personalMembership);

                // Устанавливаем личную организацию как текущую
                user.CurrentOrganizationId = personalOrg.Id;
            }
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Зарегистрирован новый пользователь: {Email}", user.Email);

            return await GenerateAuthResponseAsync(user, ipAddress);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Вход в систему
    /// </summary>
    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null)
    {
        var user = await _context.Users
            .Include(u => u.CurrentOrganization)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Неверный email или пароль");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Учетная запись деактивирована");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Пользователь вошел в систему: {Email}", user.Email);

        return await GenerateAuthResponseAsync(user, ipAddress);
    }

    /// <summary>
    /// Обновление токенов
    /// </summary>
    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
    {
        var token = await _context.RefreshTokens
            .Include(t => t.User)
                .ThenInclude(u => u!.CurrentOrganization)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null)
        {
            throw new UnauthorizedAccessException("Недействительный refresh token");
        }

        if (!token.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token истек или отозван");
        }

        // Отзываем старый токен
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;

        var user = token.User!;
        var response = await GenerateAuthResponseAsync(user, ipAddress, token.Token);

        await _context.SaveChangesAsync();

        return response;
    }

    /// <summary>
    /// Выход из системы (отзыв refresh token)
    /// </summary>
    public async Task LogoutAsync(string refreshToken, string? ipAddress = null)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token != null && token.IsActive)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Отзыв всех токенов пользователя
    /// </summary>
    public async Task RevokeAllTokensAsync(int userId, string? ipAddress = null)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Получение пользователя по ID
    /// </summary>
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.CurrentOrganization)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <summary>
    /// Получение членства пользователя в текущей организации
    /// </summary>
    public async Task<OrganizationMember?> GetUserMembershipAsync(int userId, int organizationId)
    {
        return await _context.OrganizationMembers
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == organizationId);
    }

    /// <summary>
    /// Получение всех организаций пользователя
    /// </summary>
    public async Task<List<OrganizationMembershipDto>> GetUserOrganizationsAsync(int userId)
    {
        return await _context.OrganizationMembers
            .Include(m => m.Organization)
            .Where(m => m.UserId == userId)
            .Select(m => new OrganizationMembershipDto
            {
                OrganizationId = m.OrganizationId,
                OrganizationName = m.Organization!.Name,
                IsPersonal = m.Organization.IsPersonal,
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// Смена текущей организации
    /// </summary>
    public async Task<AuthResponse> SwitchOrganizationAsync(int userId, int organizationId, string? ipAddress = null)
    {
        var membership = await _context.OrganizationMembers
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == organizationId);

        if (membership == null)
        {
            throw new UnauthorizedAccessException("Вы не являетесь участником этой организации");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("Пользователь не найден");
        }

        user.CurrentOrganizationId = organizationId;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GenerateAuthResponseAsync(user, ipAddress);
    }

    // ==================== PRIVATE METHODS ====================

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, string? ipAddress = null, string? replacedToken = null)
    {
        // Получаем членство в текущей организации
        OrganizationMember? currentMembership = null;
        if (user.CurrentOrganizationId.HasValue)
        {
            currentMembership = await _context.OrganizationMembers
                .Include(m => m.Organization)
                .FirstOrDefaultAsync(m => m.UserId == user.Id && m.OrganizationId == user.CurrentOrganizationId);
        }

        // Генерируем JWT токен
        var accessToken = GenerateJwtToken(user, currentMembership);

        // Генерируем refresh token
        var refreshToken = GenerateRefreshToken(ipAddress);
        refreshToken.UserId = user.Id;
        refreshToken.ReplacedByToken = replacedToken;

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        // Получаем все организации пользователя
        var organizations = await GetUserOrganizationsAsync(user.Id);

        var jwtSettings = _configuration.GetSection("Jwt");
        var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "60");

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            User = new UserDto
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
            },
            Organizations = organizations
        };
    }

    private string GenerateJwtToken(User user, OrganizationMember? membership)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("userId", user.Id.ToString())
        };

        if (membership != null)
        {
            claims.Add(new Claim("organizationId", membership.OrganizationId.ToString()));
            claims.Add(new Claim("role", membership.Role.ToString()));
        }

        var expirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(string? ipAddress)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var expirationDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"] ?? "7");

        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    private static string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
