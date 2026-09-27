using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIM.CRM.Models;

[Table("Contactos")]
public class Contacto
{
    [Key]
    public int ContactoId { get; set; }

    [Required]
    [Display(Name = "Empresa")]
    public int EmpresaId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    [StringLength(150)]
    public string? Apellidos { get; set; }

    [StringLength(100)]
    public string? Puesto { get; set; }

    [StringLength(30)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(150)]
    [EmailAddress(ErrorMessage = "Correo no válido")]
    public string? Email { get; set; }

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    [ForeignKey(nameof(EmpresaId))]
    public Empresa? Empresa { get; set; }

    public ICollection<Actividad> Actividades { get; set; } = new List<Actividad>();
}
