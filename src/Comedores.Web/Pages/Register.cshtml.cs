using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
public class RegisterModel(AppDb db,MealService meals):PageModel {
 [BindProperty(SupportsGet=true)] public string Number {get;set;}="";
 [BindProperty] public Guid RequestId {get;set;}
 [TempData] public string? Message {get;set;}
 public Employee? Employee {get;set;}
 public string? Room {get;set;}
 public int TodayCount {get;set;}
 public async Task OnGetAsync() {
  Room=(await meals.Terminal(meals.ResolveIp(HttpContext.Connection.RemoteIpAddress)))?.DiningRoom.Name;
  RequestId=Guid.NewGuid();
  if(string.IsNullOrWhiteSpace(Number)) return;
  Employee=await db.Employees.SingleOrDefaultAsync(x=>x.Number==Number.Trim());
  if(Employee==null) { Message="Empleado no encontrado en el catálogo.";return; }
  var date=DateOnly.FromDateTime(MealService.Local(meals.Now));
  TodayCount=await db.Consumptions.CountAsync(x=>x.EmployeeId==Employee.Id && x.ServiceDate==date);
 }
 public async Task<IActionResult> OnPostAsync() {
  if(!ModelState.IsValid) {Message="Solicitud inválida.";return RedirectToPage(new {Number});}
  Message=await meals.Register(Number.Trim(),meals.ResolveIp(HttpContext.Connection.RemoteIpAddress),User.Identity!.Name!,RequestId);
  return RedirectToPage(new {Number});
 }
}
