using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIM.CRM.Models;

[Table("DetalleCotizacion")]
public class DetalleCotizacion
{
    [Key]
    public int DetalleCotizacionId { get; set; }

    [Required]
    public int CotizacionId { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria")]
    [StringLength(250)]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Debe ser mayor que cero")]
    public decimal Cantidad { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [DataType(DataType.Currency)]
    public decimal Precio { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [DataType(DataType.Currency)]
    public decimal Importe { get; private set; }

    [ForeignKey(nameof(CotizacionId))]
    public Cotizacion? Cotizacion { get; set; }
}
