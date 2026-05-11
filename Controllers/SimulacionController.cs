using CooperativaApp.DTOs;
using CooperativaApp.DTOS;
using CooperativaApp.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SimulacionController : ControllerBase
{
    private readonly CreditoService _creditoService;

    public SimulacionController(CreditoService creditoService)
    {
        _creditoService = creditoService;
    }

    [HttpPost]
    public async Task<ActionResult<SimulacionResponseDTO>> Simular([FromBody] SimulacionRequestDTO request)
    {
        try
        {
            var resultado = await _creditoService.SimularCreditoAsync(request);
            return Ok(resultado);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}