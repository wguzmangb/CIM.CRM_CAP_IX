using CIM.CRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CIM.CRM.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Contacto> Contactos => Set<Contacto>();
    public DbSet<Prospecto> Prospectos => Set<Prospecto>();
    public DbSet<Oportunidad> Oportunidades => Set<Oportunidad>();
    public DbSet<Actividad> Actividades => Set<Actividad>();
    public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();
    public DbSet<DetalleCotizacion> DetalleCotizacion => Set<DetalleCotizacion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Valores por omisión, puestos por SQL Server
        builder.Entity<Empresa>()
            .Property(e => e.FechaRegistro).HasDefaultValueSql("GETDATE()");

        builder.Entity<Contacto>()
            .Property(e => e.FechaRegistro).HasDefaultValueSql("GETDATE()");

        builder.Entity<Prospecto>(e =>
        {
            e.Property(p => p.FechaRegistro).HasDefaultValueSql("GETDATE()");
            e.Property(p => p.Estado).HasDefaultValue("Nuevo");
        });

        builder.Entity<Oportunidad>(e =>
        {
            e.Property(o => o.FechaCreacion).HasDefaultValueSql("GETDATE()");
            e.Property(o => o.Etapa).HasDefaultValue("Nueva");
        });

        builder.Entity<Actividad>()
            .Property(a => a.Estado).HasDefaultValue("Pendiente");

        builder.Entity<Cotizacion>(e =>
        {
            e.Property(c => c.Fecha).HasDefaultValueSql("GETDATE()");
            e.Property(c => c.Estado).HasDefaultValue("Borrador");
            e.Property(c => c.Subtotal).HasDefaultValue(0m);
            e.Property(c => c.Impuesto).HasDefaultValue(0m);
            e.Property(c => c.Total).HasDefaultValue(0m);
        });

        // La columna calculada
        builder.Entity<DetalleCotizacion>()
            .Property(d => d.Importe)
            .HasComputedColumnSql("[Cantidad] * [Precio]", stored: true);

        // Nadie se borra en cascada
        foreach (var fk in builder.Model.GetEntityTypes()
                                        .SelectMany(t => t.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
