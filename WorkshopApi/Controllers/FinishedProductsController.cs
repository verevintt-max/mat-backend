using Microsoft.AspNetCore.Mvc;
using WorkshopApi.DTOs;
using WorkshopApi.Models;
using WorkshopApi.Services;

namespace WorkshopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinishedProductsController : ControllerBase
{
    private readonly FinishedProductService _finishedProductService;

    public FinishedProductsController(FinishedProductService finishedProductService)
    {
        _finishedProductService = finishedProductService;
    }

    /// <summary>
    /// Получить список готовой продукции
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<FinishedProductListItemDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] int? productId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var products = await _finishedProductService.GetAllAsync(status, productId, dateFrom, dateTo);
        return Ok(products);
    }

    /// <summary>
    /// Получить готовое изделие по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<FinishedProductResponseDto>> GetById(int id)
    {
        var product = await _finishedProductService.GetByIdAsync(id);
        if (product == null)
            return NotFound(new { message = $"Готовое изделие с ID {id} не найдено" });

        return Ok(product);
    }

    /// <summary>
    /// Продать изделие
    /// </summary>
    [HttpPost("{id}/sell")]
    public async Task<ActionResult<FinishedProductResponseDto>> Sell(int id, [FromBody] SellProductDto dto)
    {
        try
        {
            var product = await _finishedProductService.SellAsync(id, dto);
            if (product == null)
                return NotFound(new { message = $"Готовое изделие с ID {id} не найдено" });

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Списать изделие как брак
    /// </summary>
    [HttpPost("{id}/write-off")]
    public async Task<ActionResult<FinishedProductResponseDto>> WriteOff(int id, [FromBody] WriteOffProductDto dto)
    {
        try
        {
            var product = await _finishedProductService.WriteOffAsync(id, dto);
            if (product == null)
                return NotFound(new { message = $"Готовое изделие с ID {id} не найдено" });

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Вернуть изделие на склад
    /// </summary>
    [HttpPost("{id}/return-to-stock")]
    public async Task<ActionResult<FinishedProductResponseDto>> ReturnToStock(int id)
    {
        try
        {
            var product = await _finishedProductService.ReturnToStockAsync(id);
            if (product == null)
                return NotFound(new { message = $"Готовое изделие с ID {id} не найдено" });

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Обновить данные о продаже/списании
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<FinishedProductResponseDto>> Update(int id, [FromBody] FinishedProductUpdateDto dto)
    {
        var product = await _finishedProductService.UpdateAsync(id, dto);
        if (product == null)
            return NotFound(new { message = $"Готовое изделие с ID {id} не найдено" });

        return Ok(product);
    }

    /// <summary>
    /// Получить сводку по готовой продукции
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<FinishedProductSummaryDto>> GetSummary()
    {
        var summary = await _finishedProductService.GetSummaryAsync();
        return Ok(summary);
    }

    /// <summary>
    /// Получить доступные статусы
    /// </summary>
    [HttpGet("statuses")]
    public ActionResult<List<object>> GetStatuses()
    {
        var statuses = new List<object>
        {
            new { value = FinishedProductStatus.InStock, label = "На складе" },
            new { value = FinishedProductStatus.Sold, label = "Продано" },
            new { value = FinishedProductStatus.WrittenOff, label = "Списано" }
        };
        return Ok(statuses);
    }
}
