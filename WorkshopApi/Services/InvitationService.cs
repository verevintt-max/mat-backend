using Microsoft.EntityFrameworkCore;
using WorkshopApi.Data;
using WorkshopApi.DTOs;
using WorkshopApi.Models;

namespace WorkshopApi.Services;

public class InvitationService
{
    private readonly WorkshopDbContext _context;
    private readonly ILogger<InvitationService> _logger;

    public InvitationService(WorkshopDbContext context, ILogger<InvitationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Создать приглашение в организацию (только Owner)
    /// </summary>
    public async Task<InvitationDto> CreateInvitationAsync(int organizationId, int invitedById, CreateInvitationRequest request)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new InvalidOperationException("Организация не найдена");

        if (organization.OwnerId != invitedById)
            throw new UnauthorizedAccessException("Только владелец может приглашать участников");

        if (organization.IsPersonal)
            throw new InvalidOperationException("Нельзя приглашать в личную организацию");

        var email = request.Email.ToLower();

        // Проверяем, нет ли уже такого участника
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (existingUser != null)
        {
            var existingMembership = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.UserId == existingUser.Id && m.OrganizationId == organizationId);

            if (existingMembership != null)
                throw new InvalidOperationException("Пользователь уже является участником организации");
        }

        // Проверяем, нет ли уже активного приглашения
        var existingInvitation = await _context.Invitations
            .FirstOrDefaultAsync(i => i.OrganizationId == organizationId
                && i.Email.ToLower() == email
                && i.Status == InvitationStatus.Pending
                && i.ExpiresAt > DateTime.UtcNow);

        if (existingInvitation != null)
            throw new InvalidOperationException("Активное приглашение для этого email уже существует");

        var inviter = await _context.Users.FindAsync(invitedById);

        var invitation = new Invitation
        {
            OrganizationId = organizationId,
            Email = email,
            Token = GenerateToken(),
            Status = InvitationStatus.Pending,
            InvitedById = invitedById,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // Приглашение действует 7 дней
            CreatedAt = DateTime.UtcNow
        };

        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Создано приглашение для {Email} в организацию {OrgId}", email, organizationId);

        // TODO: Отправить email с приглашением

