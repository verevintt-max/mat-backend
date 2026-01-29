using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopApi.DTOs;
using WorkshopApi.Services;

namespace WorkshopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductionsController : BaseApiController
{
    private readonly ProductionService _productionService;

    public ProductionsController(ProductionService productionService)
    {
        _productionService = productionService;
    }

    /// <summary>
    /// Получить список всех производств
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ProductionListItemDto>>> GetAll(
        [FromQuery] int? productId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] bool includeCancelled = false)
    {
        if (!TryValidateOrganizationContext(out var error))
            return error!;

        var productions = await _productionService.GetAllAsync(OrganizationId!.Value, productId, dateFrom, dateTo, includeCancelled);
        return Ok(productions);
    }

    /// <summary>
    /// Получить производство по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductionResponseDto>> GetById(int id)
    {
        if (!TryValidateOrganizationContext(out var error))
            return error!;

        var production = await _productionService.GetByIdAsync(OrganizationId!.Value, id);
        if (production == null)
            return NotFound(new { message = $"Производство с ID {id} не найдено" });

        return Ok(production);
    }

    /// <summary>
    /// Проверить наличие материалов для производства
    /// </summary>
    [HttpGet("check-availability")]
    public async Task<ActionResult<ProductionCheckResultDto>> CheckAvailability(
        [FromQuery] int productId,
        [FromQuery] int quantity)
    {
        try
        {
            if (!TryValidateOrganizationContext(out var error))
                return error!;

            var result = await _productionService.CheckAvailabilityAsync(OrganizationId!.Value, productId, quantity);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Создать производство (с автоматическим списанием материалов)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductionResponseDto>> Create([FromBody] ProductionCreateDto dto)
    {
        try
        {
            if (!TryValidateOrganizationContext(out var error))
                return error!;

            var ctx = GetOrganizationContext();
            var production = await _productionService.CreateAsync(ctx, dto);
            return CreatedAtAction(nameof(GetById), new { id = production.Id }, production);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Отменить производство (возврат материалов)
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> Cancel(int id)
    {
        try
        {
            if (!TryValidateOrganizationContext(out var error))
                return error!;

            var ctx = GetOrganizationContext();
            var result = await _productionService.CancelAsync(ctx, id);
            if (!result)
                return NotFound(new { message = $"Производство с ID {id} не найдено" });

            return Ok(new { message = "Производство отменено, материалы возвращены на склад" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Полное удаление производства из базы данных
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            if (!TryValidateOrganizationContext(out var error))
                return error!;

            var ctx = GetOrganizationContext();
            var result = await _productionService.DeleteAsync(ctx, id);
            if (!result)
                return NotFound(new { message = $"Производство с ID {id} не найдено" });

            return Ok(new { message = "Производство удалено" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
