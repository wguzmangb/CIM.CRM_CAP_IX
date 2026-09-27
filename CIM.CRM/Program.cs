using CIM.CRM.Data;
using CIM.CRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);





var connectionString = builder.Configuration.GetConnectionString("CimCrmConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexion 'CimCrmConnection'.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();




builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();



var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// El rol de administrador y el primer administrador del sistema
using (var scope = app.Services.CreateScope())
{
    await SembrarDatos.EjecutarAsync(scope.ServiceProvider);
}

app.Run();