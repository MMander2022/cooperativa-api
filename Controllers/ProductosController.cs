using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CooperativaApp.Models; // Asegúrate de que apunte a tus modelos
using CooperativaApp.DTOS;   // Asegúrate de que apunte a tus DTOS
using CooperativaApp.Data;   // Tu DbContext

namespace CooperativaApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly CooperativaContext _context;

        public ProductosController(CooperativaContext context)
        {
            _context = context;
        }

        // GET: api/Productos
        // Este lo usaremos para llenar el Select de la pantalla de Créditos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoDTO>>> GetProductos()
        {
            var productos = await _context.Productos
        .Include(p => p.Tasas) // 👈 ESTO ES VITAL para que vengan los rangos
        .Where(p => p.Estado == true)
        .Select(p => new ProductoDTO
        {
            Id = p.Id,
            Nombre = p.Nombre,
            TipoAmortizacion = p.CalculoCuota,
            TasaReferencial = p.TasaReferencial ?? 0,
            // Mapeamos los rangos al DTO
            Rangos = p.Tasas.Select(t => new TasaRangoDTO
            {
                Min = t.MontoMinimo,
                Max = t.MontoMaximo,
                Tasa = t.TasaInteres
            }).ToList()
        })
        .ToListAsync();

            return Ok(productos);
        }

        // GET: api/Productos/{id}/tasa-sugerida/{monto}
        // El "Cerebro" que busca el rango de tasa
        [HttpGet("{id}/tasa-sugerida/{monto}")]
        public async Task<ActionResult<decimal>> GetTasaSugerida(int id, decimal monto)
        {
            // Buscamos si existe un rango configurado para ese monto
            var tasaRango = await _context.ProductoTasas
                .Where(t => t.ProductoId == id &&
                            monto >= t.MontoMinimo &&
                            monto <= t.MontoMaximo)
                .Select(t => t.TasaInteres)
                .FirstOrDefaultAsync();

            // Si no hay rango (tasaRango == 0), devolvemos la tasa base del producto
            if (tasaRango == 0)
            {
                var producto = await _context.Productos
                    .Where(p => p.Id == id)
                    .Select(p => p.TasaReferencial)
                    .FirstOrDefaultAsync();

                return Ok(new { tasa = producto ?? 0 });
            }

            return Ok(new { tasa = tasaRango });
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductoDTO dto)
        {
            // Validación de DTO básico
            if (dto == null) return BadRequest("Los datos del producto son obligatorios.");

            // VALIDACIÓN SENIOR: Traslape de rangos
            var (isValid, message) = ValidarTraslapeRangos(dto.Rangos);
            if (!isValid) return BadRequest(new { error = message });
            int analistaId = GetCurrentUserId();
            var producto = new Producto
            {
                Nombre = dto.Nombre,
                CalculoCuota = dto.TipoAmortizacion,
                TasaReferencial = dto.TasaReferencial,
                Estado = true,
                Tasas = new List<ProductoTasa>(), // Asegúrate de inicializar la lista
                UsuarioId= analistaId
            };

            foreach (var r in dto.Rangos)
            {
                producto.Tasas.Add(new ProductoTasa
                {
                    MontoMinimo = r.Min,
                    MontoMaximo = r.Max,
                    TasaInteres = r.Tasa,
                    UsuarioId = analistaId // Ajustar según tu auth
                });
            }

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
            return Ok(new { id = producto.Id, mensaje = "Producto creado con éxito" });
        }

        // DELETE: api/Productos/5 (Desactivación lógica)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            producto.Estado = false; // Desactivación lógica
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/Productos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody]  ProductoDTO dto)
        {

            var (isValid, message) = ValidarTraslapeRangos(dto.Rangos);
            if (!isValid) return BadRequest(new { error = message });
            if (id != dto.Id) return BadRequest();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var producto = await _context.Productos
                    .Include(p => p.Tasas)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (producto == null) return NotFound();

                // 1. Actualizar datos maestros
                producto.Nombre = dto.Nombre;
                producto.CalculoCuota = dto.TipoAmortizacion;
                producto.TasaReferencial = dto.TasaReferencial;
                int analistaId = GetCurrentUserId();
                // 2. Eliminar rangos anteriores (Clear)
                _context.ProductoTasas.RemoveRange(producto.Tasas);

                // 3. Insertar nuevos rangos (Sync)
                foreach (var r in dto.Rangos)
                {
                    producto.Tasas.Add(new ProductoTasa
                    {
                        ProductoId = id,
                        MontoMinimo = r.Min,
                        MontoMaximo = r.Max,
                        TasaInteres = r.Tasa,
                        UsuarioId = analistaId// Idealmente obtener del JWT
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
        private (bool isValid, string message) ValidarTraslapeRangos(List<TasaRangoDTO> rangos)
        {
            if (rangos == null || !rangos.Any()) return (true, "");

            // 1. Ordenar por monto mínimo para facilitar la comparación
            var rangosOrdenados = rangos.OrderBy(r => r.Min).ToList();

            for (int i = 0; i < rangosOrdenados.Count; i++)
            {
                var actual = rangosOrdenados[i];

                // Validar que el rango individual sea coherente
                if (actual.Min >= actual.Max)
                    return (false, $"En el rango {i + 1}, el monto mínimo ({actual.Min}) no puede ser mayor o igual al máximo ({actual.Max}).");

                // Comparar con el siguiente rango
                if (i < rangosOrdenados.Count - 1)
                {
                    var siguiente = rangosOrdenados[i + 1];
                    if (actual.Max >= siguiente.Min)
                    {
                        return (false, $"Existe un traslape: El rango {i + 1} termina en {actual.Max}, pero el siguiente inicia en {siguiente.Min}.");
                    }
                }
            }
            return (true, "");
        }
        private int GetCurrentUserId()
        {
            // Busca el claim "IdUsuario" o el estándar NameIdentifier del JWT
            var claim = User.FindFirst("IdUsuario") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int userId) ? userId : 0;
        }
    }
}