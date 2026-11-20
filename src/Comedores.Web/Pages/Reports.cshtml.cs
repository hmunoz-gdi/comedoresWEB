using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class ReportsModel(AppDb db, MealService meals):PageModel {
 [BindProperty(SupportsGet=true)] public DateOnly From {get;set;}=DateOnly.FromDateTime(MealService.Local(meals.Now));
 [BindProperty(SupportsGet=true)] public DateOnly To {get;set;}=DateOnly.FromDateTime(MealService.Local(meals.Now));
 [BindProperty(SupportsGet=true)] public int? RoomId {get;set;}
 [BindProperty(SupportsGet=true)] public string? Category {get;set;}
 [BindProperty(SupportsGet=true)] public string? EmployeeSearch{get;set;}
 [BindProperty(SupportsGet=true)] public bool ShowDetails{get;set;}
 [TempData] public string? Message{get;set;}
 public bool CanDelete=>AccessProfiles.ManageConsumptions(User);
 public async Task<IActionResult> OnPostDeleteAsync(long consumptionId,string? reason) {
  if(!CanDelete)return Forbid();
  var result=await new ConsumptionManagementService(db).CancelAsync(User,consumptionId,reason);
  Message=result.Message;
  return RedirectToPage(new{From=From.ToString("yyyy-MM-dd"),To=To.ToString("yyyy-MM-dd"),RoomId,Category,EmployeeSearch,ShowDetails=true});
 }
 public List<DiningRoom> Rooms {get;set;}=[];
 public List<Consumption> Rows {get;set;}=[];
 public List<PickupReportGroup> PickupGroups {get;set;}=[];
 public async Task<IActionResult> OnGetCsvAsync(string format="detalle") {
  Response.Headers.CacheControl="no-store, private";Response.Headers["X-Content-Type-Options"]="nosniff";
  if(format!="detalle"&&format!="resumen")return BadRequest("Formato de exportación inválido.");
  await OnGetAsync();
  if(!ModelState.IsValid)return BadRequest("No se puede exportar: revisa las fechas y el comedor permitido para tu cuenta.");
  return File(ReportCsv.Build(Rows,format=="resumen"),"text/csv; charset=utf-8",$"Comedores_{format}_{From:yyyyMMdd}_{To:yyyyMMdd}.csv");
 }
 public async Task OnGetAsync() {
  Rooms=await db.DiningRooms.OrderBy(x=>x.Name).ToListAsync();
  if(!AccessProfiles.ManageConsumptions(User)) {
   var terminal=await meals.Terminal(meals.ResolveIp(HttpContext.Connection.RemoteIpAddress));
   if(terminal==null||AccessProfiles.OperatorRoom(User)!=terminal.DiningRoomId){Rooms=[];ModelState.AddModelError("","Equipo sin comedor autorizado para consultar reportes.");return;}
   Rooms=Rooms.Where(r=>r.Id==terminal.DiningRoomId).ToList();
   if(RoomId!=null&&RoomId!=terminal.DiningRoomId){ModelState.AddModelError("","El perfil Operador sólo permite reportes de su comedor.");return;}
   RoomId=terminal.DiningRoomId;
  }
  if(From>To) ModelState.AddModelError("","La fecha inicial no puede ser posterior a la final.");
  if(To.DayNumber-From.DayNumber>366) ModelState.AddModelError("","Seleccione un periodo de hasta 366 días.");
  if(EmployeeSearch?.Length>150)ModelState.AddModelError("EmployeeSearch","La búsqueda admite hasta 150 caracteres.");
  if(!ModelState.IsValid) return;
  var q=db.Consumptions.AsNoTracking().Include(x=>x.Employee).Include(x=>x.DiningRoom).Where(x=>x.ServiceDate>=From && x.ServiceDate<=To);
  if(RoomId!=null) q=q.Where(x=>x.DiningRoomId==RoomId);
  var scope=q; // Conserva el ámbito autorizado para consultar integrantes del grupo completo.
  var search=EmployeeSearch?.Trim();
  if(!string.IsNullOrEmpty(search)) {
   if(EmployeeNumber.IsValid(search))q=q.Where(x=>x.Employee.Number==search);
   else {
    var escaped=search.Replace("\\","\\\\").Replace("%","\\%").Replace("_","\\_");
    var pattern="%"+escaped+"%";
    q=q.Where(x=>EF.Functions.ILike(x.EmployeeName,pattern,"\\")||EF.Functions.ILike(x.Employee.Name,pattern,"\\"));
   }
  }
  var rows=await q.OrderBy(x=>x.Employee.Number).ThenBy(x=>x.OccurredAt).ToListAsync();
  Rows=rows.Where(x=>string.IsNullOrEmpty(Category)||x.Employee.Category==Category).ToList();
  var ids=Rows.Where(c=>c.PickupBatchId.HasValue).Select(c=>c.PickupBatchId!.Value).Distinct().ToList();
  if(ids.Count>0){
   var members=await scope.Where(c=>c.PickupBatchId.HasValue&&ids.Contains(c.PickupBatchId.Value)).OrderBy(c=>c.Employee.Number).ToListAsync();
   var batches=await db.PickupBatches.AsNoTracking().Where(g=>ids.Contains(g.Id)).OrderByDescending(g=>g.OccurredAt).ThenBy(g=>g.Id).ToListAsync();
   PickupGroups=batches.Select(g=>new PickupReportGroup(g,members.Where(c=>c.PickupBatchId==g.Id&&c.DiningRoomId==g.DiningRoomId).ToList())).Where(g=>g.Members.Count>0).ToList();
  }
 }
}

public record PickupReportGroup(PickupBatch Batch,List<Consumption> Members);
