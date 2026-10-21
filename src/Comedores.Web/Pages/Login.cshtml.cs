using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Comedores.Web.Pages;
public class LoginModel(SignInManager<IdentityUser> signIn) : PageModel {
 [BindProperty] public string Username {get;set;}="";
 [BindProperty] public string Password {get;set;}="";
 public string? Error {get;set;}
 public async Task<IActionResult> OnPostAsync() {
  if(!ModelState.IsValid) {Error="Capture usuario y contraseña.";return Page();}
  var result=await signIn.PasswordSignInAsync(Username,Password,false,true);
  if(result.Succeeded) return RedirectToPage("/Index");
  Error="No fue posible iniciar sesión. Revise sus datos o inténtelo más tarde.";return Page();
 }
 public async Task<IActionResult> OnPostLogoutAsync(){await signIn.SignOutAsync();return RedirectToPage("/Login");}
}
