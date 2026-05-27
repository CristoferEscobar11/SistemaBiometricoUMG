using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.IsisMtt.X509;

namespace SistemaBiometricoUMG.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Persona> Personas { get; set; }
        public DbSet<Ubicacion> Ubicaciones { get; set; }
        public DbSet<RegistroIngreso> RegistrosIngreso { get; set; }
        public DbSet<Asistencia> Asistencias { get; set; }
        public DbSet<Restriccion> Restricciones { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<HistorialCarrera> HistorialCarreras { get; set; }
        public DbSet<Curso> Cursos { get; set; }

        public DbSet<Inscripcion> Inscripciones { get; set; }
    }
}
