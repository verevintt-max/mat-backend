using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopApi.DTOs;
using WorkshopApi.Services;

namespace WorkshopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaterialsController : BaseApiController
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
        var validation = ValidateOrganizationContext();
        if (validation != null) return validation;

        var materials = await _materialService.GetAllAsync(OrganizationId!.Value, search, category, includeArchived);
        return Ok(materials);
    }

    /// <summary>
    /// Получить материал по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MaterialResponseDto>> GetById(int id)
    {
        var validation = ValidateOrganizationContext();
        if (validation != null) return validation;

        var material = await _materialService.GetByIdAsync(OrganizationId!.Value, id);
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
            var validation = ValidateOrganizationContext();
            if (validation != null) return validation;

            var ctx = new OrganizationContext
            {
                UserId = UserId!.Value,
                OrganizationId = OrganizationId!.Value,
                Role = OrganizationRole ?? "Member"
            };

            var material = await _materialService.CreateAsync(ctx, dto);
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
            var validation = ValidateOrganizationContext();
            if (validation != null) return validation;

            var ctx = new OrganizationContext
            {
                UserId = UserId!.Value,
                OrganizationId = OrganizationId!.Value,
                Role = OrganizationRole ?? "Member"
            };

            var material = await _materialService.UpdateAsync(ctx, id, dto);
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
            var validation = ValidateOrganizationContext();
            if (validation != null) return validation;

            var ctx = new OrganizationContext
            {
                UserId = UserId!.Value,
                OrganizationId = OrganizationId!.Value,
                Role = OrganizationRole ?? "Member"
            };

            var result = await _materialService.DeleteAsync(ctx, id);
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
    public async Task<ActionResult> Archive(int id)
    {
        var validation = ValidateOrganizationContext();
        if (validation != null) return validation;

        var ctx = new OrganizationContext
        {
            UserId = UserId!.Value,
            OrganizationId = OrganizationId!.Value,
            Role = OrganizationRole ?? "Member"
        };

        var result = await _materialService.ArchiveAsync(ctx, id);
        if (!result)
            return NotFound(new { message = $"Материал с ID {id} не найден" });

        return Ok(new { message = "Материал архивирован" });
    }

    /// <summary>
    /// Разархивировать материал
    /// </summary>
    [HttpPost("{id}/unarchive")]
    public async Task<ActionResult> Unarchive(int id)
    {
        var validation = ValidateOrganizationContext();
        if (validation != null) return validation;

        var ctx = new OrganizationContext
        {
            UserId = UserId!.Value,
            OrganizationId = OrganizationId!.Value,
            Role = OrganizationRole ?? "Member"
        };

        var result = await _materialService.UnarchiveAsync(ctx, id);
        if (!result)
            return NotFound(new { message = $"Материал с ID {id} не найден" });

        return Ok(new { message = "Материал разархивирован" });
    }

    /// <summary>
    /// Получить список категорий
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var validation = ValidateOrganizationContext();
        if (validation != null) return validation;

        var categories = await _materialService.GetCategoriesAsync(OrganizationId!.Value);
        return Ok(categories);
    }

    /// <summary>
    /// Получить остатки всех материалов
    /// </summary>
    [HttpGet("balances")]
    public async Task<ActionResult<List<MaterialBalanceDto>>> GetBalances([FromQuery] bool includeZeroStock = false)
    {
        var validation = ValidateOrganizationContext();
        if (validation != null) return validation;

        var balances = await _materialService.GetAllBalancesAsync(OrganizationId!.Value, includeZeroStock);
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
            var validation = ValidateOrganizationContext();
            if (validation != null) return validation;

            var balance = await _materialService.GetMaterialBalanceAsync(OrganizationId!.Value, id);
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
        var validation = ValidateOrganizationContext();
        if (validation != null) return validation;

        var products = await _materialService.GetProductsUsingMaterialAsync(OrganizationId!.Value, id);
        return Ok(products);
    }
}
