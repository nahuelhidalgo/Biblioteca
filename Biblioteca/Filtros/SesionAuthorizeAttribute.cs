using Biblioteca.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Biblioteca.Filtros;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class SesionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _rolesPermitidos;

    public SesionAuthorizeAttribute(params string[] rolesPermitidos)
    {
        _rolesPermitidos = rolesPermitidos;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var usuarioId = context.HttpContext.Session.GetString(SesionKeys.UsuarioId);

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            var request = context.HttpContext.Request;
            var returnUrl = $"{request.PathBase}{request.Path}{request.QueryString}";

            context.Result = new RedirectToActionResult("Login", "Cuentas", new { returnUrl });
            return;
        }

        if (_rolesPermitidos.Length == 0)
        {
            return;
        }

        var rol = context.HttpContext.Session.GetString(SesionKeys.Rol);
        var tienePermiso = rol is not null
            && _rolesPermitidos.Contains(rol, StringComparer.OrdinalIgnoreCase);

        if (!tienePermiso)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Cuentas", null);
        }
    }
}
