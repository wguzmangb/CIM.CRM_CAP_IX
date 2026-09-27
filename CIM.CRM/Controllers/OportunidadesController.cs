using System.Security.Claims;
using CIM.CRM.Data;
using CIM.CRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CIM.CRM.Controllers;

[Authorize]
public class OportunidadesController : Controller
{
    private readonly ApplicationDbContext _context;

    public OportunidadesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public static readonly string[] Etapas =
    {
        "Nueva", "Calificada", "Propuesta", "Negociación", "Ganada", "Perdida"
    };

    // Estas dos no se escogen a mano: las pone el botón de cerrar.
    public static readonly string[] EtapasCerradas = { "Ganada", "Perdida" };

    // La probabilidad que le toca a cada etapa cuando no se captura.
    private static decimal ProbabilidadSugerida(string etapa) => etapa switch
    {
        "Calificada" => 30,
        "Propuesta" => 60,
        "Negociación" => 80,
        "Ganada" => 100,
        "Perdida" => 0,
        _ => 10
    };

    private bool EsAdmin => User.IsInRole(SembrarDatos.RolAdmin);

    private int MiId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IQueryable<Oportunidad> Visibles()
    {
        var consulta = _context.Oportunidades.AsQueryable();

        if (!EsAdmin)
        {
            consulta = consulta.Where(o => o.UsuarioId == MiId);
        }

        return consulta;
    }

    private IQueryable<Empresa> EmpresasVisibles()
    {
        var consulta = _context.Empresas.AsQueryable();

        if (!EsAdmin)
        {
            consulta = consulta.Where(e => e.UsuarioId == MiId);
        }

        return consulta;
    }

    public async Task<IActionResult> Index(string? buscar)
    {
        var consulta = Visibles();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            consulta = consulta.Where(o =>
                o.Nombre.Contains(buscar) ||
                o.Empresa!.Nombre.Contains(buscar) ||
                o.Etapa.Contains(buscar));
        }

        ViewData["Buscar"] = buscar;

        return View(await consulta
            .Include(o => o.Empresa)
            .Include(o => o.Usuario)
            .OrderByDescending(o => o.FechaCreacion)
            .ThenBy(o => o.Nombre)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var oportunidad = await Visibles()
            .Include(o => o.Empresa)
            .Include(o => o.Contacto)
            .Include(o => o.Usuario)
            .Include(o => o.Cotizaciones)
            .FirstOrDefaultAsync(o => o.OportunidadId == id);

        if (oportunidad == null) return NotFound();

        return View(oportunidad);
    }

