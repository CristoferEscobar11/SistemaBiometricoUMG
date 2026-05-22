using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBiometricoUMG.Models;

namespace SistemaBiometricoUMG.Controllers
{
    [Authorize]
    public class CarrerasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CarrerasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Historial de carreras ────────────────────────────────────
        public async Task<IActionResult> Historial(int personaId)
        {
            var persona = await _context.Personas.FindAsync(personaId);
            if (persona == null) return NotFound();

            var historial = await _context.HistorialCarreras
                .Where(h => h.PersonaId == personaId)
                .OrderByDescending(h => h.FechaInicio)
                .ToListAsync();

            ViewBag.Persona = persona;
            return View(historial);
        }

        // ── Mostrar formulario agregar ───────────────────────────────
        public async Task<IActionResult> Agregar(int personaId)
        {
            var persona = await _context.Personas.FindAsync(personaId);
            if (persona == null) return NotFound();
            ViewBag.Persona = persona;
            return View();
        }

        // ── Guardar nueva carrera ────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Agregar(int personaId,
                                                   string carrera,
                                                   string seccion,
                                                   string motivo,
                                                   bool cerrarAnterior)
        {
            try
            {
                if (cerrarAnterior)
                {
                    var anteriores = await _context.HistorialCarreras
                        .Where(h => h.PersonaId == personaId && h.Activo)
                        .ToListAsync();

                    foreach (var anterior in anteriores)
                    {
                        anterior.Activo = false;
                        anterior.FechaFin = DateOnly.FromDateTime(DateTime.Today);
                    }
                }

                _context.HistorialCarreras.Add(new HistorialCarrera
                {
                    PersonaId = personaId,
                    Carrera = carrera,
                    Seccion = seccion,
                    Motivo = motivo,
                    FechaInicio = DateOnly.FromDateTime(DateTime.Today),
                    Activo = true
                });

                var persona = await _context.Personas.FindAsync(personaId);
                if (persona != null)
                {
                    persona.Carrera = carrera;
                    persona.Seccion = seccion;
                }

                await _context.SaveChangesAsync();
                TempData["Exito"] = "✅ Carrera actualizada correctamente";
                return RedirectToAction("Historial", new { personaId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("Agregar", new { personaId });
            }
        }

        // ── Desactivar carrera ───────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Desactivar(int historialId,
                                                      int personaId)
        {
            try
            {
                var historial = await _context.HistorialCarreras
                    .FindAsync(historialId);

                if (historial != null)
                {
                    historial.Activo = false;
                    historial.FechaFin = DateOnly.FromDateTime(DateTime.Today);
                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "✅ Carrera desactivada correctamente";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Historial", new { personaId });
        }

        // ── Reactivar carrera ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Reactivar(int historialId,
                                                     int personaId)
        {
            try
            {
                var historial = await _context.HistorialCarreras
                    .FindAsync(historialId);

                if (historial != null)
                {
                    historial.Activo = true;
                    historial.FechaFin = null;
                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "✅ Carrera reactivada correctamente";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Historial", new { personaId });
        }
    }
}
