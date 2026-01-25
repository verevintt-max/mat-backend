using Microsoft.AspNetCore.Mvc;
using WorkshopApi.DTOs;
using WorkshopApi.Services;

namespace WorkshopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductionsController : ControllerBase
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
        var productions = await _productionService.GetAllAsync(productId, dateFrom, dateTo, includeCancelled);
        return Ok(productions);
    }

    /// <summary>
    /// Получить производство по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductionResponseDto>> GetById(int id)
    {
        var production = await _productionService.GetByIdAsync(id);
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
            var result = await _productionService.CheckAvailabilityAsync(productId, quantity);
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
            var production = await _productionService.CreateAsync(dto);
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
            var result = await _productionService.CancelAsync(id);
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
            var result = await _productionService.DeleteAsync(id);
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
