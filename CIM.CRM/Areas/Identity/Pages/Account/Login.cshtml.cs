using System.ComponentModel.DataAnnotations;
using CIM.CRM.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CIM.CRM.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ReturnUrl { get; set; } = "/";

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Escribe tu correo")]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        [Display(Name = "Correo")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Escribe tu contraseña")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = null!;

        [Display(Name = "Mantener la sesión abierta")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        ReturnUrl = returnUrl ?? Url.Content("~/");

        // Limpia cualquier sesión externa a medias
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Una cuenta desactivada no entra, aunque la contraseña sea correcta
        var cuenta = await _userManager.FindByEmailAsync(Input.Email);
        if (cuenta != null && !cuenta.Activo)
        {
            ModelState.AddModelError(string.Empty,
                "Esta cuenta está desactivada. Habla con el administrador del sistema.");
            return Page();
        }

        var resultado = await _signInManager.PasswordSignInAsync(
            Input.Email,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (resultado.Succeeded)
        {
            return LocalRedirect(ReturnUrl);
        }

        if (resultado.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty,
                "La cuenta está bloqueada por varios intentos fallidos. Inténtalo más tarde.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "El correo o la contraseña no son correctos.");
        return Page();
    }
}