    public async Task<IActionResult> Create(int? empresaId)
    {
        await PrepararFormulario(empresaId);

        return View(new Oportunidad
        {
            UsuarioId = EsAdmin ? null : MiId,
            EmpresaId = empresaId ?? 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Nombre,Descripcion,EmpresaId,ContactoId,Importe,Etapa,Probabilidad,FechaEstimadaCierre,UsuarioId")] Oportunidad oportunidad)
    {
        if (!EsAdmin) oportunidad.UsuarioId = MiId;

        if (EtapasCerradas.Contains(oportunidad.Etapa)) oportunidad.Etapa = "Nueva";

        oportunidad.Probabilidad ??= ProbabilidadSugerida(oportunidad.Etapa);

        if (!await EsMiEmpresa(oportunidad.EmpresaId))
        {
            ModelState.AddModelError("EmpresaId", "Esa empresa no está en su cartera.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(oportunidad);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"Se agregó la oportunidad {oportunidad.Nombre}.";
            return RedirectToAction(nameof(Index));
        }

        await PrepararFormulario(oportunidad.EmpresaId, oportunidad.UsuarioId);
        return View(oportunidad);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var oportunidad = await Visibles().FirstOrDefaultAsync(o => o.OportunidadId == id);
        if (oportunidad == null) return NotFound();

        await PrepararFormulario(oportunidad.EmpresaId, oportunidad.UsuarioId,
            oportunidad.FechaCierreReal != null);
        return View(oportunidad);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("OportunidadId,Nombre,Descripcion,EmpresaId,ContactoId,Importe,Etapa,Probabilidad,FechaEstimadaCierre,UsuarioId,FechaCreacion")] Oportunidad oportunidad)
    {
        if (id != oportunidad.OportunidadId) return NotFound();

        var era = await Visibles().AsNoTracking()
            .FirstOrDefaultAsync(o => o.OportunidadId == id);

        if (era == null) return NotFound();

        if (!EsAdmin) oportunidad.UsuarioId = MiId;

        oportunidad.FechaCierreReal = era.FechaCierreReal;
        oportunidad.MotivoPerdida = era.MotivoPerdida;

        // Si ya está cerrada, la etapa no se mueve. Y si llega una etapa de
        // cierre sin haber pasado por el botón, se regresa a la que traía.
        if (era.FechaCierreReal != null || EtapasCerradas.Contains(oportunidad.Etapa))
        {
            oportunidad.Etapa = era.Etapa;
        }

        oportunidad.Probabilidad ??= ProbabilidadSugerida(oportunidad.Etapa);

        if (!await EsMiEmpresa(oportunidad.EmpresaId))
        {
            ModelState.AddModelError("EmpresaId", "Esa empresa no está en su cartera.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(oportunidad);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = $"Se guardó la oportunidad {oportunidad.Nombre}.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Oportunidades.Any(o => o.OportunidadId == oportunidad.OportunidadId))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        await PrepararFormulario(oportunidad.EmpresaId, oportunidad.UsuarioId,
            era.FechaCierreReal != null);
        return View(oportunidad);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var oportunidad = await Visibles()
            .Include(o => o.Empresa)
            .Include(o => o.Usuario)
            .FirstOrDefaultAsync(o => o.OportunidadId == id);

        if (oportunidad == null) return NotFound();

        return View(oportunidad);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var oportunidad = await Visibles().FirstOrDefaultAsync(o => o.OportunidadId == id);
        if (oportunidad == null) return NotFound();

        _context.Oportunidades.Remove(oportunidad);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"Se eliminó la oportunidad {oportunidad.Nombre}.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = $"No se puede eliminar {oportunidad.Nombre}: " +
                                "tiene cotizaciones o actividades ligadas.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Cerrar(int? id)
    {
        if (id == null) return NotFound();

        var oportunidad = await Visibles()
            .Include(o => o.Empresa)
            .FirstOrDefaultAsync(o => o.OportunidadId == id);

        if (oportunidad == null) return NotFound();

        if (oportunidad.FechaCierreReal != null)
        {
            TempData["Error"] = $"{oportunidad.Nombre} ya estaba cerrada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(oportunidad);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id, string resultado, DateOnly? fechaCierre,
        string? motivoPerdida)
    {
        var oportunidad = await Visibles().FirstOrDefaultAsync(o => o.OportunidadId == id);
        if (oportunidad == null) return NotFound();

        if (oportunidad.FechaCierreReal != null)
        {
            TempData["Error"] = $"{oportunidad.Nombre} ya estaba cerrada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!EtapasCerradas.Contains(resultado))
        {
            TempData["Error"] = "Hay que decir si la venta se ganó o se perdió.";
            return RedirectToAction(nameof(Cerrar), new { id });
        }

        if (resultado == "Perdida" && string.IsNullOrWhiteSpace(motivoPerdida))
        {
            TempData["Error"] = "Una venta perdida necesita su motivo.";
            return RedirectToAction(nameof(Cerrar), new { id });
        }

        oportunidad.Etapa = resultado;
        oportunidad.FechaCierreReal = fechaCierre ?? DateOnly.FromDateTime(DateTime.Now);
        oportunidad.Probabilidad = ProbabilidadSugerida(resultado);
        oportunidad.MotivoPerdida = resultado == "Perdida" ? motivoPerdida!.Trim() : null;

        await _context.SaveChangesAsync();

        TempData["Mensaje"] = resultado == "Ganada"
            ? $"Se ganó {oportunidad.Nombre}."
            : $"Se cerró {oportunidad.Nombre} como perdida.";

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<bool> EsMiEmpresa(int empresaId)
    {
        return await EmpresasVisibles().AnyAsync(e => e.EmpresaId == empresaId);
    }

    private async Task PrepararFormulario(int? empresaId = null, int? seleccionado = null,
        bool yaCerrada = false)
    {
        var etapas = yaCerrada
            ? Etapas.AsEnumerable()
            : Etapas.Where(e => !EtapasCerradas.Contains(e));

        ViewData["Etapas"] = new SelectList(etapas);

        var empresas = await EmpresasVisibles()
            .OrderBy(e => e.Nombre)
            .Select(e => new { e.EmpresaId, e.Nombre })
            .ToListAsync();

        ViewData["Empresas"] = new SelectList(empresas, "EmpresaId", "Nombre", empresaId);

        var consultaContactos = _context.Contactos.AsQueryable();

        if (!EsAdmin)
        {
            consultaContactos = consultaContactos.Where(c => c.Empresa!.UsuarioId == MiId);
        }

        var contactos = await consultaContactos
            .OrderBy(c => c.Nombre)
            .Select(c => new
            {
                empresaId = c.EmpresaId,
                contactoId = c.ContactoId,
                nombre = (c.Nombre + " " + (c.Apellidos ?? "")).Trim()
            })
            .ToListAsync();

        ViewData["ContactosJson"] = System.Text.Json.JsonSerializer.Serialize(contactos);

        ViewData["PuedeAsignar"] = EsAdmin;

        if (!EsAdmin) return;

        var usuarios = await _context.Users
            .Where(u => u.Activo)
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellidos)
            .Select(u => new { u.Id, u.Nombre, u.Apellidos, u.Email })
            .ToListAsync();

        var lista = usuarios
            .Select(u => new
            {
                u.Id,
                Texto = string.IsNullOrWhiteSpace(u.Nombre) && string.IsNullOrWhiteSpace(u.Apellidos)
                    ? u.Email
                    : $"{u.Nombre} {u.Apellidos}".Trim()
            })
            .ToList();

        ViewData["Ejecutivos"] = new SelectList(lista, "Id", "Texto", seleccionado);
    }
}