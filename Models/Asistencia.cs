namespace SistemaBiometricoUMG.Models
{
    public class Asistencia
    {
        public int Id { get; set; }
        public int PersonaId { get; set; }
        public int UbicacionId { get; set; }
        public DateOnly Fecha { get; set; }
        public bool Presente { get; set; } = false;
        public bool Confirmado { get; set; } = false;
        public int? ConfirmadoPor { get; set; }

        public Persona Persona { get; set; }
        public Ubicacion Ubicacion { get; set; }
    }
}
