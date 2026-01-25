using Microsoft.AspNetCore.Mvc;
using WorkshopApi.DTOs;
using WorkshopApi.Services;

namespace WorkshopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoryController : ControllerBase
{
    private readonly OperationHistoryService _historyService;

    public HistoryController(OperationHistoryService historyService)
    {
        _historyService = historyService;
    }

    /// <summary>
    /// Получить историю операций с фильтрацией и пагинацией
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<OperationHistoryItemDto>>> GetHistory(
        [FromQuery] string? operationType = null,
        [FromQuery] string? entityType = null,
        [FromQuery] int? entityId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] bool includeCancelled = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var filter = new OperationHistoryFilterDto
        {
            OperationType = operationType,
            EntityType = entityType,
            EntityId = entityId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            IncludeCancelled = includeCancelled,
            Page = page,
            PageSize = pageSize
        };

        var result = await _historyService.GetHistoryAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Получить последние операции
    /// </summary>
    [HttpGet("recent")]
    public async Task<ActionResult<List<OperationHistoryItemDto>>> GetRecent([FromQuery] int count = 10)
    {
        var operations = await _historyService.GetRecentAsync(count);
        return Ok(operations);
    }
}
