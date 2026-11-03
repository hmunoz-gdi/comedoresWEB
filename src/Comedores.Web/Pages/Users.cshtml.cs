using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
public class UsersModel(UserManager<IdentityUser> users,AppDb db,ILogger<UsersModel> logger):PageModel {
 [BindProperty,Required,StringLength(100)] public string Username{get;set;}="";
 [BindProperty] public string? Password{get;set;}
 [BindProperty,Required] public string Profile{get;set;}=AccessProfiles.Operator;
 [TempData] public string? Message{get;set;}
 [BindProperty] public int? RoomId{get;set;}
 public List<AccountSecurityEvent> SecurityEvents{get;set;}=[];
 public List<DiningRoom> Rooms{get;set;}=[];
 public List<(string Name,string Roles,string Room,bool Locked)> Accounts{get;set;}=[];
 public async Task OnGetAsync(){
  SecurityEvents=await db.AccountSecurityEvents.AsNoTracking().OrderByDescending(e=>e.Id).Take(50).ToListAsync();
  Rooms=await db.DiningRooms.OrderBy(r=>r.Name).ToListAsync();
  foreach(var account in await users.Users.OrderBy(u=>u.UserName).ToListAsync()) {
   var claim=(await users.GetClaimsAsync(account)).FirstOrDefault(c=>c.Type==AccessProfiles.RoomClaim)?.Value;
   var room=int.TryParse(claim,out var id)?Rooms.FirstOrDefault(r=>r.Id==id)?.Name:null;
   Accounts.Add((account.UserName??"",string.Join(", ",await users.GetRolesAsync(account)),room??"—",await users.IsLockedOutAsync(account)));
  }
 }
 public async Task<IActionResult> OnPostUnlockAsync(string accountName,string reason) {
  if(string.IsNullOrWhiteSpace(accountName)||accountName.Length>100||string.IsNullOrWhiteSpace(reason)||reason.Trim().Length<5||reason.Length>300){Message="Escribe el motivo del desbloqueo (entre 5 y 300 caracteres), después de verificar la identidad del solicitante.";return RedirectToPage();}
  await using var tx=await db.Database.BeginTransactionAsync();
  var normalized=users.NormalizeName(accountName.Trim());
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({normalized}))");
  var account=await users.FindByNameAsync(accountName.Trim());
  if(account==null){Message="No se encontró la cuenta.";return RedirectToPage();}
  if(!await users.IsLockedOutAsync(account)){Message="La cuenta ya está habilitada.";return RedirectToPage();}
  var reset=await users.ResetAccessFailedCountAsync(account);
  var unlock=await users.SetLockoutEndDateAsync(account,null);
  var stamp=await users.UpdateSecurityStampAsync(account);
  if(!reset.Succeeded||!unlock.Succeeded||!stamp.Succeeded){Message="No fue posible desbloquear la cuenta. Reintenta o consulta a Sistemas.";return RedirectToPage();}
  db.AccountSecurityEvents.Add(new(){OccurredAt=DateTime.UtcNow,Account=account.UserName??"",Actor=User.Identity?.Name??"",Action="Desbloqueo",Reason=reason.Trim()});await db.SaveChangesAsync();
  await tx.CommitAsync();
  logger.LogInformation("Desbloqueo administrativo. Administrador={Administrator} CuentaId={AccountId} Motivo={Reason}",User.Identity?.Name,account.Id,reason.Trim().Replace('\r',' ').Replace('\n',' '));
  Message="Cuenta desbloqueada. El usuario puede iniciar sesión con su contraseña habitual.";return RedirectToPage();
 }
 public async Task<IActionResult> OnPostAsync(string operation){
  if(!ModelState.IsValid||!new[]{AccessProfiles.Operator,AccessProfiles.Catalog,AccessProfiles.Coordinator}.Contains(Profile)){Message="Indica usuario y un perfil válido.";return RedirectToPage();}
  if(Profile==AccessProfiles.Operator&&(RoomId==null||!await db.DiningRooms.AnyAsync(r=>r.Id==RoomId))){Message="Selecciona el comedor del operador.";return RedirectToPage();}
  await using var tx=await db.Database.BeginTransactionAsync();
  var normalized=users.NormalizeName(Username.Trim());
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({normalized}))");
  var account=await users.FindByNameAsync(Username.Trim());
  if(operation=="create"){
   if(account!=null){Message="El usuario ya existe. Usa Cambiar perfil.";return RedirectToPage();}
   if(string.IsNullOrEmpty(Password)){Message="Introduce una contraseña de al menos 12 caracteres, con mayúsculas, minúsculas, números y símbolos.";return RedirectToPage();}
   account=new IdentityUser{UserName=Username.Trim()};var created=await users.CreateAsync(account,Password);
   if(!created.Succeeded){Message="No se creó la cuenta. Revisa el nombre de usuario y usa una contraseña de al menos 12 caracteres con mayúsculas, minúsculas, números y símbolos.";return RedirectToPage();}
  }else if(operation=="profile"){
   if(account==null){Message="El usuario no existe.";return RedirectToPage();}
   if(await users.IsInRoleAsync(account,AccessProfiles.Admin)){Message="La cuenta administradora se conserva para gestionar el sistema. Usa cuentas operativas separadas.";return RedirectToPage();}
  }else return BadRequest();
  // Sólo se admiten los tres perfiles operativos desde este formulario.
  var previous=await users.GetRolesAsync(account!);
  var removed=await users.RemoveFromRolesAsync(account!,previous);
  var added=removed.Succeeded?await users.AddToRoleAsync(account!,Profile):removed;
  var claims=await users.GetClaimsAsync(account!);
  var claimsRemoved=await users.RemoveClaimsAsync(account!,claims.Where(c=>c.Type==AccessProfiles.RoomClaim));
  var claimAdded=Profile==AccessProfiles.Operator?await users.AddClaimAsync(account!,new System.Security.Claims.Claim(AccessProfiles.RoomClaim,RoomId!.Value.ToString())):IdentityResult.Success;
  var stamp=await users.UpdateSecurityStampAsync(account!);
  if(added.Succeeded&&stamp.Succeeded&&claimsRemoved.Succeeded&&claimAdded.Succeeded)await tx.CommitAsync();
  Message=added.Succeeded&&stamp.Succeeded&&claimsRemoved.Succeeded&&claimAdded.Succeeded?"Perfil guardado. El usuario debe iniciar sesión nuevamente.":"No se pudo completar la asignación. Revisa el perfil de esta cuenta antes de utilizarla.";
  return RedirectToPage();
 }
}
