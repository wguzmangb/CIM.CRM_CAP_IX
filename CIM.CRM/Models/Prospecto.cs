using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIM.CRM.Models;

[Table("Prospectos")]
public class Prospecto
{
    [Key]
    public int ProspectoId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(150)]
    public string Nombre { get; set; } = null!;

    [Column("Empresa")]
    [StringLength(150)]
    [Display(Name = "Empresa (texto libre)")]
    public string? NombreEmpresa { get; set; }

    [StringLength(30)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(150)]
    [EmailAddress(ErrorMessage = "Correo no válido")]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? Origen { get; set; }

    [Required]
    [StringLength(50)]
    public string Estado { get; set; } = "Nuevo";

    [Display(Name = "Ejecutivo")]
    public int? UsuarioId { get; set; }

    public int? EmpresaId { get; set; }
    public int? ContactoId { get; set; }

    [Display(Name = "Fecha de conversión")]
    public DateTime? FechaConversion { get; set; }

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [ForeignKey(nameof(UsuarioId))]
    public ApplicationUser? Usuario { get; set; }

    [ForeignKey(nameof(EmpresaId))]
    public Empresa? Empresa { get; set; }

    [ForeignKey(nameof(ContactoId))]
    public Contacto? Contacto { get; set; }
}
