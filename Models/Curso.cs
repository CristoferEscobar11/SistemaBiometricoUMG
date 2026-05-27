namespace SistemaBiometricoUMG.Models
{
    public class Curso
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Carrera { get; set; }
        public int UbicacionId { get; set; }
        public string? Horario { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Navegación
        public Ubicacion? Ubicacion { get; set; }
    }
}