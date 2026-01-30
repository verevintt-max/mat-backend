using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopApi.DTOs;
using WorkshopApi.Models;
using WorkshopApi.Services;

namespace WorkshopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FinishedProductsController : BaseApiController
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
        if (!TryValidateOrganizationContext(out var error))
            return error!;

        var products = await _finishedProductService.GetAllAsync(OrganizationId!.Value, status, productId, dateFrom, dateTo);
        return Ok(products);
    }

    /// <summary>
    /// Получить готовое изделие по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<FinishedProductResponseDto>> GetById(int id)
    {
        if (!TryValidateOrganizationContext(out var error))
            return error!;

        var product = await _finishedProductService.GetByIdAsync(OrganizationId!.Value, id);
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
            if (!TryValidateOrganizationContext(out var error))
                return error!;

            var ctx = GetOrganizationContext();
            var product = await _finishedProductService.SellAsync(ctx, id, dto);
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
            if (!TryValidateOrganizationContext(out var error))
                return error!;

            var ctx = GetOrganizationContext();
            var product = await _finishedProductService.WriteOffAsync(ctx, id, dto);
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
            if (!TryValidateOrganizationContext(out var error))
                return error!;

            var ctx = GetOrganizationContext();
            var product = await _finishedProductService.ReturnToStockAsync(ctx, id);
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
        if (!TryValidateOrganizationContext(out var error))
            return error!;

        var ctx = GetOrganizationContext();
        var product = await _finishedProductService.UpdateAsync(ctx, id, dto);
        if (product == null)
            return NotFound(new { message = $"Готовое изделие с ID {id} не найдено" });

        return Ok(product);
    }

    /// <summary>
    /// Удалить готовое изделие (только со статусом "На складе")
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            if (!TryValidateOrganizationContext(out var error))
                return error!;

            var ctx = GetOrganizationContext();
            var result = await _finishedProductService.DeleteAsync(ctx, id);
            if (!result)
                return NotFound(new { message = $"Готовое изделие с ID {id} не найдено" });

            return Ok(new { message = "Готовое изделие удалено" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Получить сводку по готовой продукции
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<FinishedProductSummaryDto>> GetSummary()
    {
        if (!TryValidateOrganizationContext(out var error))
            return error!;

        var summary = await _finishedProductService.GetSummaryAsync(OrganizationId!.Value);
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
