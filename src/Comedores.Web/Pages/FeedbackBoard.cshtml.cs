using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class FeedbackBoardModel(AppDb db):PageModel {
 [BindProperty(SupportsGet=true)]public DateOnly From{get;set;}=DateOnly.FromDateTime(MealService.Local(DateTime.UtcNow)).AddDays(-30);
 [BindProperty(SupportsGet=true)]public DateOnly To{get;set;}=DateOnly.FromDateTime(MealService.Local(DateTime.UtcNow));
 [BindProperty(SupportsGet=true)]public int? RoomId{get;set;}
 [BindProperty(SupportsGet=true)]public string? Kind{get;set;}
 [BindProperty(SupportsGet=true)]public string? Status{get;set;}
 [BindProperty(SupportsGet=true)]public string? Number{get;set;}
 [BindProperty(SupportsGet=true)]public int PageNumber{get;set;}=1;
 public List<DiningRoom> Rooms{get;set;}=[];
 public List<FeedbackEntry> Rows{get;set;}=[];
 public Dictionary<string,int> ByStatus{get;set;}=[];
 public Dictionary<string,int> ByKind{get;set;}=[];
 public int Total{get;set;}
 public int Pages=>Math.Max(1,(Total+49)/50);
 public string RoomName(int id)=>Rooms.FirstOrDefault(r=>r.Id==id)?.Name??"Sin nombre";
 IQueryable<FeedbackEntry>? Query(){
  if(From>To||To.DayNumber-From.DayNumber>366||From.Year<2000||To.Year>=9999)ModelState.AddModelError("","Selecciona un periodo válido de hasta 366 días.");
  if(!string.IsNullOrEmpty(Kind)&&!FeedbackService.Kinds.Contains(Kind))ModelState.AddModelError("Kind","Tipo inválido.");
  if(!string.IsNullOrEmpty(Status)&&!FeedbackService.Statuses.Contains(Status))ModelState.AddModelError("Status","Estado inválido.");
  if(!string.IsNullOrEmpty(Number)&&!EmployeeNumber.IsValid(Number))ModelState.AddModelError("Number","Número de empleado inválido.");
  if(PageNumber<1||PageNumber>100000)ModelState.AddModelError("","Página inválida.");
  if(!ModelState.IsValid)return null;
  var start=TimeZoneInfo.ConvertTimeToUtc(From.ToDateTime(TimeOnly.MinValue),MealService.Zone);var end=TimeZoneInfo.ConvertTimeToUtc(To.AddDays(1).ToDateTime(TimeOnly.MinValue),MealService.Zone);
  var q=db.FeedbackEntries.AsNoTracking().Where(f=>f.CreatedAt>=start&&f.CreatedAt<end);
  if(RoomId.HasValue)q=q.Where(f=>f.DiningRoomId==RoomId);if(!string.IsNullOrEmpty(Kind))q=q.Where(f=>f.Kind==Kind);if(!string.IsNullOrEmpty(Status))q=q.Where(f=>f.Status==Status);if(!string.IsNullOrEmpty(Number))q=q.Where(f=>f.EmployeeNumber==Number);
  return q.OrderByDescending(f=>f.CreatedAt).ThenByDescending(f=>f.Id);
 }
 public async Task OnGetAsync(){
  Rooms=await db.DiningRooms.OrderBy(r=>r.Name).ToListAsync();var q=Query();if(q==null)return;
  Total=await q.CountAsync();ByStatus=await q.GroupBy(f=>f.Status).Select(g=>new{Key=g.Key,Count=g.Count()}).ToDictionaryAsync(x=>x.Key,x=>x.Count);ByKind=await q.GroupBy(f=>f.Kind).Select(g=>new{Key=g.Key,Count=g.Count()}).ToDictionaryAsync(x=>x.Key,x=>x.Count);
  if(PageNumber>Pages)PageNumber=Pages;Rows=await q.Skip((PageNumber-1)*50).Take(50).ToListAsync();
 }
 public async Task<IActionResult> OnGetCsvAsync(){
  if(!FeedbackService.CanManage(User))return Forbid();var q=Query();if(q==null)return BadRequest("Revisa los filtros.");if(await q.CountAsync()>10000)return BadRequest("Reduce el periodo: la exportación admite 10000 comentarios.");
  var rooms=await db.DiningRooms.ToDictionaryAsync(r=>r.Id,r=>r.Name);
  string Cell(string value){if(value.Length>0&&"=+-@".Contains(value.TrimStart().FirstOrDefault())||value.StartsWith('\t')||value.StartsWith('\r'))value="'"+value;return "\""+value.Replace("\"","\"\"")+"\"";}
  var csv=new StringBuilder("Folio;Fecha y hora CDMX;Comedor;Número proporcionado;Nombre en catálogo;Tipo;Mensaje;Estado;Recibido por\r\n");
  foreach(var f in await q.ToListAsync())csv.Append(string.Join(';',new[]{f.Folio,MealService.Local(f.CreatedAt).ToString("dd/MM/yyyy HH:mm:ss"),rooms.GetValueOrDefault(f.DiningRoomId,""),f.EmployeeNumber,f.EmployeeName,f.Kind,f.Message,f.Status,f.ReceivedBy}.Select(Cell))).Append("\r\n");
  return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),"text/csv; charset=utf-8",$"Comentarios_SICOM_{From:yyyyMMdd}_{To:yyyyMMdd}.csv");
 }
}
