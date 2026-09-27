using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIM.CRM.Models;

[Table("Oportunidades")]
public class Oportunidad
{
    [Key]
    public int OportunidadId { get; set; }

    [Required]
    [Display(Name = "Empresa")]
    public int EmpresaId { get; set; }

    [Display(Name = "Contacto")]
    public int? ContactoId { get; set; }

    [Display(Name = "Ejecutivo")]
    public int? UsuarioId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(200)]
    public string Nombre { get; set; } = null!;

    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [DataType(DataType.Currency)]
    public decimal? Importe { get; set; }

    [Required]
    [StringLength(50)]
    public string Etapa { get; set; } = "Nueva";

    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100, ErrorMessage = "Debe ir de 0 a 100")]
    public decimal? Probabilidad { get; set; }

    [Display(Name = "Cierre estimado")]
    public DateOnly? FechaEstimadaCierre { get; set; }

    [Display(Name = "Cierre real")]
    public DateOnly? FechaCierreReal { get; set; }

    [StringLength(250)]
    [Display(Name = "Motivo de pérdida")]
    public string? MotivoPerdida { get; set; }

    [Display(Name = "Fecha de creación")]
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    [ForeignKey(nameof(EmpresaId))]
    public Empresa? Empresa { get; set; }

    [ForeignKey(nameof(ContactoId))]
    public Contacto? Contacto { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public ApplicationUser? Usuario { get; set; }

    public ICollection<Actividad> Actividades { get; set; } = new List<Actividad>();
    public ICollection<Cotizacion> Cotizaciones { get; set; } = new List<Cotizacion>();
}
