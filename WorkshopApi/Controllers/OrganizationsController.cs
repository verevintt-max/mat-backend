using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopApi.DTOs;
using WorkshopApi.Services;

namespace WorkshopApi.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly OrganizationService _organizationService;
    private readonly InvitationService _invitationService;
    private readonly ILogger<OrganizationsController> _logger;

    public OrganizationsController(
        OrganizationService organizationService,
        InvitationService invitationService,
        ILogger<OrganizationsController> logger)
    {
        _organizationService = organizationService;
        _invitationService = invitationService;
        _logger = logger;
    }

    /// <summary>
    /// Получить список организаций пользователя
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrganizationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrganizationDto>>> GetOrganizations()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var organizations = await _organizationService.GetUserOrganizationsAsync(userId.Value);
        return Ok(organizations);
    }

    /// <summary>
    /// Получить организацию по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrganizationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganizationDetailDto>> GetOrganization(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var organization = await _organizationService.GetOrganizationAsync(id, userId.Value);
        if (organization == null)
        {
            return NotFound(new { message = "Организация не найдена или у вас нет доступа" });
        }

        return Ok(organization);
    }

    /// <summary>
    /// Создать новую организацию
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrganizationDto>> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var organization = await _organizationService.CreateOrganizationAsync(userId.Value, request);
            return CreatedAtAction(nameof(GetOrganization), new { id = organization.Id }, organization);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Обновить организацию (только Owner)
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrganizationDto>> UpdateOrganization(int id, [FromBody] UpdateOrganizationRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var organization = await _organizationService.UpdateOrganizationAsync(id, userId.Value, request);
            return Ok(organization);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Удалить организацию (только Owner)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteOrganization(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            await _organizationService.DeleteOrganizationAsync(id, userId.Value);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Сгенерировать новый код для присоединения (только Owner)
    /// </summary>
    [HttpPost("{id}/regenerate-code")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegenerateJoinCode(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var newCode = await _organizationService.RegenerateJoinCodeAsync(id, userId.Value);
            return Ok(new { joinCode = newCode });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==================== MEMBERS ====================

    /// <summary>
    /// Получить список участников организации
    /// </summary>
    [HttpGet("{id}/members")]
    [ProducesResponseType(typeof(List<OrganizationMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrganizationMemberDto>>> GetMembers(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var members = await _organizationService.GetMembersAsync(id, userId.Value);
            return Ok(members);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Удалить участника из организации (только Owner)
    /// </summary>
    [HttpDelete("{id}/members/{memberId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveMember(int id, int memberId)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            await _organizationService.RemoveMemberAsync(id, memberId, userId.Value);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Покинуть организацию
    /// </summary>
    [HttpPost("{id}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LeaveOrganization(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            await _organizationService.LeaveOrganizationAsync(id, userId.Value);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Присоединиться к организации по коду
    /// </summary>
    [HttpPost("join")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganizationDto>> JoinByCode([FromBody] JoinOrganizationRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var organization = await _organizationService.JoinByCodeAsync(userId.Value, request.JoinCode);
            return Ok(organization);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Передать владение организацией (только Owner)
    /// </summary>
    [HttpPost("{id}/transfer-ownership")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TransferOwnership(int id, [FromBody] TransferOwnershipRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            await _organizationService.TransferOwnershipAsync(id, userId.Value, request.NewOwnerId);
            return Ok(new { message = "Владение успешно передано" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==================== INVITATIONS ====================

    /// <summary>
    /// Создать приглашение в организацию (только Owner)
    /// </summary>
    [HttpPost("{id}/invite")]
    [ProducesResponseType(typeof(InvitationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InvitationDto>> CreateInvitation(int id, [FromBody] CreateInvitationRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var invitation = await _invitationService.CreateInvitationAsync(id, userId.Value, request);
            return Created($"/api/invitations/{invitation.Token}", invitation);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Получить приглашения организации (только Owner)
    /// </summary>
    [HttpGet("{id}/invitations")]
    [ProducesResponseType(typeof(List<InvitationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<InvitationDto>>> GetOrganizationInvitations(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var invitations = await _invitationService.GetOrganizationInvitationsAsync(id, userId.Value);
            return Ok(invitations);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    // ==================== HELPER METHODS ====================

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
