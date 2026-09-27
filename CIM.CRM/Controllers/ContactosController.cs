using System.Security.Claims;
using CIM.CRM.Data;
using CIM.CRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CIM.CRM.Controllers;

[Authorize]   // wguzman
public class ContactosController : Controller
{
    private readonly ApplicationDbContext _context;

    public ContactosController(ApplicationDbContext context)
    {
        _context = context;
    }

    private bool EsAdmin => User.IsInRole(SembrarDatos.RolAdmin);

    private int MiId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); //wguzman

    private IQueryable<Contacto> Visibles()
    {
        var consulta = _context.Contactos.AsQueryable();

        if (!EsAdmin)
        {
            consulta = consulta.Where(c => c.Empresa!.UsuarioId == MiId);
        }

        return consulta;
    } //wguzman

    private IQueryable<Empresa> EmpresasVisibles()
    {
        var consulta = _context.Empresas.AsQueryable();

        if (!EsAdmin)
        {
            consulta = consulta.Where(e => e.UsuarioId == MiId);
        }

        return consulta;
    }

    private async Task<bool> EsMiEmpresa(int empresaId)
    {
        return await EmpresasVisibles().AnyAsync(e => e.EmpresaId == empresaId);
    }

    public async Task<IActionResult> Index(string? buscar)
    {
        var consulta = Visibles().Include(c => c.Empresa);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            consulta = consulta.Where(c =>
                c.Nombre.Contains(buscar) ||
                (c.Apellidos != null && c.Apellidos.Contains(buscar)) ||
                (c.Email != null && c.Email.Contains(buscar)) ||
                c.Empresa!.Nombre.Contains(buscar))
                .Include(c => c.Empresa);
        }

        ViewData["Buscar"] = buscar;

        return View(await consulta
            .OrderBy(c => c.Nombre)
            .ThenBy(c => c.Apellidos)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var contacto = await Visibles()
            .Include(c => c.Empresa)
            .FirstOrDefaultAsync(c => c.ContactoId == id);

        if (contacto == null) return NotFound();

        return View(contacto);
    }

    public async Task<IActionResult> Create(int? empresaId)
    {
        await PrepararFormulario(empresaId);
        return View(new Contacto { EmpresaId = empresaId ?? 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("EmpresaId,Nombre,Apellidos,Puesto,Telefono,Email")] Contacto contacto)
    {
        if (!await EsMiEmpresa(contacto.EmpresaId))
        {
            ModelState.AddModelError(nameof(contacto.EmpresaId),
                "Esa empresa no está en tu cartera.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(contacto);
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"Se agregó el contacto {contacto.Nombre}.";
            return RedirectToAction(nameof(Index));
        }

        await PrepararFormulario(contacto.EmpresaId);
        return View(contacto);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var contacto = await Visibles().FirstOrDefaultAsync(c => c.ContactoId == id);
        if (contacto == null) return NotFound();

        await PrepararFormulario(contacto.EmpresaId);
        return View(contacto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("ContactoId,EmpresaId,Nombre,Apellidos,Puesto,Telefono,Email,FechaRegistro")] Contacto contacto)
    {
        if (id != contacto.ContactoId) return NotFound();

        var era = await Visibles().AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContactoId == id);

        if (era == null) return NotFound();

        if (!await EsMiEmpresa(contacto.EmpresaId))
        {
            ModelState.AddModelError(nameof(contacto.EmpresaId),
                "Esa empresa no está en tu cartera.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(contacto);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = $"Se guardó el contacto {contacto.Nombre}.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Contactos.Any(c => c.ContactoId == contacto.ContactoId))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        await PrepararFormulario(contacto.EmpresaId);
        return View(contacto);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var contacto = await Visibles()
            .Include(c => c.Empresa)
            .FirstOrDefaultAsync(c => c.ContactoId == id);

        if (contacto == null) return NotFound();

        return View(contacto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var contacto = await Visibles().FirstOrDefaultAsync(c => c.ContactoId == id);
        if (contacto == null) return NotFound();

        _context.Contactos.Remove(contacto);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Mensaje"] = $"Se eliminó el contacto {contacto.Nombre}.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = $"No se puede eliminar a {contacto.Nombre}: " +
                                "tiene actividades, oportunidades o prospectos ligados.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PrepararFormulario(int? seleccionada = null)
    {
        var empresas = await EmpresasVisibles()
            .OrderBy(e => e.Nombre)
            .Select(e => new { e.EmpresaId, e.Nombre })
            .ToListAsync();

        ViewData["Empresas"] = new SelectList(empresas, "EmpresaId", "Nombre", seleccionada);
    }
}