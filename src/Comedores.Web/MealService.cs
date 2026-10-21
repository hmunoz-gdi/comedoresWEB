using Microsoft.EntityFrameworkCore;
using System.Net;
namespace Comedores.Web;
public class MealService(AppDb db, IConfiguration config, IWebHostEnvironment environment) {
 public static readonly TimeZoneInfo Zone=TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
 public DateTime Now => environment.IsDevelopment() && DateTimeOffset.TryParse(config["Demo:Now"],out var time) ? time.UtcDateTime : DateTime.UtcNow;
 public static DateTime Local(DateTime utc)=>TimeZoneInfo.ConvertTimeFromUtc(utc,Zone);
 public string ResolveIp(IPAddress? address)=>environment.IsDevelopment() && !string.IsNullOrEmpty(config["Demo:TerminalIp"]) ? config["Demo:TerminalIp"]! : address?.MapToIPv4().ToString()??"";
 public async Task<Terminal?> Terminal(string ip)=>await db.Terminals.Include(x=>x.DiningRoom).SingleOrDefaultAsync(x=>x.Ip==ip);
 public async Task<string> Register(string number,string ip,string user,Guid request) {
  if(request==Guid.Empty) return "Solicitud inválida; vuelve a consultar al empleado.";
  var employee=await db.Employees.SingleOrDefaultAsync(x=>x.Number==number);
  if(employee is null) return "El empleado no está en el catálogo.";
  if(employee.Active!=true) return "El empleado no tiene habilitado el registro. Consulte a RH.";
  var terminal=await Terminal(ip);
  if(terminal is null) return "Equipo sin comedor autorizado.";
  var now=Now; var local=Local(now); var date=DateOnly.FromDateTime(local); var hour=TimeOnly.FromDateTime(local);
  var windows=await db.MealWindows.Where(x=>x.DiningRoomId==terminal.DiningRoomId && x.Start<=hour && x.End>=hour).ToListAsync();
  if(windows.Count!=1) return "No hay un horario único de servicio vigente para este comedor.";
  await using var tx=await db.Database.BeginTransactionAsync();
  // Serializa registros simultáneos del mismo empleado, incluso desde otros comedores.
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({(long)employee.Id})");
  if(await db.Consumptions.AnyAsync(x=>x.RequestId==request)) return "Esta solicitud ya fue registrada; no se duplicó el consumo.";
  var sequence=(await db.Consumptions.Where(x=>x.EmployeeId==employee.Id && x.ServiceDate==date).MaxAsync(x=>(int?)x.DailySequence)??0)+1;
  db.Consumptions.Add(new Consumption {RequestId=request,EmployeeId=employee.Id,DiningRoomId=terminal.DiningRoomId,OccurredAt=now,ServiceDate=date,Type=windows[0].Type,SpecialShift=employee.SpecialShift,EmployeeName=employee.Name,Position=employee.Position,Ip=ip,Operator=user,DailySequence=sequence});
  await db.SaveChangesAsync(); await tx.CommitAsync();
  return $"Servicio registrado. Servicio {sequence} del día"+(sequence>1?" · adicional.":" · primero del día.");
 }
}
