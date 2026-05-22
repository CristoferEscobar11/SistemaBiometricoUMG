using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SistemaBiometricoUMG.Models;
using System.Security.Claims;

namespace SistemaBiometricoUMG.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Mostrar login ────────────────────────────────────────────
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // ── Procesar login ───────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Login(string usuario,
                                                string contrasena)
        {
            try
            {
                // Buscar en tabla Usuarios independiente de Personas
                var user = _context.Usuarios
                    .FirstOrDefault(u => u.NombreUsuario == usuario &&
                                         u.Contrasena == contrasena &&
                                         u.Activo);

                if (user == null)
                {
                    TempData["Error"] = "Usuario o contraseña incorrectos";
                    return View();
                }

                // Crear claims del usuario
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Nombre),
                    new Claim(ClaimTypes.Role, user.Rol),
                    new Claim("UsuarioId", user.Id.ToString())
                };

                var identity = new ClaimsIdentity(claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return View();
            }
        }

        // ── Cerrar sesión ────────────────────────────────────────────
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ── Acceso denegado ──────────────────────────────────────────
        public IActionResult Acceso() => View();
    }
}