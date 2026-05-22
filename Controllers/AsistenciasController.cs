using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBiometricoUMG.Models;
using SistemaBiometricoUMG.Servicios;

namespace SistemaBiometricoUMG.Controllers
{
    [Authorize]
    public class AsistenciasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CarnetService _carnetService;
        private readonly CorreoService _correoService;

        public AsistenciasController(ApplicationDbContext context,
                                     CarnetService carnetService,
                                     CorreoService correoService)
        {
            _context = context;
            _carnetService = carnetService;
            _correoService = correoService;
        }

        // ── Panel del catedrático ────────────────────────────────────
        public async Task<IActionResult> Panel(int ubicacionId = 1)
        {
            var ubicaciones = await _context.Ubicaciones
                .Where(u => u.Tipo == "salon")
                .ToListAsync();

            var estudiantes = await _context.Personas
                .Where(p => p.TipoPersona == "Estudiante" && p.Activo)
                .ToListAsync();

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var ingresos = await _context.RegistrosIngreso
                .Where(r => r.UbicacionId == ubicacionId &&
                            r.FechaHora.Date == DateTime.Today)
                .Select(r => r.PersonaId)
                .ToListAsync();

            var yaConfirmado = await _context.Asistencias
                .AnyAsync(a => a.UbicacionId == ubicacionId &&
                               a.Fecha == hoy &&
                               a.Confirmado);

            var vm = new AsistenciaPanelViewModel
            {
                Ubicaciones = ubicaciones,
                UbicacionSeleccionada = ubicacionId,
                Estudiantes = estudiantes,
                PersonasPresentes = ingresos,
                YaConfirmado = yaConfirmado,
                Fecha = DateTime.Today
            };

            return View(vm);
        }

        // ── Confirmar asistencia ─────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Confirmar(int ubicacionId, int catedraticoid)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var yaConfirmado = await _context.Asistencias
                .AnyAsync(a => a.UbicacionId == ubicacionId &&
                               a.Fecha == hoy && a.Confirmado);

            if (yaConfirmado)
            {
                TempData["Error"] = "La asistencia ya fue confirmada hoy.";
                return RedirectToAction("Panel", new { ubicacionId });
            }

            var estudiantes = await _context.Personas
                .Where(p => p.TipoPersona == "Estudiante" && p.Activo)
                .ToListAsync();

            var ingresos = await _context.RegistrosIngreso
                .Where(r => r.UbicacionId == ubicacionId &&
                            r.FechaHora.Date == DateTime.Today)
                .Select(r => r.PersonaId)
                .ToListAsync();

            foreach (var estudiante in estudiantes)
            {
                _context.Asistencias.Add(new Asistencia
                {
                    PersonaId = estudiante.Id,
                    UbicacionId = ubicacionId,
                    Fecha = hoy,
                    Presente = ingresos.Contains(estudiante.Id),
                    Confirmado = true,
                    ConfirmadoPor = null
                });
            }

            await _context.SaveChangesAsync();

            var ubicacion = await _context.Ubicaciones.FindAsync(ubicacionId);
            var catedratico = await _context.Personas.FindAsync(catedraticoid);
            var pdfBytes = GenerarPDFAsistencia(estudiantes, ingresos,
                                                ubicacion, hoy);

            if (catedratico != null)
            {
                await _correoService.EnviarCarnet(
                    catedratico.Correo,
                    $"{catedratico.Nombre} {catedratico.Apellido}",
                    pdfBytes
                );
            }

