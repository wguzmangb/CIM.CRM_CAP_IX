namespace CIM.CRM.Models;

/// <summary>
/// Todo lo que la pantalla de inicio necesita mostrar, en un solo objeto.
/// No es una tabla: no existe en la base. Se arma en el controlador.
/// </summary>
public class DashboardViewModel
{
    public string Saludo { get; set; } = "Hola";
    public string NombreUsuario { get; set; } = "";

    /// <summary>Verdadero para el administrador: los números son de toda la empresa.</summary>
    public bool VeTodaLaCartera { get; set; }

    // Los cuatro indicadores de arriba
    public int TotalEmpresas { get; set; }
    public int TotalContactos { get; set; }
    public int ProspectosPorAtender { get; set; }
    public int OportunidadesAbiertas { get; set; }
    public decimal ValorEmbudo { get; set; }

    // Pendientes
    public int ActividadesVencidas { get; set; }
    public List<Actividad> ActividadesPendientes { get; set; } = new();

    // Movimiento
    public List<Empresa> UltimasEmpresas { get; set; } = new();
    public List<EtapaResumen> Embudo { get; set; } = new();

    public bool SistemaVacio =>
        TotalEmpresas == 0 && TotalContactos == 0 && ProspectosPorAtender == 0;
}

/// <summary>Una etapa del embudo con cuántas oportunidades trae y por cuánto.</summary>
public class EtapaResumen
{
    public string Etapa { get; set; } = "";
    public int Cuantas { get; set; }
    public decimal Importe { get; set; }

    /// <summary>Qué tan larga se pinta la barra, de 0 a 100.</summary>
    public int Porcentaje { get; set; }
}
