using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
public class ReportsModel(AppDb db, MealService meals):PageModel {
 [BindProperty(SupportsGet=true)] public DateOnly From {get;set;}=DateOnly.FromDateTime(MealService.Local(meals.Now));
 [BindProperty(SupportsGet=true)] public DateOnly To {get;set;}=DateOnly.FromDateTime(MealService.Local(meals.Now));
 [BindProperty(SupportsGet=true)] public int? RoomId {get;set;}
 [BindProperty(SupportsGet=true)] public string? Category {get;set;}
 public List<DiningRoom> Rooms {get;set;}=[];
 public List<Consumption> Rows {get;set;}=[];
 public async Task OnGetAsync() {
  Rooms=await db.DiningRooms.OrderBy(x=>x.Name).ToListAsync();
  if(From>To) ModelState.AddModelError("","La fecha inicial no puede ser posterior a la final.");
  if(To.DayNumber-From.DayNumber>366) ModelState.AddModelError("","Seleccione un periodo de hasta 366 días.");
  if(!ModelState.IsValid) return;
  var q=db.Consumptions.AsNoTracking().Include(x=>x.Employee).Include(x=>x.DiningRoom).Where(x=>x.ServiceDate>=From && x.ServiceDate<=To);
  if(RoomId!=null) q=q.Where(x=>x.DiningRoomId==RoomId);
  var rows=await q.OrderBy(x=>x.Employee.Number).ThenBy(x=>x.OccurredAt).ToListAsync();
  Rows=rows.Where(x=>string.IsNullOrEmpty(Category)||x.Employee.Category==Category).ToList();
 }
}
