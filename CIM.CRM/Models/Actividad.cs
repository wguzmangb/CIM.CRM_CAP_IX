using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIM.CRM.Models;

[Table("Actividades")]
public class Actividad
{
    [Key]
    public int ActividadId { get; set; }

    [Required]
    [Display(Name = "Empresa")]
    public int EmpresaId { get; set; }

    [Display(Name = "Contacto")]
    public int? ContactoId { get; set; }

    [Display(Name = "Oportunidad")]
    public int? OportunidadId { get; set; }

    [Display(Name = "Ejecutivo")]
    public int? UsuarioId { get; set; }

    [Required(ErrorMessage = "El tipo es obligatorio")]
    [StringLength(50)]
    public string Tipo { get; set; } = null!;

    [Required(ErrorMessage = "El asunto es obligatorio")]
    [StringLength(200)]
    public string Asunto { get; set; } = null!;

    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Display(Name = "Programada para")]
    public DateTime? FechaProgramada { get; set; }

    [Display(Name = "Realizada el")]
    public DateTime? FechaRealizacion { get; set; }

    [Required]
    [StringLength(50)]
    public string Estado { get; set; } = "Pendiente";

    [ForeignKey(nameof(EmpresaId))]
    public Empresa? Empresa { get; set; }

    [ForeignKey(nameof(ContactoId))]
    public Contacto? Contacto { get; set; }

    [ForeignKey(nameof(OportunidadId))]
    public Oportunidad? Oportunidad { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public ApplicationUser? Usuario { get; set; }
}
