namespace SistemaBiometricoUMG.Models
{
    public class RegistroIngreso
    {
        public int Id { get; set; }
        public int PersonaId { get; set; }
        public int UbicacionId { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.Now;
        public DateTime? FechaSalida { get; set; }
        public string TipoMovimiento { get; set; } = "Entrada";

        public Persona? Persona { get; set; }
        public Ubicacion? Ubicacion { get; set; }
    }
}