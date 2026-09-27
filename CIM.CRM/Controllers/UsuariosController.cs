using CIM.CRM.Data;
using CIM.CRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CIM.CRM.Controllers;

/// <summary>
/// Administración: quién entra al sistema y con qué rol.
/// Toda la clase pide rol de administrador.
/// </summary>
[Authorize(Roles = SembrarDatos.RolAdmin)]
public class UsuariosController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsuariosController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // GET: Usuarios
    public async Task<IActionResult> Index()
    {
        var yo = _userManager.GetUserId(User);

        var usuarios = await _userManager.Users
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellidos)
            .ToListAsync();

        var lista = new List<UsuarioListaViewModel>();

        foreach (var usuario in usuarios)
        {
            var roles = await _userManager.GetRolesAsync(usuario);

            lista.Add(new UsuarioListaViewModel
            {
                Id = usuario.Id,
                NombreCompleto = $"{usuario.Nombre} {usuario.Apellidos}".Trim(),
                Puesto = usuario.Puesto,
                Email = usuario.Email ?? "",
                Rol = roles.FirstOrDefault() ?? SembrarDatos.RolUsuario,
                Activo = usuario.Activo,
                FechaRegistro = usuario.FechaRegistro,
                EsUnoMismo = usuario.Id.ToString() == yo
            });
        }

        ViewData["TotalAdmins"] = lista.Count(u => u.Rol == SembrarDatos.RolAdmin);

        return View(lista);
    }

    // POST: Usuarios/CambiarRol/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(int id, string rol)
    {
        if (!SembrarDatos.Roles.Contains(rol))
        {
            TempData["Error"] = "Ese rol no existe.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario == null) return NotFound();

        // No dejar el sistema sin administradores
        if (rol == SembrarDatos.RolUsuario && !await QuedaOtroAdmin(usuario))
        {
            TempData["Error"] = "No puedes quitar al último administrador: " +
                                "el sistema se quedaría sin quien dé de alta usuarios.";
            return RedirectToAction(nameof(Index));
        }

        var actuales = await _userManager.GetRolesAsync(usuario);
        await _userManager.RemoveFromRolesAsync(usuario, actuales);
        await _userManager.AddToRoleAsync(usuario, rol);

        TempData["Mensaje"] = $"{usuario.Nombre} ahora es {rol}.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Usuarios/CambiarEstado/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString());
        if (usuario == null) return NotFound();

        // Nadie se desactiva a sí mismo: quedaría fuera sin poder volver a entrar
        if (usuario.Id.ToString() == _userManager.GetUserId(User))
        {
            TempData["Error"] = "No puedes desactivar tu propia cuenta.";
            return RedirectToAction(nameof(Index));
        }

        if (usuario.Activo && !await QuedaOtroAdmin(usuario))
        {
            TempData["Error"] = "No puedes desactivar al último administrador.";
            return RedirectToAction(nameof(Index));
        }

        usuario.Activo = !usuario.Activo;
        await _userManager.UpdateAsync(usuario);

        TempData["Mensaje"] = usuario.Activo
            ? $"Se reactivó la cuenta de {usuario.Nombre}."
            : $"Se desactivó la cuenta de {usuario.Nombre}. Ya no podrá entrar.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>¿Hay otro administrador además de este?</summary>
    private async Task<bool> QuedaOtroAdmin(ApplicationUser usuario)
    {
        var admins = await _userManager.GetUsersInRoleAsync(SembrarDatos.RolAdmin);
        return admins.Any(a => a.Id != usuario.Id && a.Activo);
    }
}
