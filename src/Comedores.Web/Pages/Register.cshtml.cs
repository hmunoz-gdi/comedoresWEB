using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
public class RegisterModel(AppDb db,MealService meals,EmployeePhotos photos):PageModel {
 [BindProperty(SupportsGet=true)] public string Number {get;set;}="";
 [BindProperty] public Guid RequestId {get;set;}
 [TempData] public string? Message {get;set;}
 public Employee? Employee {get;set;}
 public string? Room {get;set;}
 public int TodayCount {get;set;}
 public bool HasPhoto {get;set;}
 public async Task OnGetAsync() {
  Room=(await meals.Terminal(meals.ResolveIp(HttpContext.Connection.RemoteIpAddress)))?.DiningRoom.Name;
  RequestId=Guid.NewGuid();
  if(string.IsNullOrEmpty(Number) && !Request.Query.ContainsKey("Number")) return;
  if(!EmployeeNumber.IsValid(Number)) {Message=EmployeeNumber.ValidationMessage;return;}
  Employee=await db.Employees.SingleOrDefaultAsync(x=>x.Number==Number);
  if(Employee==null) { Message="Empleado no encontrado en el catálogo.";return; }
  HasPhoto=photos.Find(Employee.Number)!=null;
  var date=DateOnly.FromDateTime(MealService.Local(meals.Now));
  TodayCount=await db.Consumptions.CountAsync(x=>x.EmployeeId==Employee.Id && x.ServiceDate==date);
 }
 public async Task<IActionResult> OnGetPhotoAsync(string number) {
  Response.Headers.CacheControl="no-store, private";
  Response.Headers["X-Content-Type-Options"]="nosniff";
  var path=photos.Find(number??"");
  if(path is null || !await db.Employees.AnyAsync(x=>x.Number==number)) return NotFound();
  byte[] bytes;
  try { bytes=await System.IO.File.ReadAllBytesAsync(path); }
  catch(IOException) { return NotFound(); }
  var type=EmployeePhotos.ContentType(bytes);
  return type is null ? NotFound() : File(bytes,type);
 }
 public async Task<IActionResult> OnPostAsync() {
  if(!EmployeeNumber.IsValid(Number)) {Message=EmployeeNumber.ValidationMessage;return RedirectToPage();}
  if(!ModelState.IsValid) {Message="Solicitud inválida.";return RedirectToPage(new {Number});}
  Message=await meals.Register(Number,meals.ResolveIp(HttpContext.Connection.RemoteIpAddress),User.Identity!.Name!,RequestId);
  return RedirectToPage(new {Number});
 }
}
