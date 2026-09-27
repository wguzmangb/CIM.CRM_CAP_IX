namespace CIM.CRM.Models;

/// <summary>
/// Un renglón de la lista de usuarios. Junta lo que vive en AspNetUsers
/// con el rol, que vive en otra tabla.
/// </summary>
public class UsuarioListaViewModel
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string? Puesto { get; set; }
    public string Email { get; set; } = "";
    public string Rol { get; set; } = "";
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }

    /// <summary>Es la cuenta con la que está entrada la persona que ve la pantalla.</summary>
    public bool EsUnoMismo { get; set; }

    public string Iniciales
    {
        get
        {
            var partes = NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0) return Email.Length > 0 ? Email[..1].ToUpper() : "?";
            var ini = partes[0][..1];
            if (partes.Length > 1) ini += partes[1][..1];
            return ini.ToUpper();
        }
    }
}
