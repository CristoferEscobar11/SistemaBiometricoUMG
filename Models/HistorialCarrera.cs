namespace SistemaBiometricoUMG.Models
{
    public class HistorialCarrera
    {
        public int Id { get; set; }
        public int PersonaId { get; set; }
        public string Carrera { get; set; }
        public string? Seccion { get; set; }
        public DateOnly FechaInicio { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly? FechaFin { get; set; }
        public string? Motivo { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Navegación
        public Persona? Persona { get; set; }
    }
}