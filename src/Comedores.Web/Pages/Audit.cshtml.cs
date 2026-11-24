using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class AuditModel(AppDb db):PageModel {
 [BindProperty(SupportsGet=true)] public DateOnly From{get;set;}=DateOnly.FromDateTime(MealService.Local(DateTime.UtcNow));
 [BindProperty(SupportsGet=true)] public DateOnly To{get;set;}=DateOnly.FromDateTime(MealService.Local(DateTime.UtcNow));
 [BindProperty(SupportsGet=true)] public string? Actor{get;set;}
 [BindProperty(SupportsGet=true)] public string? ActionFilter{get;set;}
 [BindProperty(SupportsGet=true)] public string? Target{get;set;}
 [BindProperty(SupportsGet=true)] public int PageNumber{get;set;}=1;
 public List<AuditLog> Rows{get;set;}=[];
 public List<string> Actions{get;set;}=[];
 public int Total{get;set;}
 public int Pages=>Math.Max(1,(Total+99)/100);
 IQueryable<AuditLog>? Query(){
  if(From>To||To.DayNumber-From.DayNumber>366||From.Year<2000||To.Year>=9999)ModelState.AddModelError("","Selecciona fechas válidas y un periodo de hasta 366 días.");
  if(Actor?.Length>100||ActionFilter?.Length>100||Target?.Length>150)ModelState.AddModelError("","Los filtros exceden la longitud permitida.");
  if(PageNumber<1||PageNumber>100000)ModelState.AddModelError("","Página inválida.");
  if(!ModelState.IsValid)return null;
  var zone=TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
  var start=TimeZoneInfo.ConvertTimeToUtc(From.ToDateTime(TimeOnly.MinValue),zone);var end=TimeZoneInfo.ConvertTimeToUtc(To.AddDays(1).ToDateTime(TimeOnly.MinValue),zone);
  var q=db.AuditLogs.AsNoTracking().Where(e=>e.OccurredAt>=start&&e.OccurredAt<end);
  if(!string.IsNullOrWhiteSpace(Actor))q=q.Where(e=>e.Actor==Actor.Trim());
  if(!string.IsNullOrWhiteSpace(ActionFilter))q=q.Where(e=>e.Action==ActionFilter.Trim());
  if(!string.IsNullOrWhiteSpace(Target)){var text=Target.Trim().Replace("\\","\\\\").Replace("%","\\%").Replace("_","\\_");q=q.Where(e=>EF.Functions.ILike(e.Target,"%"+text+"%","\\")||EF.Functions.ILike(e.Detail,"%"+text+"%","\\"));}
  return q.OrderByDescending(e=>e.OccurredAt).ThenByDescending(e=>e.Id);
 }
 public async Task OnGetAsync(){Actions=await db.AuditLogs.Select(x=>x.Action).Distinct().OrderBy(x=>x).ToListAsync();var q=Query();if(q==null)return;Total=await q.CountAsync();if(PageNumber>Pages)PageNumber=Pages;Rows=await q.Skip((PageNumber-1)*100).Take(100).ToListAsync();}
 public async Task<IActionResult> OnGetCsvAsync(){
  var q=Query();if(q==null)return BadRequest("Revisa los filtros de auditoría.");
  if(await q.CountAsync()>50000)return BadRequest("La exportación admite hasta 50000 movimientos. Reduce las fechas o filtra por usuario.");
  string Cell(string text){if(text.TrimStart().StartsWith('=')||text.TrimStart().StartsWith('+')||text.TrimStart().StartsWith('-')||text.TrimStart().StartsWith('@')||text.StartsWith('\t')||text.StartsWith('\r'))text="'"+text;return "\""+text.Replace("\"","\"\"")+"\"";}
  var result=new StringBuilder("Fecha y hora CDMX;Usuario;IP;Movimiento;Registro;Detalle;Origen\r\n");
  foreach(var row in await q.ToListAsync())result.AppendLine(string.Join(';',new[]{MealService.Local(row.OccurredAt).ToString("dd/MM/yyyy HH:mm:ss"),row.Actor,row.Ip,row.Action,row.Target,row.Detail,row.Origin}.Select(Cell)));
  return File(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(result.ToString())).ToArray(),"text/csv; charset=utf-8",$"Auditoria_SICOM_{From:yyyyMMdd}_{To:yyyyMMdd}.csv");
 }
}
