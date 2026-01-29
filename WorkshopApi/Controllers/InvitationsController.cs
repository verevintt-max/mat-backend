using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopApi.DTOs;
using WorkshopApi.Services;

namespace WorkshopApi.Controllers;

[ApiController]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly InvitationService _invitationService;
    private readonly AuthService _authService;
    private readonly ILogger<InvitationsController> _logger;

    public InvitationsController(
        InvitationService invitationService,
        AuthService authService,
        ILogger<InvitationsController> logger)
    {
        _invitationService = invitationService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Получить входящие приглашения текущего пользователя
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<InvitationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<InvitationDto>>> GetMyInvitations()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user == null) return Unauthorized();

        var invitations = await _invitationService.GetUserInvitationsAsync(user.Email);
        return Ok(invitations);
    }

    /// <summary>
    /// Принять приглашение
    /// </summary>
    [HttpPost("{token}/accept")]
    [Authorize]
    [ProducesResponseType(typeof(OrganizationMembershipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrganizationMembershipDto>> AcceptInvitation(string token)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var membership = await _invitationService.AcceptInvitationAsync(token, userId.Value);
            return Ok(membership);
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

    /// <summary>
    /// Отклонить приглашение
    /// </summary>
    [HttpPost("{token}/reject")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectInvitation(string token)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            await _invitationService.RejectInvitationAsync(token, userId.Value);
            return Ok(new { message = "Приглашение отклонено" });
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

    /// <summary>
    /// Отменить приглашение (только Owner организации)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelInvitation(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            await _invitationService.CancelInvitationAsync(id, userId.Value);
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

    // ==================== HELPER METHODS ====================

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
