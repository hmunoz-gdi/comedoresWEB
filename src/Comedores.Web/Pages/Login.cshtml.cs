using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
public class LoginModel(SignInManager<IdentityUser> signIn,UserManager<IdentityUser> users,AppDb db,ILogger<LoginModel> logger) : PageModel {
 [BindProperty] public string Username {get;set;}="";
 [BindProperty] public string Password {get;set;}="";
 public string? Error {get;set;}
 public async Task<IActionResult> OnPostAsync() {
  if(!ModelState.IsValid) {Error="Capture usuario y contraseña.";return Page();}
  await using var tx=await db.Database.BeginTransactionAsync();
  var normalized=users.NormalizeName(Username.Trim());
  // Un mismo bloqueo se usa para intentos simultáneos y desbloqueos administrativos.
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({normalized}))");
  var account=await users.FindByNameAsync(Username.Trim());
  // La validación de una cookie pudo cargar la cuenta antes de esperar el bloqueo.
  if(account!=null){await db.Entry(account).ReloadAsync();if(db.Entry(account).State==EntityState.Detached)account=null;}
  if(account!=null&&!account.LockoutEnabled) {
   var enabled=await users.SetLockoutEnabledAsync(account,true);
   if(!enabled.Succeeded){Error="No fue posible iniciar sesión. Contacta a la Mesa de Ayuda del AICM, extensión 2442.";return Page();}
  }
  var wasLocked=account!=null&&await users.IsLockedOutAsync(account);
  var result=await signIn.PasswordSignInAsync(Username.Trim(),Password,false,true);
  if(result.IsLockedOut && account!=null) {
   // El bloqueo no vence automáticamente: sólo se retira desde administración.
   var locked=await users.SetLockoutEndDateAsync(account,DateTimeOffset.MaxValue);
   if(!locked.Succeeded){Error="No fue posible completar el acceso. Contacta a la Mesa de Ayuda del AICM, extensión 2442.";return Page();}
   if(!wasLocked){
    var stamp=await users.UpdateSecurityStampAsync(account);
    if(!stamp.Succeeded){Error="No fue posible completar el bloqueo. Contacta a la Mesa de Ayuda del AICM, extensión 2442.";return Page();}
    db.AccountSecurityEvents.Add(new(){OccurredAt=DateTime.UtcNow,Account=account.UserName??"",Actor="Inicio de sesión",Action="Bloqueo",Reason="Tres intentos fallidos consecutivos."});await db.SaveChangesAsync();
   }
   logger.LogWarning("Cuenta bloqueada por intentos de acceso. CuentaId={AccountId}",account.Id);
  }
  await tx.CommitAsync();
  if(result.Succeeded) return RedirectToPage("/Index");
  ModelState.Remove(nameof(Password));Password="";
  Error=result.IsLockedOut?"Cuenta bloqueada. Comunícate con la Mesa de Ayuda del AICM, extensión 2442, para solicitar el desbloqueo.":"No fue posible iniciar sesión. Revisa usuario y contraseña. Si necesitas ayuda, comunícate con la Mesa de Ayuda del AICM, extensión 2442.";return Page();
 }
 public async Task<IActionResult> OnPostLogoutAsync(){await signIn.SignOutAsync();return RedirectToPage("/Login");}
}
