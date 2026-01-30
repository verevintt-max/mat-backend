using Microsoft.EntityFrameworkCore;
using WorkshopApi.Data;
using WorkshopApi.DTOs;
using WorkshopApi.Models;

namespace WorkshopApi.Services;

public class OrganizationService
{
    private readonly WorkshopDbContext _context;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(WorkshopDbContext context, ILogger<OrganizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Получить список организаций пользователя
    /// </summary>
    public async Task<List<OrganizationDto>> GetUserOrganizationsAsync(int userId)
    {
        return await _context.OrganizationMembers
            .Include(m => m.Organization)
                .ThenInclude(o => o!.Owner)
            .Include(m => m.Organization)
                .ThenInclude(o => o!.Members)
            .Where(m => m.UserId == userId)
            .Select(m => new OrganizationDto
            {
                Id = m.Organization!.Id,
                Name = m.Organization.Name,
                Description = m.Organization.Description,
                IsPersonal = m.Organization.IsPersonal,
                JoinCode = m.Role == OrganizationRole.Owner ? m.Organization.JoinCode : null,
                OwnerId = m.Organization.OwnerId,
                OwnerName = m.Organization.Owner!.FullName,
                MembersCount = m.Organization.Members.Count,
                CreatedAt = m.Organization.CreatedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// Получить организацию по ID
    /// </summary>
    public async Task<OrganizationDetailDto?> GetOrganizationAsync(int organizationId, int userId)
    {
        var membership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == organizationId);

        if (membership == null)
            return null;

        var organization = await _context.Organizations
            .Include(o => o.Owner)
            .Include(o => o.Members)
                .ThenInclude(m => m.User)
            .Include(o => o.Invitations.Where(i => i.Status == InvitationStatus.Pending))
                .ThenInclude(i => i.InvitedBy)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            return null;

        return new OrganizationDetailDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Description = organization.Description,
            IsPersonal = organization.IsPersonal,
            JoinCode = membership.Role == OrganizationRole.Owner ? organization.JoinCode : null,
            OwnerId = organization.OwnerId,
            OwnerName = organization.Owner?.FullName ?? "",
            MembersCount = organization.Members.Count,
            CreatedAt = organization.CreatedAt,
            Members = organization.Members.Select(m => new OrganizationMemberDto
            {
                UserId = m.UserId,
                Email = m.User!.Email,
                Username = m.User.Username,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                FullName = m.User.FullName,
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt
            }).ToList(),
            PendingInvitations = membership.Role == OrganizationRole.Owner
                ? organization.Invitations.Select(i => new InvitationDto
                {
                    Id = i.Id,
                    Email = i.Email,
                    Token = i.Token,
                    Status = i.Status.ToString(),
                    OrganizationId = i.OrganizationId,
                    OrganizationName = organization.Name,
                    InvitedByName = i.InvitedBy?.FullName ?? "",
                    ExpiresAt = i.ExpiresAt,
                    CreatedAt = i.CreatedAt,
                    IsExpired = i.IsExpired,
                    CanBeAccepted = i.CanBeAccepted
                }).ToList()
                : new List<InvitationDto>()
        };
    }

    /// <summary>
    /// Создать новую организацию
    /// </summary>
    public async Task<OrganizationDto> CreateOrganizationAsync(int userId, CreateOrganizationRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Пользователь не найден");

        var organization = new Organization
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = userId,
            IsPersonal = false,
            JoinCode = GenerateJoinCode(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        // Добавляем создателя как владельца
        var membership = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = userId,
            Role = OrganizationRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        _context.OrganizationMembers.Add(membership);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Создана организация {OrgName} пользователем {UserId}", organization.Name, userId);

        return new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Description = organization.Description,
            IsPersonal = false,
            JoinCode = organization.JoinCode,
            OwnerId = userId,
            OwnerName = user.FullName,
            MembersCount = 1,
            CreatedAt = organization.CreatedAt
        };
    }

    /// <summary>
    /// Обновить организацию (только Owner)
    /// </summary>
    public async Task<OrganizationDto> UpdateOrganizationAsync(int organizationId, int userId, UpdateOrganizationRequest request)
    {
        var organization = await _context.Organizations
            .Include(o => o.Owner)
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new InvalidOperationException("Организация не найдена");

        if (organization.OwnerId != userId)
            throw new UnauthorizedAccessException("Только владелец может редактировать организацию");

        if (organization.IsPersonal)
            throw new InvalidOperationException("Нельзя редактировать личную организацию");

        if (!string.IsNullOrEmpty(request.Name))
            organization.Name = request.Name;

        if (request.Description != null)
            organization.Description = request.Description;

        organization.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Обновлена организация {OrgId}", organizationId);

        return new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Description = organization.Description,
            IsPersonal = organization.IsPersonal,
            JoinCode = organization.JoinCode,
            OwnerId = organization.OwnerId,
            OwnerName = organization.Owner?.FullName ?? "",
            MembersCount = organization.Members.Count,
            CreatedAt = organization.CreatedAt
        };
    }

    /// <summary>
    /// Удалить организацию (только Owner)
    /// </summary>
    public async Task DeleteOrganizationAsync(int organizationId, int userId)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new InvalidOperationException("Организация не найдена");

        if (organization.OwnerId != userId)
            throw new UnauthorizedAccessException("Только владелец может удалить организацию");

        if (organization.IsPersonal)
            throw new InvalidOperationException("Нельзя удалить личную организацию");

        // Переключаем всех участников на их личные организации
        var members = await _context.OrganizationMembers
            .Include(m => m.User)
            .Where(m => m.OrganizationId == organizationId)
            .ToListAsync();

        foreach (var member in members)
        {
            if (member.User?.CurrentOrganizationId == organizationId)
            {
                // Находим личную организацию пользователя
                var personalOrg = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.OwnerId == member.UserId && o.IsPersonal);

                if (personalOrg != null)
                {
                    member.User.CurrentOrganizationId = personalOrg.Id;
                }
            }
        }

