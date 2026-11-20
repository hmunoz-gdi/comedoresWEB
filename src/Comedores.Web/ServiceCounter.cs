using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web;
public record ServiceCounterSnapshot(int? Count,string? Type,string? Room,DateOnly Date,string? Message);
public static class ServiceCounter {
 public static async Task<ServiceCounterSnapshot> ReadAsync(AppDb db,MealService meals,ClaimsPrincipal user,string ip,int? roomId,string? serviceType) {
  var now=meals.Now;var date=DateOnly.FromDateTime(MealService.Local(now));
  var denied=await meals.OperatorAccessErrorAsync(user,ip);
  if(denied!=null)return new(null,null,null,date,denied);
  DiningRoom? room;string? type;
  if(user.IsInRole(AccessProfiles.Coordinator)) {
   room=await db.DiningRooms.SingleOrDefaultAsync(r=>r.Id==roomId);
   type=MealTypes.IsKnown(serviceType??"")?serviceType:null;
  }else{
   room=(await meals.Terminal(ip))?.DiningRoom;
   type=room==null?null:(await meals.PeriodAtAsync(room.Id,now)).Window?.Type;
  }
  if(room==null||type==null)return new(null,type,room?.Name,date,user.IsInRole(AccessProfiles.Coordinator)?"Selecciona comedor y servicio.":"Sin servicio vigente.");
  var count=await db.Consumptions.CountAsync(c=>c.DiningRoomId==room.Id&&c.ServiceDate==date&&c.Type==type);
  return new(count,type,room.Name,date,null);
 }
}
