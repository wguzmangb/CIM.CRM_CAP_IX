using CIM.CRM.Data;
using CIM.CRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace CIM.CRM.Controllers {

    
[Authorize]
public class CotizacionesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CotizacionesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public static readonly string[] Estados =
    {
        "Borrador", "Enviada", "Aceptada", "Rechazada"
    };

    private const decimal TasaImpuesto = 0.16m;

    private bool EsAdmin => User.IsInRole(SembrarDatos.RolAdmin);

    private int MiId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

   
    private IQueryable<Cotizacion> Visibles()
    {
        var consulta = _context.Cotizaciones.AsQueryable();

        if (!EsAdmin)
        {
            consulta = consulta.Where(c => c.Oportunidad!.UsuarioId == MiId);
        }

        return consulta;
    }

    private IQueryable<Oportunidad> OportunidadesVisibles()
    {
        var consulta = _context.Oportunidades.AsQueryable();

        if (!EsAdmin)
        {
            consulta = consulta.Where(o => o.UsuarioId == MiId);
        }

        return consulta;
    }

    public async Task<IActionResult> Index(string? buscar)
    {
        var consulta = Visibles();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            consulta = consulta.Where(c =>
                c.Folio.Contains(buscar) ||
                c.Oportunidad!.Nombre.Contains(buscar) ||
                c.Oportunidad!.Empresa!.Nombre.Contains(buscar) ||
                c.Estado.Contains(buscar));
        }

        ViewData["Buscar"] = buscar;

        return View(await consulta
            .Include(c => c.Oportunidad)!.ThenInclude(o => o!.Empresa)
            .Include(c => c.Usuario)
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.CotizacionId)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var cotizacion = await Visibles()
            .Include(c => c.Oportunidad)!.ThenInclude(o => o!.Empresa)
            .Include(c => c.Usuario)
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.CotizacionId == id);

        if (cotizacion == null) return NotFound();

        return View(cotizacion);
    }

    public async Task<IActionResult> Create(int? oportunidadId)
    {
        await PrepararFormulario(oportunidadId);

        var cotizacion = new Cotizacion
        {
            OportunidadId = oportunidadId ?? 0,
            Folio = await FolioSiguiente(),
            Fecha = DateTime.Now,
            Estado = "Borrador"
        };

   
        for (var i = 0; i < 3; i++)
        {
            cotizacion.Detalles.Add(new DetalleCotizacion());
        }

        return View(cotizacion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("OportunidadId,Folio,Fecha,Estado,Detalles")] Cotizacion cotizacion)
    {
        cotizacion.UsuarioId = MiId;

        if (!Estados.Contains(cotizacion.Estado)) cotizacion.Estado = "Borrador";

        PrepararRenglones(cotizacion);

        if (!await EsMiOportunidad(cotizacion.OportunidadId))
        {
            ModelState.AddModelError("OportunidadId", "Esa oportunidad no está en su cartera.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(cotizacion);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"Se agregó la cotización {cotizacion.Folio}.";
            return RedirectToAction(nameof(Details), new { id = cotizacion.CotizacionId });
        }

        await PrepararFormulario(cotizacion.OportunidadId, cotizacion.Estado);
        return View(cotizacion);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var cotizacion = await Visibles()
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.CotizacionId == id);

        if (cotizacion == null) return NotFound();

        await PrepararFormulario(cotizacion.OportunidadId, cotizacion.Estado);
        return View(cotizacion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("CotizacionId,OportunidadId,Folio,Fecha,Estado,Detalles")] Cotizacion cotizacion)
    {
        if (id != cotizacion.CotizacionId) return NotFound();

        var era = await Visibles().AsNoTracking()
            .FirstOrDefaultAsync(c => c.CotizacionId == id);

        if (era == null) return NotFound();

       
        cotizacion.UsuarioId = era.UsuarioId;

        if (!Estados.Contains(cotizacion.Estado)) cotizacion.Estado = era.Estado;

        PrepararRenglones(cotizacion);

        if (!await EsMiOportunidad(cotizacion.OportunidadId))
        {
            ModelState.AddModelError("OportunidadId", "Esa oportunidad no está en su cartera.");
        }

        if (ModelState.IsValid)
        {
            var viejos = await _context.DetalleCotizacion
                .Where(d => d.CotizacionId == id)
                .ToListAsync();

            _context.DetalleCotizacion.RemoveRange(viejos);

            var nuevos = cotizacion.Detalles.ToList();
            cotizacion.Detalles.Clear();

            _context.Update(cotizacion);

            foreach (var renglon in nuevos)
            {
                renglon.DetalleCotizacionId = 0;
                renglon.CotizacionId = id;
                _context.DetalleCotizacion.Add(renglon);
            }

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Se guardó la cotización {cotizacion.Folio}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await PrepararFormulario(cotizacion.OportunidadId, cotizacion.Estado);
        return View(cotizacion);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var cotizacion = await Visibles()
            .Include(c => c.Oportunidad)!.ThenInclude(o => o!.Empresa)
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.CotizacionId == id);

        if (cotizacion == null) return NotFound();

        return View(cotizacion);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var cotizacion = await Visibles()
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.CotizacionId == id);

        if (cotizacion == null) return NotFound();

        _context.DetalleCotizacion.RemoveRange(cotizacion.Detalles);
        _context.Cotizaciones.Remove(cotizacion);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"Se eliminó la cotización {cotizacion.Folio}.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = $"No se puede eliminar {cotizacion.Folio}.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> EsMiOportunidad(int oportunidadId)
    {
        return await OportunidadesVisibles().AnyAsync(o => o.OportunidadId == oportunidadId);
    }

    private void PrepararRenglones(Cotizacion cotizacion)
    {
        cotizacion.Detalles = cotizacion.Detalles
            .Where(d => !string.IsNullOrWhiteSpace(d.Descripcion)
                        || d.Cantidad > 0 || d.Precio > 0)
            .ToList();

        ModelState.Clear();
        TryValidateModel(cotizacion);

        if (cotizacion.Detalles.Count == 0)
        {
            ModelState.AddModelError("", "La cotización necesita al menos un renglón.");
        }

        cotizacion.Subtotal = cotizacion.Detalles.Sum(d => d.Cantidad * d.Precio);
        cotizacion.Impuesto = Math.Round(cotizacion.Subtotal * TasaImpuesto, 2);
        cotizacion.Total = cotizacion.Subtotal + cotizacion.Impuesto;
    }

    private async Task<string> FolioSiguiente()
    {
        var cuantas = await _context.Cotizaciones.CountAsync();
        return "COT-" + (cuantas + 1).ToString("0000");
    }

    private async Task PrepararFormulario(int? oportunidadId = null, string? estado = null)
    {
        ViewData["Estados"] = new SelectList(Estados, estado);

        var oportunidades = await OportunidadesVisibles()
            .OrderBy(o => o.Nombre)
            .Select(o => new
            {
                o.OportunidadId,
                Texto = o.Nombre + " - " + o.Empresa!.Nombre
            })
            .ToListAsync();

        ViewData["Oportunidades"] =
            new SelectList(oportunidades, "OportunidadId", "Texto", oportunidadId);
    }
}

}