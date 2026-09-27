
using System.Security.Claims;
using CIM.CRM.Data;
using CIM.CRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CIM.CRM.Controllers;

[Authorize]
public class ProspectosController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProspectosController(ApplicationDbContext context)
    {
        _context = context;
    }

    public static readonly string[] Estados =
    {
        "Nuevo", "Contactado", "Interesado", "No interesado", "Convertido"
    };

    private bool EsAdmin => User.IsInRole(SembrarDatos.RolAdmin);

    private int MiId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IQueryable<Prospecto> Visibles()
    {
        var consulta = _context.Prospectos.AsQueryable();

        if (!EsAdmin)
        {
            consulta = consulta.Where(p => p.UsuarioId == MiId);
        }

        return consulta;
    }

    public async Task<IActionResult> Index(string? buscar)
    {
        var consulta = Visibles().Include(p => p.Usuario);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            consulta = consulta.Where(p =>
                p.Nombre.Contains(buscar) ||
                (p.NombreEmpresa != null && p.NombreEmpresa.Contains(buscar)) ||
                (p.Email != null && p.Email.Contains(buscar)) ||
                (p.Telefono != null && p.Telefono.Contains(buscar)))
                .Include(p => p.Usuario);
        }

        ViewData["Buscar"] = buscar;

        return View(await consulta
            .OrderByDescending(p => p.FechaRegistro)
            .ThenBy(p => p.Nombre)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var prospecto = await Visibles()
            .Include(p => p.Usuario)
            .Include(p => p.Empresa)
            .Include(p => p.Contacto)
            .FirstOrDefaultAsync(p => p.ProspectoId == id);

        if (prospecto == null) return NotFound();

        return View(prospecto);
    }

    public async Task<IActionResult> Create()
    {
        await PrepararFormulario();
        return View(new Prospecto { UsuarioId = EsAdmin ? null : MiId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Nombre,NombreEmpresa,Telefono,Email,Origen,Estado,UsuarioId")] Prospecto prospecto)
    {
        if (!EsAdmin) prospecto.UsuarioId = MiId;

        if (prospecto.Estado == "Convertido") prospecto.Estado = "Nuevo";

        if (ModelState.IsValid)
        {
            _context.Add(prospecto);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"Se agregó el prospecto {prospecto.Nombre}.";
            return RedirectToAction(nameof(Index));
        }

        await PrepararFormulario(prospecto.UsuarioId);
        return View(prospecto);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var prospecto = await Visibles().FirstOrDefaultAsync(p => p.ProspectoId == id);
        if (prospecto == null) return NotFound();

        await PrepararFormulario(prospecto.UsuarioId, prospecto.EmpresaId != null);
        return View(prospecto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("ProspectoId,Nombre,NombreEmpresa,Telefono,Email,Origen,Estado,UsuarioId,FechaRegistro")] Prospecto prospecto)
    {
        if (id != prospecto.ProspectoId) return NotFound();

        var era = await Visibles().AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProspectoId == id);

        if (era == null) return NotFound();

        if (!EsAdmin) prospecto.UsuarioId = MiId;

        
        prospecto.EmpresaId = era.EmpresaId;
        prospecto.ContactoId = era.ContactoId;
        prospecto.FechaConversion = era.FechaConversion;

        if (era.EmpresaId != null)
        {
            prospecto.Estado = "Convertido";
        }
        else if (prospecto.Estado == "Convertido")
        {
            prospecto.Estado = era.Estado;
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(prospecto);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = $"Se guardó el prospecto {prospecto.Nombre}.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Prospectos.Any(p => p.ProspectoId == prospecto.ProspectoId))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        await PrepararFormulario(prospecto.UsuarioId, era.EmpresaId != null);
        return View(prospecto);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var prospecto = await Visibles()
            .Include(p => p.Usuario)
            .FirstOrDefaultAsync(p => p.ProspectoId == id);

        if (prospecto == null) return NotFound();

        return View(prospecto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var prospecto = await Visibles().FirstOrDefaultAsync(p => p.ProspectoId == id);
        if (prospecto == null) return NotFound();

        _context.Prospectos.Remove(prospecto);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"Se eliminó el prospecto {prospecto.Nombre}.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = $"No se puede eliminar a {prospecto.Nombre}: " +
                                "tiene actividades u oportunidades ligadas.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PrepararFormulario(int? seleccionado = null, bool yaConvertido = false)
    {
        var estados = yaConvertido
            ? Estados.AsEnumerable()
            : Estados.Where(e => e != "Convertido");

        ViewData["Estados"] = new SelectList(estados);

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


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convertir(int id)
    {
        var prospecto = await Visibles().FirstOrDefaultAsync(p => p.ProspectoId == id);
        if (prospecto == null) return NotFound();

        if (prospecto.EmpresaId != null)
        {
            TempData["Error"] = $"{prospecto.Nombre} ya estaba convertido.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var empresa = new Empresa
        {
            Nombre = string.IsNullOrWhiteSpace(prospecto.NombreEmpresa)
                ? prospecto.Nombre
                : prospecto.NombreEmpresa,
            Telefono = prospecto.Telefono,
            Email = prospecto.Email,
            UsuarioId = prospecto.UsuarioId
        };

        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();

        var contacto = new Contacto
        {
            EmpresaId = empresa.EmpresaId,
            Nombre = prospecto.Nombre,
            Telefono = prospecto.Telefono,
            Email = prospecto.Email
        };

        _context.Contactos.Add(contacto);
        await _context.SaveChangesAsync();

        prospecto.EmpresaId = empresa.EmpresaId;
        prospecto.ContactoId = contacto.ContactoId;
        prospecto.FechaConversion = DateTime.Now;
        prospecto.Estado = "Convertido";

        await _context.SaveChangesAsync();

        TempData["Mensaje"] = $"{prospecto.Nombre} se convirtió: se creó la empresa " +
                              $"{empresa.Nombre} y su contacto.";

        return RedirectToAction(nameof(Details), new { id });
    }

}