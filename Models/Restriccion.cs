namespace SistemaBiometricoUMG.Models
{
    public class Restriccion
    {
        public int Id { get; set; }
        public int PersonaId { get; set; }
        public string Motivo { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public bool Activo { get; set; } = true;

        public Persona Persona { get; set; }
    }
}
