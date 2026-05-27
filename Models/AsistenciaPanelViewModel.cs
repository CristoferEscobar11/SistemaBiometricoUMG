namespace SistemaBiometricoUMG.Models
{
    public class AsistenciaPanelViewModel
    {
        public List<Ubicacion> Ubicaciones { get; set; } = new();
        public int UbicacionSeleccionada { get; set; }
        public List<Persona> Estudiantes { get; set; } = new();
        public List<int> PersonasPresentes { get; set; } = new();
        public List<int> PersonasConSalida { get; set; } = new();
        public bool YaConfirmado { get; set; }
        public DateTime Fecha { get; set; }
        public Curso? CursoDelSalon { get; set; }
    }
}