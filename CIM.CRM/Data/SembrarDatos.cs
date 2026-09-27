using CIM.CRM.Models;
using Microsoft.AspNetCore.Identity;

namespace CIM.CRM.Data;


public static class SembrarDatos
{
    public const string RolAdmin = "Administrador";
    public const string RolUsuario = "Usuario";

   
    public static readonly string[] Roles = { RolAdmin, RolUsuario };

    public static async Task EjecutarAsync(IServiceProvider servicios)
    {
        var roles = servicios.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var usuarios = servicios.GetRequiredService<UserManager<ApplicationUser>>();

      
        foreach (var rol in Roles)
        {
            if (!await roles.RoleExistsAsync(rol))
            {
                await roles.CreateAsync(new IdentityRole<int>(rol));
            }
        }

         var administradores = await usuarios.GetUsersInRoleAsync(RolAdmin);
        if (administradores.Count == 0)
        {
            var primero = usuarios.Users.OrderBy(u => u.Id).FirstOrDefault();
            if (primero != null)
            {
                await usuarios.AddToRoleAsync(primero, RolAdmin);
            }
        }

        foreach (var usuario in usuarios.Users.ToList())
        {
            var suyos = await usuarios.GetRolesAsync(usuario);
            if (suyos.Count == 0)
            {
                await usuarios.AddToRoleAsync(usuario, RolUsuario);
            }
        }
    }
}
