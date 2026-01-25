using Microsoft.AspNetCore.Mvc;
using WorkshopApi.DTOs;
using WorkshopApi.Services;

namespace WorkshopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Получить список всех изделий
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ProductListItemDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] bool includeArchived = false)
    {
        var products = await _productService.GetAllAsync(search, category, includeArchived);
        return Ok(products);
    }

    /// <summary>
    /// Получить изделие по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponseDto>> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
            return NotFound(new { message = $"Изделие с ID {id} не найдено" });

        return Ok(product);
    }

    /// <summary>
    /// Создать новое изделие
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductResponseDto>> Create([FromBody] ProductCreateDto dto)
    {
        try
        {
            var product = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Обновить изделие
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ProductResponseDto>> Update(int id, [FromBody] ProductUpdateDto dto)
    {
        try
        {
            var product = await _productService.UpdateAsync(id, dto);
            if (product == null)
                return NotFound(new { message = $"Изделие с ID {id} не найдено" });

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Удалить изделие
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var result = await _productService.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = $"Изделие с ID {id} не найдено" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Копировать изделие
    /// </summary>
    [HttpPost("{id}/copy")]
    public async Task<ActionResult<ProductResponseDto>> Copy(int id, [FromBody] ProductCopyDto dto)
    {
        try
        {
            var product = await _productService.CopyAsync(id, dto);
            if (product == null)
                return NotFound(new { message = $"Изделие с ID {id} не найдено" });

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Архивировать изделие
    /// </summary>
    [HttpPost("{id}/archive")]
    public async Task<ActionResult<ProductResponseDto>> Archive(int id)
    {
        var product = await _productService.UpdateAsync(id, new ProductUpdateDto { IsArchived = true });
        if (product == null)
            return NotFound(new { message = $"Изделие с ID {id} не найдено" });

        return Ok(product);
    }

    /// <summary>
    /// Разархивировать изделие
    /// </summary>
    [HttpPost("{id}/unarchive")]
    public async Task<ActionResult<ProductResponseDto>> Unarchive(int id)
    {
        var product = await _productService.UpdateAsync(id, new ProductUpdateDto { IsArchived = false });
        if (product == null)
            return NotFound(new { message = $"Изделие с ID {id} не найдено" });

        return Ok(product);
    }

    /// <summary>
    /// Получить список категорий
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var categories = await _productService.GetCategoriesAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Пересчитать вес изделия на основе материалов
    /// </summary>
    [HttpPost("{id}/recalculate-weight")]
    public async Task<ActionResult<ProductResponseDto>> RecalculateWeight(int id)
    {
        await _productService.RecalculateWeightAsync(id);
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
            return NotFound(new { message = $"Изделие с ID {id} не найдено" });

        return Ok(product);
    }
}
