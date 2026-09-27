using System.Security.Claims;
using CIM.CRM.Data;
using CIM.CRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CIM.CRM.Controllers;

[Authorize]
public class EmpresasController : Controller
{
    private readonly ApplicationDbContext _context;

    public EmpresasController(ApplicationDbContext context)
    {
        _context = context;
    }

    
    private bool EsAdmin => User.IsInRole(SembrarDatos.RolAdmin);

    private int MiId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    
    private IQueryable<Empresa> Visibles()
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
        var consulta = Visibles().Include(e => e.Usuario);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            consulta = consulta.Where(e =>
                e.Nombre.Contains(buscar) ||
                (e.RFC != null && e.RFC.Contains(buscar)))
                .Include(e => e.Usuario);
        }

        ViewData["Buscar"] = buscar;
        ViewData["VeTodo"] = EsAdmin;

        return View(await consulta.OrderBy(e => e.Nombre).ToListAsync());
    }

    
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var empresa = await Visibles()
            .Include(e => e.Usuario)
            .Include(e => e.Contactos)
            .FirstOrDefaultAsync(e => e.EmpresaId == id);

    
        if (empresa == null) return NotFound();

        return View(empresa);
    }

    
    public async Task<IActionResult> Create()
    {
        await PrepararFormulario();
        return View(new Empresa { UsuarioId = EsAdmin ? null : MiId });
    }

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Nombre,RFC,Telefono,Email,Direccion,SitioWeb,UsuarioId")] Empresa empresa)
    {
    
        if (!EsAdmin) empresa.UsuarioId = MiId;

        if (ModelState.IsValid)
        {
            _context.Add(empresa);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"Se agregó la empresa {empresa.Nombre}.";
            return RedirectToAction(nameof(Index));
        }

        await PrepararFormulario(empresa.UsuarioId);
        return View(empresa);
    }

    
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var empresa = await Visibles().FirstOrDefaultAsync(e => e.EmpresaId == id);
        if (empresa == null) return NotFound();

        await PrepararFormulario(empresa.UsuarioId);
        return View(empresa);
    }

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("EmpresaId,Nombre,RFC,Telefono,Email,Direccion,SitioWeb,UsuarioId,FechaRegistro")] Empresa empresa)
    {
        if (id != empresa.EmpresaId) return NotFound();

    
        var era = await Visibles().AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmpresaId == id);

        if (era == null) return NotFound();

    
        if (!EsAdmin) empresa.UsuarioId = MiId;

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(empresa);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = $"Se guardó la empresa {empresa.Nombre}.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Empresas.Any(e => e.EmpresaId == empresa.EmpresaId))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        await PrepararFormulario(empresa.UsuarioId);
        return View(empresa);
    }

    
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var empresa = await Visibles()
            .Include(e => e.Usuario)
            .FirstOrDefaultAsync(e => e.EmpresaId == id);

        if (empresa == null) return NotFound();

        return View(empresa);
    }

    
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var empresa = await Visibles().FirstOrDefaultAsync(e => e.EmpresaId == id);
        if (empresa == null) return NotFound();

        _context.Empresas.Remove(empresa);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"Se eliminó la empresa {empresa.Nombre}.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = $"No se puede eliminar {empresa.Nombre}: " +
                                "tiene contactos, oportunidades o actividades registradas.";
        }

        return RedirectToAction(nameof(Index));
    }

    
    private async Task PrepararFormulario(int? seleccionado = null)
    {
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
