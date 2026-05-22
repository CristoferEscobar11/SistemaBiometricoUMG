using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaBiometricoUMG.Models;
using SistemaBiometricoUMG.Servicios;

namespace SistemaBiometricoUMG.Controllers
{
    // ── Clase para recibir los datos del fetch ───────────────────────
    public class ProcesarRequest
    {
        public string ImagenBase64 { get; set; }
        public int UbicacionId { get; set; }
    }
    [Authorize]
    public class ReconocimientoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ReconocimientoFacialService _reconocimiento;

        public ReconocimientoController(ApplicationDbContext context,
                                        ReconocimientoFacialService reconocimiento)
        {
            _context = context;
            _reconocimiento = reconocimiento;
        }

        // ── Página principal de reconocimiento ───────────────────────
        public IActionResult Index(int ubicacionId = 1)
        {
            var ubicaciones = _context.Ubicaciones.ToList();
            ViewBag.Ubicaciones = ubicaciones;
            ViewBag.UbicacionId = ubicacionId;
            return View();
        }

        // ── Procesar imagen de la cámara ─────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Procesar([FromBody] ProcesarRequest request)
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
                    // Registrar ingreso
                    _context.RegistrosIngreso.Add(new RegistroIngreso
                    {
                        PersonaId = resultado.Persona.Id,
                        UbicacionId = request.UbicacionId,
                        FechaHora = DateTime.Now
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
                    puntaje = resultado.Puntaje
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
    }
}