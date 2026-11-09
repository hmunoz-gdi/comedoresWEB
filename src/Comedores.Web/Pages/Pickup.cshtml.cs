using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class PickupModel(AppDb db,MealService meals,PickupService pickups):PageModel {
 public bool Simulated=>meals.IsSimulated||meals.IsIpSimulated;
 public bool Coordinator=>User.IsInRole(AccessProfiles.Coordinator);
 public List<DiningRoom> Rooms{get;set;}=[];
 public string Room{get;set;}="";
 public string Service{get;set;}="";
 public async Task OnGetAsync(){
  Rooms=await db.DiningRooms.OrderBy(r=>r.Name).ToListAsync();
  var terminal=await meals.Terminal(meals.ResolveIp(HttpContext.Connection.RemoteIpAddress));
  Room=terminal?.DiningRoom.Name??"Selecciona comedor";
  if(terminal!=null){var period=await meals.PeriodAtAsync(terminal.DiningRoomId,meals.Now);Service=period.Window==null?period.Error!:MealTypes.Name(period.Window.Type);}
 }
 public async Task<IActionResult> OnPostSaveAsync([FromBody] PickupInput? input){
  if(input==null||!ModelState.IsValid)return BadRequest();
  var result=await pickups.RegisterAsync(User,meals.ResolveIp(HttpContext.Connection.RemoteIpAddress),input);
  return new JsonResult(new{result.Ok,result.Message,result.NeedsConfirmation,result.ServiceType,result.Warnings,NextRequestId=result.Ok?Guid.NewGuid():(Guid?)null});
 }
}
