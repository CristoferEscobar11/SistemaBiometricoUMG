namespace SistemaBiometricoUMG.Models
{
    public class Inscripcion
    {
        public int Id { get; set; }
        public int PersonaId { get; set; }
        public int CursoId { get; set; }
        public DateOnly FechaInscripcion { get; set; } =
            DateOnly.FromDateTime(DateTime.Today);
        public bool Activo { get; set; } = true;

        // Navegación
        public Persona? Persona { get; set; }
        public Curso? Curso { get; set; }
    }
}