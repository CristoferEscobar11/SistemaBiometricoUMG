using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBiometricoUMG.Models;

namespace SistemaBiometricoUMG.Controllers
{
    [Authorize]
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CursosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Listar cursos ────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var cursos = await _context.Cursos
                .Include(c => c.Ubicacion)
                .Where(c => c.Activo)
                .OrderBy(c => c.UbicacionId)
                .ToListAsync();

            return View(cursos);
        }

        // ── Crear curso ──────────────────────────────────────────────
        public async Task<IActionResult> Crear()
        {
            var ubicaciones = await _context.Ubicaciones
                .Where(u => u.Tipo == "salon")
                .ToListAsync();
            ViewBag.Ubicaciones = ubicaciones;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Crear(Curso curso)
        {
            try
            {
                _context.Cursos.Add(curso);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "✅ Curso creado correctamente";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("Crear");
            }
        }

        // ── Desactivar curso ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Desactivar(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso != null)
            {
                curso.Activo = false;
                await _context.SaveChangesAsync();
                TempData["Exito"] = "✅ Curso desactivado";
            }
            return RedirectToAction("Index");
        }

        // ── Ver estudiantes inscritos en un curso ────────────────────
        public async Task<IActionResult> Estudiantes(int cursoId)
        {
            var curso = await _context.Cursos
                .Include(c => c.Ubicacion)
                .FirstOrDefaultAsync(c => c.Id == cursoId);

            if (curso == null) return NotFound();

            var inscritos = await _context.Inscripciones
                .Include(i => i.Persona)
                .Where(i => i.CursoId == cursoId && i.Activo)
                .ToListAsync();

            var todosEstudiantes = await _context.Personas
                .Where(p => p.TipoPersona == "Estudiante" && p.Activo)
                .ToListAsync();

            ViewBag.Curso = curso;
            ViewBag.TodosEstudiantes = todosEstudiantes;
            return View(inscritos);
        }

        // ── Inscribir estudiante a curso ─────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Inscribir(int cursoId, int personaId)
        {
            try
            {
                var yaInscrito = await _context.Inscripciones
                    .AnyAsync(i => i.CursoId == cursoId &&
                                   i.PersonaId == personaId && i.Activo);

                if (yaInscrito)
                {
                    TempData["Error"] = "⚠️ El estudiante ya está inscrito";
                    return RedirectToAction("Estudiantes", new { cursoId });
                }

                _context.Inscripciones.Add(new Inscripcion
                {
                    CursoId = cursoId,
                    PersonaId = personaId,
                    FechaInscripcion = DateOnly.FromDateTime(DateTime.Today),
                    Activo = true
                });

                await _context.SaveChangesAsync();
                TempData["Exito"] = "✅ Estudiante inscrito correctamente";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
            }
            return RedirectToAction("Estudiantes", new { cursoId });
        }

        // ── Desinscribir estudiante ──────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Desinscribir(int inscripcionId,
                                                        int cursoId)
        {
            var inscripcion = await _context.Inscripciones
                .FindAsync(inscripcionId);

            if (inscripcion != null)
            {
                inscripcion.Activo = false;
                await _context.SaveChangesAsync();
                TempData["Exito"] = "✅ Estudiante removido del curso";
            }
            return RedirectToAction("Estudiantes", new { cursoId });
        }
    }
}