using Microsoft.AspNetCore.Mvc;
using WorkshopApi.DTOs;
using WorkshopApi.Services;

namespace WorkshopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaterialsController : ControllerBase
{
    private readonly MaterialService _materialService;

    public MaterialsController(MaterialService materialService)
    {
        _materialService = materialService;
    }

    /// <summary>
    /// Получить список всех материалов
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<MaterialListItemDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] bool includeArchived = false)
    {
        var materials = await _materialService.GetAllAsync(search, category, includeArchived);
        return Ok(materials);
    }

    /// <summary>
    /// Получить материал по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MaterialResponseDto>> GetById(int id)
    {
        var material = await _materialService.GetByIdAsync(id);
        if (material == null)
            return NotFound(new { message = $"Материал с ID {id} не найден" });

        return Ok(material);
    }

    /// <summary>
    /// Создать новый материал
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MaterialResponseDto>> Create([FromBody] MaterialCreateDto dto)
    {
        try
        {
            var material = await _materialService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = material.Id }, material);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Обновить материал
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<MaterialResponseDto>> Update(int id, [FromBody] MaterialUpdateDto dto)
    {
        try
        {
            var material = await _materialService.UpdateAsync(id, dto);
            if (material == null)
                return NotFound(new { message = $"Материал с ID {id} не найден" });

            return Ok(material);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Удалить материал
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var result = await _materialService.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = $"Материал с ID {id} не найден" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Архивировать материал
    /// </summary>
    [HttpPost("{id}/archive")]
    public async Task<ActionResult<MaterialResponseDto>> Archive(int id)
    {
        var material = await _materialService.UpdateAsync(id, new MaterialUpdateDto { IsArchived = true });
        if (material == null)
            return NotFound(new { message = $"Материал с ID {id} не найден" });

        return Ok(material);
    }

    /// <summary>
    /// Разархивировать материал
    /// </summary>
    [HttpPost("{id}/unarchive")]
    public async Task<ActionResult<MaterialResponseDto>> Unarchive(int id)
    {
        var material = await _materialService.UpdateAsync(id, new MaterialUpdateDto { IsArchived = false });
        if (material == null)
            return NotFound(new { message = $"Материал с ID {id} не найден" });

        return Ok(material);
    }

    /// <summary>
    /// Получить список категорий
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var categories = await _materialService.GetCategoriesAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Получить остатки всех материалов
    /// </summary>
    [HttpGet("balances")]
    public async Task<ActionResult<List<MaterialBalanceDto>>> GetBalances([FromQuery] bool includeZeroStock = false)
    {
        var balances = await _materialService.GetAllBalancesAsync(includeZeroStock);
        return Ok(balances);
    }

    /// <summary>
    /// Получить остаток конкретного материала
    /// </summary>
    [HttpGet("{id}/balance")]
    public async Task<ActionResult<MaterialBalanceDto>> GetBalance(int id)
    {
        try
        {
            var balance = await _materialService.GetMaterialBalanceAsync(id);
            return Ok(balance);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Получить изделия, использующие материал
    /// </summary>
    [HttpGet("{id}/products")]
    public async Task<ActionResult<List<ProductListItemDto>>> GetProductsUsingMaterial(int id)
    {
        var products = await _materialService.GetProductsUsingMaterialAsync(id);
        return Ok(products);
    }
}
