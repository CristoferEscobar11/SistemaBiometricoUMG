using QRCoder;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using SistemaBiometricoUMG.Models;

namespace SistemaBiometricoUMG.Servicios
{
    public class CarnetService
    {
        private readonly IWebHostEnvironment _env;

        public CarnetService(IWebHostEnvironment env)
        {
            _env = env;
        }

        // ─── Genera el QR y lo guarda como imagen ───────────────────
        public string GenerarQR(string contenido, string numCarnet)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(contenido,
                         QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(10);

            var carpeta = Path.Combine(_env.WebRootPath, "qr");
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            var rutaQR = Path.Combine(carpeta, $"{numCarnet}.png");
            File.WriteAllBytes(rutaQR, qrBytes);

            return $"/qr/{numCarnet}.png";
        }

        // ─── Genera el PDF del carnet ────────────────────────────────
        public byte[] GenerarCarnetPDF(Persona persona)
        {
            using var ms = new MemoryStream();
            var writer = new PdfWriter(ms);
            var pdf = new PdfDocument(writer);
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A6.Rotate());

            // Colores
            var azulUMG = new DeviceRgb(0, 51, 102);
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            // ── Encabezado azul con logo ─────────────────────────────
            var tablaHeader = new Table(new float[] { 1, 4 })
                .SetWidth(UnitValue.CreatePercentValue(100));

            // Celda del logo
            var celdaLogo = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetBackgroundColor(azulUMG)
                .SetPadding(8)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.CENTER);

            var rutaLogo = Path.Combine(_env.WebRootPath, "img", "logo_umg.png");
            if (File.Exists(rutaLogo))
            {
                var logoData = ImageDataFactory.Create(rutaLogo);
                var logoImg = new iText.Layout.Element.Image(logoData)
                    .SetWidth(55).SetHeight(55);
                celdaLogo.Add(logoImg);
            }
            else
            {
                celdaLogo.Add(new Paragraph("UMG")
                    .SetFontColor(ColorConstants.WHITE)
                    .SetFont(boldFont)
                    .SetFontSize(14));
            }
            tablaHeader.AddCell(celdaLogo);

            // Celda del texto
            var celdaTexto = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetBackgroundColor(azulUMG)
                .SetPadding(8)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(new Paragraph("UNIVERSIDAD MARIANO GÁLVEZ")
                    .SetFontColor(ColorConstants.WHITE)
                    .SetFont(boldFont)
                    .SetFontSize(14))
                .Add(new Paragraph("Sede Boca del Monte")
                    .SetFontColor(ColorConstants.WHITE)
                    .SetFontSize(10))
                .Add(new Paragraph("Carnet de Identificación Oficial")
                    .SetFontColor(ColorConstants.WHITE)
                    .SetFontSize(9));

            tablaHeader.AddCell(celdaTexto);
            doc.Add(tablaHeader);

            // ── Cuerpo del carnet ────────────────────────────────────
            var tablaCuerpo = new Table(new float[] { 2, 3, 2 })
                .SetWidth(UnitValue.CreatePercentValue(100))
                .SetMarginTop(5);

            // Celda foto
            var celdaFoto = new Cell()
                .SetPadding(5)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);

            var rutaFotoAbsoluta = Path.Combine(_env.WebRootPath,
                                   persona.FotoRuta?.TrimStart('/') ?? "");

            if (!string.IsNullOrEmpty(persona.FotoRuta) &&
                File.Exists(rutaFotoAbsoluta))
            {
                var imgData = ImageDataFactory.Create(rutaFotoAbsoluta);
                var foto = new iText.Layout.Element.Image(imgData)
                    .SetWidth(90).SetHeight(90);
                celdaFoto.Add(foto);
            }
            else
            {
                celdaFoto.Add(new Paragraph("Sin foto").SetFontSize(9));
            }
            tablaCuerpo.AddCell(celdaFoto);

            // Celda datos
            var celdaDatos = new Cell()
                .SetPadding(8)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            celdaDatos
                .Add(new Paragraph($"{persona.Nombre} {persona.Apellido}")
                    .SetFont(boldFont).SetFontSize(13))
                .Add(new Paragraph($"Carnet: {persona.NumCarnet}")
                    .SetFontSize(9)
                    .SetFontColor(new DeviceRgb(80, 80, 80)))
                .Add(new Paragraph($"Tipo: {persona.TipoPersona}")
                    .SetFontSize(9))
                .Add(new Paragraph($"Carrera: {persona.Carrera}")
                    .SetFontSize(9))
                .Add(new Paragraph($"Sección: {persona.Seccion}")
                    .SetFontSize(9))
                .Add(new Paragraph($"Correo: {persona.Correo}")
                    .SetFontSize(8)
                    .SetFontColor(new DeviceRgb(0, 102, 204)));
            tablaCuerpo.AddCell(celdaDatos);

            // Celda QR
            var celdaQR = new Cell()
                .SetPadding(5)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER);

            var rutaQR = Path.Combine(_env.WebRootPath, "qr",
                         $"{persona.NumCarnet}.png");
            if (File.Exists(rutaQR))
            {
                var qrData = ImageDataFactory.Create(rutaQR);
                var qrImg = new iText.Layout.Element.Image(qrData)
                    .SetWidth(80).SetHeight(80);
                celdaQR.Add(qrImg);
                celdaQR.Add(new Paragraph("Escanea para verificar")
                    .SetFontSize(7)
                    .SetTextAlignment(TextAlignment.CENTER));
            }
            tablaCuerpo.AddCell(celdaQR);
            doc.Add(tablaCuerpo);

            // ── Pie de página ────────────────────────────────────────
            var tablaFooter = new Table(1)
                .SetWidth(UnitValue.CreatePercentValue(100))
                .SetMarginTop(5);

            var celdaFooter = new Cell()
                .SetBackgroundColor(azulUMG)
                .SetPadding(5)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph(
                    $"2026  —  Documento oficial UMG  —  {persona.NumCarnet}")
                    .SetFontColor(ColorConstants.WHITE)
                    .SetFontSize(8));

            tablaFooter.AddCell(celdaFooter);
            doc.Add(tablaFooter);

            doc.Close();
            return ms.ToArray();
        }
    }
}