        return new InvitationDto
        {
            Id = invitation.Id,
            Email = invitation.Email,
            Token = invitation.Token,
            Status = invitation.Status.ToString(),
            OrganizationId = organizationId,
            OrganizationName = organization.Name,
            InvitedByName = inviter?.FullName ?? "",
            ExpiresAt = invitation.ExpiresAt,
            CreatedAt = invitation.CreatedAt,
            IsExpired = invitation.IsExpired,
            CanBeAccepted = invitation.CanBeAccepted
        };
    }

    /// <summary>
    /// Получить входящие приглашения пользователя
    /// </summary>
    public async Task<List<InvitationDto>> GetUserInvitationsAsync(string email)
    {
        return await _context.Invitations
            .Include(i => i.Organization)
            .Include(i => i.InvitedBy)
            .Where(i => i.Email.ToLower() == email.ToLower() && i.Status == InvitationStatus.Pending)
            .Select(i => new InvitationDto
            {
                Id = i.Id,
                Email = i.Email,
                Token = i.Token,
                Status = i.Status.ToString(),
                OrganizationId = i.OrganizationId,
                OrganizationName = i.Organization!.Name,
                InvitedByName = i.InvitedBy!.FullName,
                ExpiresAt = i.ExpiresAt,
                CreatedAt = i.CreatedAt,
                IsExpired = i.ExpiresAt < DateTime.UtcNow,
                CanBeAccepted = i.Status == InvitationStatus.Pending && i.ExpiresAt > DateTime.UtcNow
            })
            .ToListAsync();
    }

    /// <summary>
    /// Получить приглашения организации (только Owner)
    /// </summary>
    public async Task<List<InvitationDto>> GetOrganizationInvitationsAsync(int organizationId, int userId)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
            throw new InvalidOperationException("Организация не найдена");

        if (organization.OwnerId != userId)
            throw new UnauthorizedAccessException("Только владелец может просматривать приглашения");

        return await _context.Invitations
            .Include(i => i.InvitedBy)
            .Where(i => i.OrganizationId == organizationId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InvitationDto
            {
                Id = i.Id,
                Email = i.Email,
                Token = i.Token,
                Status = i.Status.ToString(),
                OrganizationId = i.OrganizationId,
                OrganizationName = organization.Name,
                InvitedByName = i.InvitedBy!.FullName,
                ExpiresAt = i.ExpiresAt,
                CreatedAt = i.CreatedAt,
                IsExpired = i.ExpiresAt < DateTime.UtcNow,
                CanBeAccepted = i.Status == InvitationStatus.Pending && i.ExpiresAt > DateTime.UtcNow
            })
            .ToListAsync();
    }

    /// <summary>
    /// Принять приглашение
    /// </summary>
    public async Task<OrganizationMembershipDto> AcceptInvitationAsync(string token, int userId)
    {
        var invitation = await _context.Invitations
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation == null)
            throw new InvalidOperationException("Приглашение не найдено");

        if (!invitation.CanBeAccepted)
        {
            if (invitation.IsExpired)
                throw new InvalidOperationException("Срок действия приглашения истек");
            if (invitation.Status != InvitationStatus.Pending)
                throw new InvalidOperationException($"Приглашение уже {(invitation.Status == InvitationStatus.Accepted ? "принято" : "отклонено")}");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Пользователь не найден");

        // Проверяем, что приглашение для этого пользователя
        if (user.Email.ToLower() != invitation.Email.ToLower())
            throw new UnauthorizedAccessException("Это приглашение предназначено для другого email");

        // Проверяем, не является ли уже участником
        var existingMembership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == invitation.OrganizationId);

        if (existingMembership != null)
            throw new InvalidOperationException("Вы уже являетесь участником этой организации");

        // Создаем членство
        var membership = new OrganizationMember
        {
            OrganizationId = invitation.OrganizationId,
            UserId = userId,
            Role = OrganizationRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        _context.OrganizationMembers.Add(membership);

        // Обновляем статус приглашения
        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Пользователь {UserId} принял приглашение в организацию {OrgId}",
            userId, invitation.OrganizationId);

        return new OrganizationMembershipDto
        {
            OrganizationId = invitation.OrganizationId,
            OrganizationName = invitation.Organization!.Name,
            IsPersonal = invitation.Organization.IsPersonal,
            Role = OrganizationRole.Member.ToString(),
            JoinedAt = membership.JoinedAt
        };
    }

    /// <summary>
    /// Отклонить приглашение
    /// </summary>
    public async Task RejectInvitationAsync(string token, int userId)
    {
        var invitation = await _context.Invitations
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation == null)
            throw new InvalidOperationException("Приглашение не найдено");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Пользователь не найден");

        // Проверяем, что приглашение для этого пользователя
        if (user.Email.ToLower() != invitation.Email.ToLower())
            throw new UnauthorizedAccessException("Это приглашение предназначено для другого email");

        if (invitation.Status != InvitationStatus.Pending)
            throw new InvalidOperationException("Приглашение уже обработано");

        invitation.Status = InvitationStatus.Rejected;
        invitation.RejectedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Пользователь {UserId} отклонил приглашение {InvitationId}",
            userId, invitation.Id);
    }

    /// <summary>
    /// Отменить приглашение (только Owner)
    /// </summary>
    public async Task CancelInvitationAsync(int invitationId, int userId)
    {
        var invitation = await _context.Invitations
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.Id == invitationId);

        if (invitation == null)
            throw new InvalidOperationException("Приглашение не найдено");

        if (invitation.Organization?.OwnerId != userId)
            throw new UnauthorizedAccessException("Только владелец организации может отменять приглашения");

        if (invitation.Status != InvitationStatus.Pending)
            throw new InvalidOperationException("Можно отменить только ожидающее приглашение");

        invitation.Status = InvitationStatus.Cancelled;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Приглашение {InvitationId} отменено", invitationId);
    }

    private static string GenerateToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_")
            .Replace("+", "-")
            .TrimEnd('=');
    }
}
