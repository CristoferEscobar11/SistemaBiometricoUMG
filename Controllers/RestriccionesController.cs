using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBiometricoUMG.Models;

namespace SistemaBiometricoUMG.Controllers
{
    [Authorize]
    public class RestriccionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RestriccionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Lista de personas restringidas ───────────────────────────
        public async Task<IActionResult> Index()
        {
            var restricciones = await _context.Restricciones
                .Include(r => r.Persona)
                .Where(r => r.Activo)
                .OrderByDescending(r => r.FechaRegistro)
                .ToListAsync();

            return View(restricciones);
        }

        // ── Formulario para agregar restricción ──────────────────────
        public async Task<IActionResult> Agregar()
        {
            var personas = await _context.Personas
                .Where(p => p.Activo)
                .ToListAsync();

            ViewBag.Personas = personas;
            return View();
        }

        // ── Guardar restricción ──────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Agregar(int personaId, string motivo)
        {
            try
            {
                // Verificar si ya tiene restricción activa
                var yaRestringido = await _context.Restricciones
                    .AnyAsync(r => r.PersonaId == personaId && r.Activo);

                if (yaRestringido)
                {
                    TempData["Error"] = "⚠️ Esta persona ya tiene una restricción activa.";
                    return RedirectToAction("Agregar");
                }

                var restriccion = new Restriccion
                {
                    PersonaId = personaId,
                    Motivo = motivo,
                    FechaRegistro = DateTime.Now,
                    Activo = true
                };

                _context.Restricciones.Add(restriccion);
                await _context.SaveChangesAsync();

                TempData["Exito"] = "✅ Restricción registrada correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("Agregar");
            }
        }

        // ── Quitar restricción ───────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Quitar(int id)
        {
            var restriccion = await _context.Restricciones.FindAsync(id);
            if (restriccion != null)
            {
                restriccion.Activo = false;
                await _context.SaveChangesAsync();
                TempData["Exito"] = "✅ Restricción eliminada correctamente.";
            }
            return RedirectToAction("Index");
        }

        // ── Verificar si persona tiene restricción ───────────────────
        public async Task<IActionResult> Verificar(int personaId)
        {
            var persona = await _context.Personas.FindAsync(personaId);
            if (persona == null)
                return Json(new { restringido = false });

            var restriccion = await _context.Restricciones
                .Where(r => r.PersonaId == personaId && r.Activo)
                .FirstOrDefaultAsync();

            return Json(new
            {
                restringido = restriccion != null,
                motivo = restriccion?.Motivo,
                nombre = $"{persona.Nombre} {persona.Apellido}"
            });
        }
    }
}