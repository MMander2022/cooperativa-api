using CooperativaApp.Models;
using CooperativaApp.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.EntityFrameworkCore;
namespace CooperativaApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CuotaController : ControllerBase
    {
        private readonly CooperativaContext _context;

        public CuotaController(CooperativaContext context)
        {
            _context = context;
        }

        // GET: api/cuota
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cuota>>> Get()
        {
            return await _context.Cuotas.ToListAsync();
        }

        // POST: api/cuota
        [HttpPost]
        public async Task<ActionResult<Cuota>> Post(Cuota cuota)
        {
            _context.Cuotas.Add(cuota);
            await _context.SaveChangesAsync();

            return Ok(cuota);
        }
    }
}
