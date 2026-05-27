using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBiometricoUMG.Models;
using SistemaBiometricoUMG.Servicios;

namespace SistemaBiometricoUMG.Controllers
{
    [Authorize]
    public class PuertaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ReconocimientoFacialService _reconocimiento;

        public PuertaController(ApplicationDbContext context,
                                 ReconocimientoFacialService reconocimiento)
        {
            _context = context;
            _reconocimiento = reconocimiento;
        }

        // ── Página principal de puerta ───────────────────────────────
        public async Task<IActionResult> Index()
        {
            // Obtener la ubicación de puerta principal
            var puerta = await _context.Ubicaciones
                .FirstOrDefaultAsync(u => u.Tipo == "puerta");

            // Registros de hoy
            var registrosHoy = await _context.RegistrosIngreso
                .Include(r => r.Persona)
                .Where(r => r.UbicacionId == puerta.Id &&
                            r.FechaHora.Date == DateTime.Today)
                .OrderByDescending(r => r.FechaHora)
                .Take(20)
                .ToListAsync();

            ViewBag.Puerta = puerta;
            ViewBag.RegistrosHoy = registrosHoy;
            ViewBag.TotalEntradas = registrosHoy
                .Count(r => r.TipoMovimiento == "Entrada");
            ViewBag.TotalSalidas = registrosHoy
                .Count(r => r.TipoMovimiento == "Salida");

            return View();
        }

        // ── Procesar reconocimiento facial ───────────────────────────
        [HttpPost]
        public async Task<IActionResult> Procesar(
            [FromBody] ProcesarPuertaRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.ImagenBase64))
                    return Json(new
                    {
                        encontrado = false,
                        mensaje = "No se recibió imagen"
                    });

                var base64 = request.ImagenBase64.Replace(
                    "data:image/png;base64,", "");
                var bytes = Convert.FromBase64String(base64);

                var resultado = await _reconocimiento.ReconocerPersona(bytes);

                if (resultado.Encontrado && !resultado.TieneRestriccion)
                {
                    var puerta = await _context.Ubicaciones
                        .FirstOrDefaultAsync(u => u.Tipo == "puerta");

                    _context.RegistrosIngreso.Add(new RegistroIngreso
                    {
                        PersonaId = resultado.Persona.Id,
                        UbicacionId = puerta.Id,
                        FechaHora = DateTime.Now,
                        TipoMovimiento = request.TipoMovimiento
                    });

                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    encontrado = resultado.Encontrado,
                    mensaje = resultado.Mensaje,
                    nombre = resultado.Persona != null
                        ? $"{resultado.Persona.Nombre} " +
                          $"{resultado.Persona.Apellido}"
                        : "",
                    foto = resultado.Persona?.FotoRuta ?? "",
                    tieneRestriccion = resultado.TieneRestriccion,
                    motivoRestriccion = resultado.MotivoRestriccion ?? "",
                    tipo = request.TipoMovimiento
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    encontrado = false,
                    mensaje = "Error: " + ex.Message
                });
            }
        }

        // ── Registrar salida manual ──────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> RegistrarSalida(int personaId)
        {
            try
            {
                var puerta = await _context.Ubicaciones
                    .FirstOrDefaultAsync(u => u.Tipo == "puerta");

                _context.RegistrosIngreso.Add(new RegistroIngreso
                {
                    PersonaId = personaId,
                    UbicacionId = puerta.Id,
                    FechaHora = DateTime.Now,
                    TipoMovimiento = "Salida"
                });

                await _context.SaveChangesAsync();
                TempData["Exito"] = "✅ Salida registrada correctamente";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Index");
        }
    }

    public class ProcesarPuertaRequest
    {
        public string ImagenBase64 { get; set; }
        public string TipoMovimiento { get; set; } = "Entrada";
    }
}
