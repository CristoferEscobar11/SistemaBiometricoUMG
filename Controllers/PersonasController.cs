using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaBiometricoUMG.Models;
using SistemaBiometricoUMG.Servicios;

namespace SistemaBiometricoUMG.Controllers
{
    [Authorize]
    public class PersonasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly CarnetService _carnetService;
        private readonly CorreoService _correoService;

        public PersonasController(ApplicationDbContext context,
                                  IWebHostEnvironment env,
                                  CarnetService carnetService,
                                  CorreoService correoService)
        {
            _context = context;
            _env = env;
            _carnetService = carnetService;
            _correoService = correoService;
        }

        public IActionResult Registrar() => View();

        [HttpPost]
        public async Task<IActionResult> Registrar(Persona persona, string fotoBase64)
        {
            try
            {
                // Generar número de carnet único
                persona.NumCarnet = "UMG" + DateTime.Now.Ticks.ToString().Substring(10);

                // Guardar foto
                if (!string.IsNullOrEmpty(fotoBase64))
                {
                    var fotoData = fotoBase64.Replace("data:image/png;base64,", "");
                    var bytes = Convert.FromBase64String(fotoData);
                    var carpeta = Path.Combine(_env.WebRootPath, "fotos");
                    if (!Directory.Exists(carpeta))
                        Directory.CreateDirectory(carpeta);
                    var rutaFoto = Path.Combine(carpeta, $"{persona.NumCarnet}.png");
                    await System.IO.File.WriteAllBytesAsync(rutaFoto, bytes);
                    persona.FotoRuta = $"/fotos/{persona.NumCarnet}.png";
                }

                // Guardar en base de datos
                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                // Generar QR
                _carnetService.GenerarQR(persona.NumCarnet, persona.NumCarnet);

                // Generar PDF y enviar por correo
                var pdfBytes = _carnetService.GenerarCarnetPDF(persona);
                await _correoService.EnviarCarnet(
                    persona.Correo,
                    $"{persona.Nombre} {persona.Apellido}",
                    pdfBytes
                );

                TempData["Exito"] = "✅ Persona registrada y carnet enviado al correo";
                return RedirectToAction("Detalle", new { id = persona.Id });
            }
            catch (Exception ex)
            {
                var mensajeCompleto = ex.Message;
                if (ex.InnerException != null)
                    mensajeCompleto += " | DETALLE: " + ex.InnerException.Message;
                if (ex.InnerException?.InnerException != null)
                    mensajeCompleto += " | " + ex.InnerException.InnerException.Message;

                TempData["Error"] = "Error: " + mensajeCompleto;
                return View(persona);
            }
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var persona = await _context.Personas.FindAsync(id);
            if (persona == null) return NotFound();
            return View(persona);
        }

        public async Task<IActionResult> DescargarCarnet(int id)
        {
            var persona = await _context.Personas.FindAsync(id);
            if (persona == null) return NotFound();
            var pdfBytes = _carnetService.GenerarCarnetPDF(persona);
            return File(pdfBytes, "application/pdf",
                       $"Carnet_{persona.NumCarnet}.pdf");
        }

        public IActionResult Index()
        {
            var personas = _context.Personas.ToList();
            return View(personas);
        }
    }
}