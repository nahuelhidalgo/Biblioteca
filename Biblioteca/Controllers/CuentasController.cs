using Biblioteca.Datos;
using Biblioteca.Seguridad;
using Biblioteca.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Controllers;

public class CuentasController : Controller
{
    private readonly BibliotecaContext _context;

    public CuentasController(BibliotecaContext context)
    {
        _context = context;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString(SesionKeys.UsuarioId)))
        {
            return RedirigirDespuesDelLogin(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cuenta = model.Cuenta.Trim();
        var usuario = await _context.Usuarios
            .Include(u => u.Empleado)
            .FirstOrDefaultAsync(u => u.Email == cuenta && u.Activo);

        if (usuario is null || usuario.Password != model.Password)
        {
            ModelState.AddModelError(string.Empty, "La cuenta o la contrasenia no son correctas.");
            return View(model);
        }

        usuario.UltimoAcceso = DateTime.Now;
        await _context.SaveChangesAsync();

        HttpContext.Session.SetString(SesionKeys.UsuarioId, usuario.IdUsuarioSistema.ToString());
        HttpContext.Session.SetString(SesionKeys.Cuenta, usuario.Email);
        HttpContext.Session.SetString(SesionKeys.Rol, usuario.Rol);
        HttpContext.Session.SetString(SesionKeys.LegajoEmpleado, usuario.LegajoEmpleado.ToString());

        return RedirigirDespuesDelLogin(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirigirDespuesDelLogin(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Libros");
    }
}