            TempData["Exito"] = "✅ Asistencia confirmada correctamente";
            return RedirectToAction("Panel", new { ubicacionId });
        }

        // ── Generar PDF de asistencia ────────────────────────────────
        private byte[] GenerarPDFAsistencia(List<Persona> estudiantes,
                                             List<int> presentes,
                                             Ubicacion ubicacion,
                                             DateOnly fecha)
        {
            using var ms = new MemoryStream();
            var writer = new iText.Kernel.Pdf.PdfWriter(ms);
            var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
            var doc = new iText.Layout.Document(pdf);

            var azul = new iText.Kernel.Colors.DeviceRgb(0, 51, 102);

            var header = new iText.Layout.Element.Table(1)
                .SetWidth(iText.Layout.Properties.UnitValue
                .CreatePercentValue(100));

            var parrafoTitulo = new iText.Layout.Element.Paragraph(
                "UNIVERSIDAD MARIANO GÁLVEZ");
            parrafoTitulo.SetFontColor(
                iText.Kernel.Colors.ColorConstants.WHITE);
            parrafoTitulo.SetFontSize(16);
            parrafoTitulo.SetFont(
                iText.Kernel.Font.PdfFontFactory.CreateFont(
                iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD));

            var parrafoSalon = new iText.Layout.Element.Paragraph(
                $"Lista de Asistencia — {ubicacion?.Nombre}");
            parrafoSalon.SetFontColor(
                iText.Kernel.Colors.ColorConstants.WHITE);
            parrafoSalon.SetFontSize(11);

            var parrafoFecha = new iText.Layout.Element.Paragraph(
                $"Fecha: {fecha.ToString("dd/MM/yyyy")}");
            parrafoFecha.SetFontColor(
                iText.Kernel.Colors.ColorConstants.WHITE);
            parrafoFecha.SetFontSize(10);

            var celdaHeader = new iText.Layout.Element.Cell();
            celdaHeader.SetBackgroundColor(azul);
            celdaHeader.SetPadding(10);
            celdaHeader.SetTextAlignment(
                iText.Layout.Properties.TextAlignment.CENTER);
            celdaHeader.Add(parrafoTitulo);
            celdaHeader.Add(parrafoSalon);
            celdaHeader.Add(parrafoFecha);
            header.AddCell(celdaHeader);
            doc.Add(header);

            var tabla = new iText.Layout.Element.Table(
                new float[] { 1, 3, 3, 2 })
                .SetWidth(iText.Layout.Properties.UnitValue
                .CreatePercentValue(100))
                .SetMarginTop(15);

            var colorHeader = new iText.Kernel.Colors.DeviceRgb(200, 200, 200);
            var columnas = new string[] { "#", "Nombre", "Correo", "Asistencia" };
            for (int i = 0; i < columnas.Length; i++)
            {
                var parrafo = new iText.Layout.Element.Paragraph(columnas[i]);
                parrafo.SetFont(iText.Kernel.Font.PdfFontFactory.CreateFont(
                    iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD));
                var celda = new iText.Layout.Element.Cell();
                celda.SetBackgroundColor(colorHeader);
                celda.Add(parrafo);
                tabla.AddHeaderCell(celda);
            }

            int num = 1;
            foreach (var e in estudiantes)
            {
                bool presente = presentes.Contains(e.Id);
                var colorFila = presente
                    ? new iText.Kernel.Colors.DeviceRgb(198, 239, 206)
                    : new iText.Kernel.Colors.DeviceRgb(255, 199, 206);

                tabla.AddCell(new iText.Layout.Element.Cell()
                    .SetBackgroundColor(colorFila)
                    .Add(new iText.Layout.Element.Paragraph(num++.ToString())));
                tabla.AddCell(new iText.Layout.Element.Cell()
                    .SetBackgroundColor(colorFila)
                    .Add(new iText.Layout.Element.Paragraph(
                        $"{e.Nombre} {e.Apellido}")));
                tabla.AddCell(new iText.Layout.Element.Cell()
                    .SetBackgroundColor(colorFila)
                    .Add(new iText.Layout.Element.Paragraph(e.Correo)));
                tabla.AddCell(new iText.Layout.Element.Cell()
                    .SetBackgroundColor(colorFila)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .Add(new iText.Layout.Element.Paragraph(
                        presente ? "✅ Presente" : "❌ Ausente")));
            }

            doc.Add(tabla);

            doc.Add(new iText.Layout.Element.Paragraph(
                $"\nTotal presentes: {presentes.Count} | " +
                $"Total ausentes: {estudiantes.Count - presentes.Count} | " +
                $"Total estudiantes: {estudiantes.Count}")
                .SetFontSize(10)
                .SetFont(iText.Kernel.Font.PdfFontFactory.CreateFont(
                    iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD))
                .SetMarginTop(10));

            doc.Close();
            return ms.ToArray();
        }

        // ── Historial de asistencias ─────────────────────────────────
        public async Task<IActionResult> Historial(int ubicacionId = 1)
        {
            var asistencias = await _context.Asistencias
                .Include(a => a.Persona)
                .Include(a => a.Ubicacion)
                .Where(a => a.UbicacionId == ubicacionId)
                .OrderByDescending(a => a.Fecha)
                .ToListAsync();

            var ubicaciones = await _context.Ubicaciones
                .Where(u => u.Tipo == "salon")
                .ToListAsync();

            ViewBag.Ubicaciones = ubicaciones;
            ViewBag.UbicacionSeleccionada = ubicacionId;

            return View(asistencias);
        }

        // ── Árbol jerárquico ─────────────────────────────────────────
        public async Task<IActionResult> Arbol()
        {
            var ubicaciones = await _context.Ubicaciones
                .Where(u => u.Tipo == "salon")
                .ToListAsync();

            var personas = await _context.Personas
                .Where(p => p.TipoPersona == "Estudiante" && p.Activo)
                .ToListAsync();

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            // ✅ Ahora lee de Asistencias en lugar de RegistrosIngreso
            var asistencias = await _context.Asistencias
                .Where(a => a.Fecha == hoy)
                .ToListAsync();

            var estudiantesPorSalon = new Dictionary<int, List<dynamic>>();
            foreach (var ub in ubicaciones)
            {
                var lista = personas.Select(p => (dynamic)new
                {
                    p.Id,
                    Nombre = $"{p.Nombre} {p.Apellido}",
                    Correo = p.Correo ?? "",
                    FotoRuta = p.FotoRuta ?? "",
                    Presente = asistencias.Any(a => a.PersonaId == p.Id &&
                                                    a.UbicacionId == ub.Id &&
                                                    a.Presente)
                }).ToList();

                estudiantesPorSalon[ub.Id] = lista;
            }

            ViewBag.Nivel1 = ubicaciones.Where(u => u.Nivel == 1).ToList();
            ViewBag.Nivel2 = ubicaciones.Where(u => u.Nivel == 2).ToList();
            ViewBag.Nivel3 = ubicaciones.Where(u => u.Nivel == 3).ToList();
            ViewBag.Estudiantes = estudiantesPorSalon;

            return View();
        }
    }
}