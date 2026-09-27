using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIM.CRM.Models;

[Table("Cotizaciones")]
public class Cotizacion
{
    [Key]
    public int CotizacionId { get; set; }

    [Required]
    [Display(Name = "Oportunidad")]
    public int OportunidadId { get; set; }

    [Display(Name = "Elaboró")]
    public int? UsuarioId { get; set; }

    [Required(ErrorMessage = "El folio es obligatorio")]
    [StringLength(30)]
    public string Folio { get; set; } = null!;

    public DateTime Fecha { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(18,2)")]
    [DataType(DataType.Currency)]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [DataType(DataType.Currency)]
    public decimal Impuesto { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [DataType(DataType.Currency)]
    public decimal Total { get; set; }

    [Required]
    [StringLength(50)]
    public string Estado { get; set; } = "Borrador";

    [ForeignKey(nameof(OportunidadId))]
    public Oportunidad? Oportunidad { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public ApplicationUser? Usuario { get; set; }

    public ICollection<DetalleCotizacion> Detalles { get; set; } = new List<DetalleCotizacion>();
}
