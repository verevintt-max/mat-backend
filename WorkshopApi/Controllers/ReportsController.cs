using Microsoft.AspNetCore.Mvc;
using WorkshopApi.DTOs;
using WorkshopApi.Services;

namespace WorkshopApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Получить данные для дашборда
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var dashboard = await _reportService.GetDashboardAsync();
        return Ok(dashboard);
    }

    /// <summary>
    /// Отчет о движении материала
    /// </summary>
    [HttpGet("material-movement/{materialId}")]
    public async Task<ActionResult<MaterialMovementReportDto>> GetMaterialMovement(
        int materialId,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        try
        {
            var report = await _reportService.GetMaterialMovementReportAsync(materialId, dateFrom, dateTo);
            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Отчет о производстве за период
    /// </summary>
    [HttpGet("production")]
    public async Task<ActionResult<ProductionReportDto>> GetProductionReport(
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo)
    {
        var report = await _reportService.GetProductionReportAsync(dateFrom, dateTo);
        return Ok(report);
    }

    /// <summary>
    /// Отчет о продажах за период
    /// </summary>
    [HttpGet("sales")]
    public async Task<ActionResult<SalesReportDto>> GetSalesReport(
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo)
    {
        var report = await _reportService.GetSalesReportAsync(dateFrom, dateTo);
        return Ok(report);
    }

    /// <summary>
    /// Финансовая сводка
    /// </summary>
    [HttpGet("financial-summary")]
    public async Task<ActionResult<FinancialSummaryDto>> GetFinancialSummary(
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo)
    {
        var report = await _reportService.GetFinancialSummaryAsync(dateFrom, dateTo);
        return Ok(report);
    }
}
