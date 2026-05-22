using System.ComponentModel.DataAnnotations;

namespace SistemaBiometricoUMG.Models
{
    public class Persona
    {
        public int Id { get; set; }

        [Display(Name = "Nombre")]
        public string? Nombre { get; set; }

        [Display(Name = "Apellido")]
        public string? Apellido { get; set; }

        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Display(Name = "Correo UMG")]
        public string? Correo { get; set; }

        [Display(Name = "Tipo de Persona")]
        public string? TipoPersona { get; set; }

        [Display(Name = "Carrera")]
        public string? Carrera { get; set; }

        [Display(Name = "Sección")]
        public string? Seccion { get; set; }

        [Display(Name = "No. Carnet")]
        public string? NumCarnet { get; set; }

        [Display(Name = "Foto")]
        public string? FotoRuta { get; set; }

        
        public string Rol { get; set; } = "Estudiante";

        public bool Activo { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}