using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaBiometricoUMG.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; }

        [Required]
        [Column("Usuario")]
        public string NombreUsuario { get; set; }

        [Required]
        public string Contrasena { get; set; }

        public string Rol { get; set; } = "Catedratico";
        public bool Activo { get; set; } = true;
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}