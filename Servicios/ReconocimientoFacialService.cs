using OpenCvSharp;
using SistemaBiometricoUMG.Models;

namespace SistemaBiometricoUMG.Servicios
{
    public class ReconocimientoFacialService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        private CascadeClassifier _detector;

        public ReconocimientoFacialService(IWebHostEnvironment env,
                                           ApplicationDbContext context)
        {
            _env = env;
            _context = context;

            // Cargar el detector de rostros de OpenCV
            var xmlPath = Path.Combine(_env.WebRootPath,
                          "opencv", "haarcascade_frontalface_default.xml");
            _detector = new CascadeClassifier(xmlPath);
        }

        // ── Detectar si hay un rostro en la imagen ───────────────────
        public bool DetectarRostro(byte[] imagenBytes)
        {
            using var mat = Mat.FromImageData(imagenBytes);
            using var gris = new Mat();
            Cv2.CvtColor(mat, gris, ColorConversionCodes.BGR2GRAY);

            var rostros = _detector.DetectMultiScale(
                gris,
                scaleFactor: 1.1,
                minNeighbors: 5,
                minSize: new Size(30, 30)
            );

            return rostros.Length > 0;
        }

        // ── Comparar rostro con personas registradas ─────────────────
        public async Task<ResultadoReconocimiento> ReconocerPersona(
            byte[] imagenBytes)
        {
            try
            {
                using var matEntrada = Mat.FromImageData(imagenBytes);
                using var grisEntrada = new Mat();
                Cv2.CvtColor(matEntrada, grisEntrada,
                             ColorConversionCodes.BGR2GRAY);

                var rostrosEntrada = _detector.DetectMultiScale(
                    grisEntrada, 1.1, 5,
                    minSize: new Size(30, 30));

                if (rostrosEntrada.Length == 0)
                    return new ResultadoReconocimiento
                    {
                        Encontrado = false,
                        Mensaje = "No se detectó ningún rostro"
                    };

                // Recortar el rostro detectado
                var rostroRect = rostrosEntrada[0];
                using var rostroEntrada = new Mat(grisEntrada, rostroRect);
                using var rostroRedim = new Mat();
                Cv2.Resize(rostroEntrada, rostroRedim, new Size(100, 100));

                // Comparar con cada persona registrada
                var personas = _context.Personas
                    .Where(p => p.Activo && !string.IsNullOrEmpty(p.FotoRuta))
                    .ToList();

                double mejorPuntaje = double.MaxValue;
                Persona mejorPersona = null;

                foreach (var persona in personas)
                {
                    var rutaFoto = Path.Combine(_env.WebRootPath,
                                  persona.FotoRuta.TrimStart('/'));

                    if (!File.Exists(rutaFoto)) continue;

                    using var matPersona = Cv2.ImRead(rutaFoto,
                                          ImreadModes.Grayscale);

                    var rostrosPersona = _detector.DetectMultiScale(
                        matPersona, 1.1, 5, minSize: new Size(30, 30));

                    if (rostrosPersona.Length == 0) continue;

                    using var rostroPersona = new Mat(
                        matPersona, rostrosPersona[0]);
                    using var rostroPersonaRedim = new Mat();
                    Cv2.Resize(rostroPersona, rostroPersonaRedim,
                               new Size(100, 100));

                    // Calcular diferencia entre rostros
                    using var diff = new Mat();
                    Cv2.Absdiff(rostroRedim, rostroPersonaRedim, diff);
                    double puntaje = Cv2.Mean(diff).Val0;

                    if (puntaje < mejorPuntaje)
                    {
                        mejorPuntaje = puntaje;
                        mejorPersona = persona;
                    }
                }

                // Umbral de reconocimiento
                if (mejorPersona != null && mejorPuntaje < 40)
                {
                    // Verificar si tiene restricción
                    var restriccion = _context.Restricciones
                        .Where(r => r.PersonaId == mejorPersona.Id && r.Activo)
                        .FirstOrDefault();

                    return new ResultadoReconocimiento
                    {
                        Encontrado = true,
                        Persona = mejorPersona,
                        Puntaje = mejorPuntaje,
                        TieneRestriccion = restriccion != null,
                        MotivoRestriccion = restriccion?.Motivo,
                        Mensaje = restriccion != null
                            ? $"⛔ ACCESO DENEGADO: {mejorPersona.Nombre} " +
                              $"{mejorPersona.Apellido}"
                            : $"✅ Bienvenido: {mejorPersona.Nombre} " +
                              $"{mejorPersona.Apellido}"
                    };
                }

                return new ResultadoReconocimiento
                {
                    Encontrado = false,
                    Mensaje = "Persona no reconocida"
                };
            }
            catch (Exception ex)
            {
                return new ResultadoReconocimiento
                {
                    Encontrado = false,
                    Mensaje = "Error: " + ex.Message
                };
            }
        }
    }

    // ── Modelo de resultado ──────────────────────────────────────────
    public class ResultadoReconocimiento
    {
        public bool Encontrado { get; set; }
        public Persona Persona { get; set; }
        public double Puntaje { get; set; }
        public bool TieneRestriccion { get; set; }
        public string MotivoRestriccion { get; set; }
        public string Mensaje { get; set; }
    }
}