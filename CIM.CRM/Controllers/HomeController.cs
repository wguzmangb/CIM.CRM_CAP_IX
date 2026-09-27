using System.Diagnostics;
using System.Security.Claims;
using CIM.CRM.Data;
using CIM.CRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CIM.CRM.Controllers;

public class HomeController : Controller
{
    private static readonly string[] EtapasCerradas = { "Ganada", "Perdida" };

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<HomeController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>El administrador ve toda la cartera; los demás solo la suya.</summary>
    private bool EsAdmin => User.IsInRole(SembrarDatos.RolAdmin);

    private int MiId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var hoy = DateTime.Today;
        var usuario = await _userManager.GetUserAsync(User);
        var soloMio = !EsAdmin;
        var yo = soloMio ? MiId : 0;

        // Cada consulta se recorta a lo que le toca ver a quien está entrado
        var empresas = _context.Empresas
            .Where(e => !soloMio || e.UsuarioId == yo);

        var contactos = _context.Contactos
            .Where(c => !soloMio || c.Empresa!.UsuarioId == yo);

        var prospectos = _context.Prospectos
            .Where(p => !soloMio || p.UsuarioId == yo);

        var abiertas = _context.Oportunidades
            .Where(o => !EtapasCerradas.Contains(o.Etapa))
            .Where(o => !soloMio || o.UsuarioId == yo);

        var actividades = _context.Actividades
            .Where(a => !soloMio || a.UsuarioId == yo);

        var modelo = new DashboardViewModel
        {
            Saludo = Saludar(DateTime.Now.Hour),
            NombreUsuario = usuario?.Nombre ?? usuario?.Email ?? "",
            VeTodaLaCartera = EsAdmin,

            TotalEmpresas = await empresas.CountAsync(),
            TotalContactos = await contactos.CountAsync(),
            ProspectosPorAtender = await prospectos.CountAsync(p => p.FechaConversion == null),

            OportunidadesAbiertas = await abiertas.CountAsync(),
            ValorEmbudo = await abiertas.SumAsync(o => o.Importe ?? 0),

            ActividadesVencidas = await actividades
                .CountAsync(a => a.Estado == "Pendiente"
                              && a.FechaProgramada != null
                              && a.FechaProgramada < hoy),

            ActividadesPendientes = await actividades
                .Include(a => a.Empresa)
                .Include(a => a.Contacto)
                .Where(a => a.Estado == "Pendiente")
                .OrderBy(a => a.FechaProgramada ?? DateTime.MaxValue)
                .Take(6)
                .ToListAsync(),

            UltimasEmpresas = await empresas
                .Include(e => e.Usuario)
                .OrderByDescending(e => e.FechaRegistro)
                .Take(5)
                .ToListAsync()
        };

        modelo.Embudo = await ArmarEmbudo(abiertas);

        return View(modelo);
    }

    /// <summary>Agrupa las oportunidades por etapa y calcula el ancho de cada barra.</summary>
    private static async Task<List<EtapaResumen>> ArmarEmbudo(IQueryable<Oportunidad> abiertas)
    {
        var etapas = await abiertas
            .GroupBy(o => o.Etapa)
            .Select(g => new EtapaResumen
            {
                Etapa = g.Key,
                Cuantas = g.Count(),
                Importe = g.Sum(o => o.Importe ?? 0)
            })
            .ToListAsync();

        var mayor = etapas.Count == 0 ? 0 : etapas.Max(e => e.Cuantas);

        foreach (var etapa in etapas)
        {
            etapa.Porcentaje = mayor == 0 ? 0 : (int)Math.Round(etapa.Cuantas * 100.0 / mayor);
        }

        return etapas.OrderByDescending(e => e.Cuantas).ToList();
    }

    private static string Saludar(int hora) => hora switch
    {
        < 12 => "Buenos días",
        < 19 => "Buenas tardes",
        _ => "Buenas noches"
    };

    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