        _context.Organizations.Remove(organization);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Удалена организация {OrgId} пользователем {UserId}", organizationId, userId);
    }

    /// <summary>
    /// Сгенерировать новый код для присоединения
    /// </summary>
    public async Task<string> RegenerateJoinCodeAsync(int organizationId, int userId)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new InvalidOperationException("Организация не найдена");

        if (organization.OwnerId != userId)
            throw new UnauthorizedAccessException("Только владелец может изменить код организации");

        if (organization.IsPersonal)
            throw new InvalidOperationException("Личная организация не имеет кода присоединения");

        organization.JoinCode = GenerateJoinCode();
        organization.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return organization.JoinCode;
    }

    /// <summary>
    /// Получить список участников организации
    /// </summary>
    public async Task<List<OrganizationMemberDto>> GetMembersAsync(int organizationId, int userId)
    {
        // Проверяем, что пользователь - участник организации
        var membership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == organizationId);

        if (membership == null)
            throw new UnauthorizedAccessException("Вы не являетесь участником этой организации");

        return await _context.OrganizationMembers
            .Include(m => m.User)
            .Where(m => m.OrganizationId == organizationId)
            .Select(m => new OrganizationMemberDto
            {
                UserId = m.UserId,
                Email = m.User!.Email,
                Username = m.User.Username,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                FullName = m.User.FullName,
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// Удалить участника из организации (только Owner)
    /// </summary>
    public async Task RemoveMemberAsync(int organizationId, int memberUserId, int requestUserId)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new InvalidOperationException("Организация не найдена");

        if (organization.OwnerId != requestUserId)
            throw new UnauthorizedAccessException("Только владелец может удалять участников");

        if (memberUserId == organization.OwnerId)
            throw new InvalidOperationException("Нельзя удалить владельца организации");

        var membership = await _context.OrganizationMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == memberUserId);

        if (membership == null)
            throw new InvalidOperationException("Участник не найден");

        // Переключаем пользователя на личную организацию
        if (membership.User?.CurrentOrganizationId == organizationId)
        {
            var personalOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.OwnerId == memberUserId && o.IsPersonal);

            if (personalOrg != null)
            {
                membership.User.CurrentOrganizationId = personalOrg.Id;
            }
        }

        _context.OrganizationMembers.Remove(membership);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Участник {MemberId} удален из организации {OrgId}", memberUserId, organizationId);
    }

    /// <summary>
    /// Покинуть организацию (для участников)
    /// </summary>
    public async Task LeaveOrganizationAsync(int organizationId, int userId)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new InvalidOperationException("Организация не найдена");

        if (organization.OwnerId == userId)
            throw new InvalidOperationException("Владелец не может покинуть организацию. Сначала передайте владение другому участнику");

        if (organization.IsPersonal)
            throw new InvalidOperationException("Нельзя покинуть личную организацию");

        var membership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId);

        if (membership == null)
            throw new InvalidOperationException("Вы не являетесь участником этой организации");

        // Переключаем на личную организацию
        var user = await _context.Users.FindAsync(userId);
        if (user?.CurrentOrganizationId == organizationId)
        {
            var personalOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.OwnerId == userId && o.IsPersonal);

            if (personalOrg != null)
            {
                user.CurrentOrganizationId = personalOrg.Id;
            }
        }

        _context.OrganizationMembers.Remove(membership);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Пользователь {UserId} покинул организацию {OrgId}", userId, organizationId);
    }

    /// <summary>
    /// Передать владение организацией
    /// </summary>
    public async Task TransferOwnershipAsync(int organizationId, int currentOwnerId, int newOwnerId)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new InvalidOperationException("Организация не найдена");

        if (organization.OwnerId != currentOwnerId)
            throw new UnauthorizedAccessException("Только владелец может передать владение");

        if (organization.IsPersonal)
            throw new InvalidOperationException("Нельзя передать владение личной организацией");

        // Проверяем, что новый владелец - участник организации
        var newOwnerMembership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == newOwnerId);

        if (newOwnerMembership == null)
            throw new InvalidOperationException("Новый владелец должен быть участником организации");

        // Меняем роли
        var currentOwnerMembership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == currentOwnerId);

        if (currentOwnerMembership != null)
        {
            currentOwnerMembership.Role = OrganizationRole.Member;
            currentOwnerMembership.UpdatedAt = DateTime.UtcNow;
        }

        newOwnerMembership.Role = OrganizationRole.Owner;
        newOwnerMembership.UpdatedAt = DateTime.UtcNow;

        organization.OwnerId = newOwnerId;
        organization.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Владение организацией {OrgId} передано от {OldOwner} к {NewOwner}",
            organizationId, currentOwnerId, newOwnerId);
    }

    /// <summary>
    /// Присоединиться к организации по коду
    /// </summary>
    public async Task<OrganizationDto> JoinByCodeAsync(int userId, string joinCode)
    {
        if (string.IsNullOrWhiteSpace(joinCode))
            throw new InvalidOperationException("Код организации обязателен");

        var organization = await _context.Organizations
            .Include(o => o.Owner)
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.JoinCode == joinCode.Trim().ToUpper());

        if (organization == null)
            throw new InvalidOperationException("Организация с таким кодом не найдена");

        if (organization.IsPersonal)
            throw new InvalidOperationException("Нельзя присоединиться к личной организации");

        // Проверяем, не является ли уже участником
        var existingMembership = organization.Members.FirstOrDefault(m => m.UserId == userId);
        if (existingMembership != null)
            throw new InvalidOperationException("Вы уже являетесь участником этой организации");

        // Добавляем участника
        var membership = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = userId,
            Role = OrganizationRole.Member
        };

        _context.OrganizationMembers.Add(membership);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Пользователь {UserId} присоединился к организации {OrgId} по коду", userId, organization.Id);

        return new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Description = organization.Description,
            IsPersonal = organization.IsPersonal,
            JoinCode = organization.JoinCode,
            OwnerId = organization.OwnerId,
            OwnerName = organization.Owner?.FullName ?? organization.Owner?.Username ?? "",
            MembersCount = organization.Members.Count + 1,
            CreatedAt = organization.CreatedAt
        };
    }

    private static string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
