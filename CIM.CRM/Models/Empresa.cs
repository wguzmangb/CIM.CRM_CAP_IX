using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIM.CRM.Models;

[Table("Empresas")]
public class Empresa
{
    [Key]
    public int EmpresaId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(150)]
    public string Nombre { get; set; } = null!;

    [StringLength(20)]
    [Display(Name = "RFC")]
    public string? RFC { get; set; }

    [StringLength(30)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(150)]
    [EmailAddress(ErrorMessage = "Correo no válido")]
    public string? Email { get; set; }

    [StringLength(250)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [StringLength(200)]
    [Display(Name = "Sitio web")]
    public string? SitioWeb { get; set; }

    [Display(Name = "Ejecutivo")]
    public int? UsuarioId { get; set; }

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [ForeignKey(nameof(UsuarioId))]
    public ApplicationUser? Usuario { get; set; }

    public ICollection<Contacto> Contactos { get; set; } = new List<Contacto>();
    public ICollection<Oportunidad> Oportunidades { get; set; } = new List<Oportunidad>();
    public ICollection<Actividad> Actividades { get; set; } = new List<Actividad>();
}
