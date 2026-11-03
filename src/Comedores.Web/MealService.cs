using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
namespace Comedores.Web;
public record MealRegistrationResult(string Message,bool Ok=false,bool NeedsConfirmation=false,int SegmentCount=0,string? ServiceType=null,bool OutsideHours=false);
public class MealService(AppDb db, IConfiguration config, IWebHostEnvironment environment) {
 public static readonly TimeZoneInfo Zone=TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
 public bool IsSimulated => environment.IsDevelopment() && DateTimeOffset.TryParse(config["Demo:Now"],out _);
 public DateTime Now => environment.IsDevelopment() && DateTimeOffset.TryParse(config["Demo:Now"],out var time) ? time.UtcDateTime : DateTime.UtcNow;
 public static DateTime Local(DateTime utc)=>TimeZoneInfo.ConvertTimeFromUtc(utc,Zone);
 public string ResolveIp(IPAddress? address)=>environment.IsDevelopment() && !string.IsNullOrEmpty(config["Demo:TerminalIp"]) ? config["Demo:TerminalIp"]! : address?.MapToIPv4().ToString()??"";
 public async Task<Terminal?> Terminal(string ip)=>await db.Terminals.Include(x=>x.DiningRoom).SingleOrDefaultAsync(x=>x.Ip==ip);
 public async Task<(MealWindow? Window,string? Error,bool OutsideHours)> PeriodAtAsync(int roomId,DateTime utc) {
  var hour=TimeOnly.FromDateTime(Local(utc));
  var matches=await db.MealWindows.AsNoTracking().Where(x=>x.DiningRoomId==roomId && x.Start<=hour && x.End>=hour).ToListAsync();
  if(matches.Count==0)return (null,"Fuera del horario de servicio de este comedor.",true);
  if(matches.Count!=1)return (null,"Los horarios de este comedor se superponen; avise a Sistemas.",false);
  if(!MealTypes.IsKnown(matches[0].Type))return (null,"El horario contiene un tipo de servicio desconocido; avise a Sistemas.",false);
  return (matches[0],null,false);
 }
 public async Task<string> Register(string number,string ip,string user,Guid request) => (await RegisterDetailed(number,ip,user,request)).Message;
 public Task<MealRegistrationResult> RegisterDetailed(string number,string ip,string user,Guid request,int? confirmedSegmentCount=null,string? confirmedServiceType=null)=>RegisterCore(number,ip,user,request,confirmedSegmentCount,confirmedServiceType,false,null,null);
 public Task<MealRegistrationResult> RegisterForUser(ClaimsPrincipal user,string number,string ip,Guid request,int? count,string? confirmedType,string? selectedType,int? roomId) {
  if(!AccessProfiles.Services(user))return Task.FromResult(new MealRegistrationResult("El perfil no permite registrar servicios."));
  return RegisterAsPrincipal();
  async Task<MealRegistrationResult> RegisterAsPrincipal(){
   if(!user.IsInRole(AccessProfiles.Coordinator)) {
    var terminal=await Terminal(ip);
    if(AccessProfiles.OperatorRoom(user)==null||terminal?.DiningRoomId!=AccessProfiles.OperatorRoom(user))return new("El equipo no corresponde al comedor asignado a tu cuenta. Solicita revisión al administrador.");
   }
   return await RegisterCore(number,ip,user.Identity?.Name??"",request,count,confirmedType,user.IsInRole(AccessProfiles.Coordinator),selectedType,roomId);
  }
 }
 private async Task<MealRegistrationResult> RegisterCore(string number,string ip,string user,Guid request,int? confirmedSegmentCount,string? confirmedServiceType,bool coordinator,string? selectedType,int? roomId) {
  if(!EmployeeNumber.IsValid(number)) return new(EmployeeNumber.ValidationMessage);
  if(request==Guid.Empty) return new("Solicitud inválida; vuelve a consultar al empleado.");
  var employee=await db.Employees.SingleOrDefaultAsync(x=>x.Number==number);
  if(employee is null) return new("El empleado no está en el catálogo.");
  if(employee.Active!=true) return new("El empleado no tiene habilitado el registro. Consulte a RH.");
  var terminal=coordinator && roomId!=null && await db.DiningRooms.AnyAsync(r=>r.Id==roomId)?new Terminal {DiningRoomId=roomId.Value}:coordinator?null:await Terminal(ip);
  if(coordinator && !MealTypes.IsKnown(selectedType??""))return new("Selecciona DYN, COM o CEN.");
  if(terminal is null) return new(coordinator?"Selecciona un comedor válido.":"Equipo sin comedor autorizado.");
  var now=Now; var local=Local(now); var date=DateOnly.FromDateTime(local);
  await using var tx=await db.Database.BeginTransactionAsync();
  // Serializa registros simultáneos del mismo empleado, incluso desde otros comedores.
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({(long)employee.Id})");
  var existing=await db.Consumptions.AsNoTracking().SingleOrDefaultAsync(x=>x.RequestId==request);
  if(existing!=null) return existing.EmployeeId==employee.Id?new("Esta solicitud ya fue registrada; no se duplicó el consumo.",Ok:true):new("La solicitud pertenece a otro empleado; vuelve a consultar.");
  var period=coordinator?(Window:new MealWindow{Type=selectedType!},Error:(string?)null,OutsideHours:false):await PeriodAtAsync(terminal.DiningRoomId,now);
  if(period.Window==null) return new(period.Error!,OutsideHours:period.OutsideHours);
  var segmentCount=await db.Consumptions.CountAsync(x=>x.EmployeeId==employee.Id && x.ServiceDate==date && x.Type==period.Window.Type);
  if(segmentCount>0 && (confirmedSegmentCount!=segmentCount || confirmedServiceType!=period.Window.Type))
   return new($"El empleado {employee.Number} ya tiene {segmentCount} {(segmentCount==1?"servicio":"servicios")} de {MealTypes.Name(period.Window.Type).ToUpperInvariant()} hoy, sumando todos los comedores. ¿Deseas registrar otro?",NeedsConfirmation:true,SegmentCount:segmentCount,ServiceType:period.Window.Type);
  var sequence=(await db.Consumptions.Where(x=>x.EmployeeId==employee.Id && x.ServiceDate==date).MaxAsync(x=>(int?)x.DailySequence)??0)+1;
  db.Consumptions.Add(new Consumption {RequestId=request,EmployeeId=employee.Id,DiningRoomId=terminal.DiningRoomId,OccurredAt=now,ServiceDate=date,Type=period.Window.Type,SpecialShift=employee.SpecialShift,EmployeeName=employee.Name,Position=employee.Position,Ip=ip,Operator=user,DailySequence=sequence});
  await db.SaveChangesAsync(); await tx.CommitAsync();
  return new($"Servicio registrado. {MealTypes.Name(period.Window.Type)} · {local:dd/MM/yyyy HH:mm:ss} · Servicio {sequence} del día"+(sequence>1?" · adicional.":" · primero del día."),Ok:true);
 }
}
