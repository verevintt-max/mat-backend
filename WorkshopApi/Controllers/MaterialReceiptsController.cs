using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopApi.DTOs;
using WorkshopApi.Services;

namespace WorkshopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaterialReceiptsController : BaseApiController
{
    private readonly MaterialReceiptService _receiptService;

    public MaterialReceiptsController(MaterialReceiptService receiptService)
    {
        _receiptService = receiptService;
    }

    /// <summary>
    /// Получить список всех поступлений
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<MaterialReceiptListItemDto>>> GetAll(
        [FromQuery] int? materialId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        if (!TryValidateOrganizationContext(out var error))
            return error!;

        var receipts = await _receiptService.GetAllAsync(OrganizationId!.Value, materialId, dateFrom, dateTo);
        return Ok(receipts);
    }

    /// <summary>
    /// Получить поступление по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MaterialReceiptResponseDto>> GetById(int id)
    {
        if (!TryValidateOrganizationContext(out var error))
            return error!;

        var receipt = await _receiptService.GetByIdAsync(OrganizationId!.Value, id);
        if (receipt == null)
            return NotFound(new { message = $"Поступление с ID {id} не найдено" });

        return Ok(receipt);
    }

    /// <summary>
    /// Создать новое поступление
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MaterialReceiptResponseDto>> Create([FromBody] MaterialReceiptCreateDto dto)
    {
        try
        {
            if (!TryValidateOrganizationContext(out var error))
                return error!;

            var ctx = GetOrganizationContext();
            var receipt = await _receiptService.CreateAsync(ctx, dto);
            return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, receipt);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Обновить поступление
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<MaterialReceiptResponseDto>> Update(int id, [FromBody] MaterialReceiptUpdateDto dto)
    {
        try
        {
            if (!TryValidateOrganizationContext(out var error))
                return error!;

            var ctx = GetOrganizationContext();
            var receipt = await _receiptService.UpdateAsync(ctx, id, dto);
            if (receipt == null)
                return NotFound(new { message = $"Поступление с ID {id} не найдено" });

            return Ok(receipt);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Удалить поступление
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id, [FromQuery] bool force = false)
    {
        try
        {
            if (!TryValidateOrganizationContext(out var error))
                return error!;

            var ctx = GetOrganizationContext();
            var result = await _receiptService.DeleteAsync(ctx, id, force);
            if (!result)
                return NotFound(new { message = $"Поступление с ID {id} не найдено" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
