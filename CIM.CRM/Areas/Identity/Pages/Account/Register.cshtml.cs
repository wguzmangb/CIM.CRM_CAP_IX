using System.ComponentModel.DataAnnotations;
using CIM.CRM.Data;
using CIM.CRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CIM.CRM.Areas.Identity.Pages.Account;

/// <summary>
/// Alta de usuarios. NO es una pantalla pública: el sistema es privado y
/// solo un administrador da de alta a la gente del equipo.
/// </summary>
[Authorize(Roles = SembrarDatos.RolAdmin)]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserStore<ApplicationUser> _userStore;
    private readonly IUserEmailStore<ApplicationUser> _emailStore;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = (IUserEmailStore<ApplicationUser>)userStore;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Escribe el nombre")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "Escribe los apellidos")]
        [StringLength(150)]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; } = null!;

        [StringLength(100)]
        [Display(Name = "Puesto")]
        public string? Puesto { get; set; }

        [Required(ErrorMessage = "Escribe el correo")]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        [Display(Name = "Correo")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Escribe una contraseña")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "La contraseña debe tener al menos {2} caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Display(Name = "Repetir contraseña")]
        [Compare(nameof(Password), ErrorMessage = "Las dos contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = null!;

        [Required]
        [Display(Name = "Rol")]
        public string Rol { get; set; } = SembrarDatos.RolUsuario;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var usuario = new ApplicationUser
        {
            Nombre = Input.Nombre,
            Apellidos = Input.Apellidos,
            Puesto = Input.Puesto,
            Activo = true,
            FechaRegistro = DateTime.Now
        };

        await _userStore.SetUserNameAsync(usuario, Input.Email, CancellationToken.None);
        await _emailStore.SetEmailAsync(usuario, Input.Email, CancellationToken.None);

        var resultado = await _userManager.CreateAsync(usuario, Input.Password);

        if (resultado.Succeeded)
        {
            var rol = SembrarDatos.Roles.Contains(Input.Rol) ? Input.Rol : SembrarDatos.RolUsuario;
            await _userManager.AddToRoleAsync(usuario, rol);

            // Ojo: no se inicia sesión con el usuario nuevo.
            // El administrador sigue siendo quien está adentro.
            TempData["Mensaje"] = $"Se dio de alta a {Input.Nombre} {Input.Apellidos} como {rol}.";
            return RedirectToPage("/Account/Register");
        }

        foreach (var error in resultado.Errors)
        {
            ModelState.AddModelError(string.Empty, Traducir(error));
        }

        return Page();
    }

    private static string Traducir(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Ese correo ya está registrado.",
        "PasswordTooShort" => "La contraseña es demasiado corta.",
        "PasswordRequiresDigit" => "La contraseña debe llevar al menos un número.",
        "PasswordRequiresLower" => "La contraseña debe llevar al menos una minúscula.",
        "PasswordRequiresUpper" => "La contraseña debe llevar al menos una mayúscula.",
        "PasswordRequiresNonAlphanumeric" => "La contraseña debe llevar al menos un signo, como $ o !.",
        _ => error.Description
    };
}
