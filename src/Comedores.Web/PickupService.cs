using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web;
public record PickupInput(Guid RequestId,string CollectorNumber,List<string> Numbers,string? ServiceType=null,int? RoomId=null,string? ConfirmedType=null,Dictionary<string,int>? ConfirmedCounts=null);
public record PickupWarning(string Number,string Name,int Count);
public record PickupResult(bool Ok,string Message,bool NeedsConfirmation=false,string? ServiceType=null,List<PickupWarning>? Warnings=null);
public class PickupService(AppDb db,MealService meals) {
 public async Task<PickupResult> RegisterAsync(ClaimsPrincipal user,string ip,PickupInput input) {
  if(!AccessProfiles.Services(user))return new(false,"Tu perfil no permite registrar servicios.");
  var denied=await meals.OperatorAccessErrorAsync(user,ip);if(denied!=null)return new(false,denied);
  if(input.RequestId==Guid.Empty||!EmployeeNumber.IsValid(input.CollectorNumber)||input.Numbers==null||input.Numbers.Count is <1 or >100||input.Numbers.Any(n=>!EmployeeNumber.IsValid(n)))return new(false,"Revisa responsable y comensales. El grupo admite entre 1 y 100 servicios.");
  if(input.Numbers.Distinct(StringComparer.Ordinal).Count()!=input.Numbers.Count)return new(false,"Hay un número repetido en la lista. Revisa los gafetes.");
  var coordinator=user.IsInRole(AccessProfiles.Coordinator);
  var room=coordinator?await db.DiningRooms.SingleOrDefaultAsync(r=>r.Id==input.RoomId):(await meals.Terminal(ip))?.DiningRoom;
  if(room==null)return new(false,"Selecciona un comedor válido o revisa la terminal.");
  if(coordinator&&!MealTypes.IsKnown(input.ServiceType??""))return new(false,"Selecciona DYN, COM o CEN.");
  var actor=user.Identity?.Name??"";
  var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{input.CollectorNumber,Numbers=input.Numbers.OrderBy(n=>n,StringComparer.Ordinal).ToArray(),Room=room.Id,Type=coordinator?input.ServiceType:null}))));
  await using var tx=await db.Database.BeginTransactionAsync();
  var requestLock=System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(SHA256.HashData(input.RequestId.ToByteArray()))|long.MinValue;
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({requestLock})");
  var saved=await db.PickupBatches.AsNoTracking().SingleOrDefaultAsync(b=>b.Id==input.RequestId);
  if(saved!=null)return saved.Operator==actor&&saved.Ip==ip&&saved.Fingerprint==fingerprint?new(true,$"Recogida ya registrada: {saved.Count} servicios · {MealService.Local(saved.OccurredAt):dd/MM/yyyy HH:mm:ss}. No se duplicaron."):new(false,"Este identificador ya se utilizó para otra recogida.");
  var allNumbers=input.Numbers.Append(input.CollectorNumber).Distinct().ToList();
  var people=await db.Employees.Where(e=>allNumbers.Contains(e.Number)).ToListAsync();
  // Mismos bloqueos que el registro individual, ordenados para evitar interbloqueos.
  foreach(var person in people.OrderBy(e=>e.Id))await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({(long)person.Id})");
  foreach(var person in people)await db.Entry(person).ReloadAsync();
  foreach(var number in allNumbers)if(!people.Any(e=>e.Number==number&&e.Active==true))return new(false,$"El empleado {number} no existe o está de baja. No se guardó ningún servicio.");
  var collector=people.Single(e=>e.Number==input.CollectorNumber);
  var now=meals.Now;var local=MealService.Local(now);var date=DateOnly.FromDateTime(local);var type=input.ServiceType;
  if(!coordinator){var period=await meals.PeriodAtAsync(room.Id,now);if(period.Window==null)return new(false,period.Error!);type=period.Window.Type;}
  var selected=people.Where(e=>input.Numbers.Contains(e.Number)).ToList();var ids=selected.Select(e=>e.Id).ToList();
  var counts=await db.Consumptions.Where(c=>ids.Contains(c.EmployeeId)&&c.ServiceDate==date&&c.Type==type).GroupBy(c=>c.EmployeeId).Select(g=>new{Id=g.Key,Count=g.Count()}).ToDictionaryAsync(x=>x.Id,x=>x.Count);
  var warnings=selected.Where(e=>counts.GetValueOrDefault(e.Id)>0).Select(e=>new PickupWarning(e.Number,e.Name,counts[e.Id])).ToList();
  if(warnings.Any(w=>input.ConfirmedType!=type||input.ConfirmedCounts==null||!input.ConfirmedCounts.TryGetValue(w.Number,out var accepted)||accepted!=w.Count))return new(false,"Hay servicios previos en este segmento. Revisa antes de continuar.",true,type,warnings);
  var sequences=await db.Consumptions.Where(c=>ids.Contains(c.EmployeeId)&&c.ServiceDate==date).GroupBy(c=>c.EmployeeId).Select(g=>new{Id=g.Key,Last=g.Max(c=>c.DailySequence)}).ToDictionaryAsync(x=>x.Id,x=>x.Last);
  db.PickupBatches.Add(new(){Id=input.RequestId,CollectorNumber=collector.Number,CollectorName=collector.Name,DiningRoomId=room.Id,Type=type!,OccurredAt=now,Operator=actor,Ip=ip,Fingerprint=fingerprint,Count=selected.Count});
  foreach(var person in selected)db.Consumptions.Add(new(){RequestId=Guid.NewGuid(),EmployeeId=person.Id,DiningRoomId=room.Id,OccurredAt=now,ServiceDate=date,Type=type!,SpecialShift=person.SpecialShift,EmployeeName=person.Name,Position=person.Position,Ip=ip,Operator=actor,DailySequence=sequences.GetValueOrDefault(person.Id)+1,PickupBatchId=input.RequestId,CollectedByNumber=collector.Number,CollectedByName=collector.Name});
  await db.SaveChangesAsync();await tx.CommitAsync();
  return new(true,$"Servicio aplicado: {selected.Count} servicios de {MealTypes.Name(type!)} · {local:dd/MM/yyyy HH:mm:ss}. Responsable: {collector.Number} · {collector.Name}.");
 }
}
