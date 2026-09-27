using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CIM.CRM.Models;

public class ApplicationUser : IdentityUser<int>
{
    [StringLength(100)]
    public string? Nombre { get; set; }

    [StringLength(150)]
    public string? Apellidos { get; set; }

    [StringLength(100)]
    public string? Puesto { get; set; }

    public bool Activo { get; set; } = true;

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}