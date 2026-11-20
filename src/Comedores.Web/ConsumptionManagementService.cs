using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web;
public record ConsumptionCancellationResult(bool Ok,string Message);
public class ConsumptionManagementService(AppDb db) {
 public static Task EnsureSchemaAsync(AppDb db)=>db.Database.ExecuteSqlRawAsync("""
 ALTER TABLE "Consumptions" ADD COLUMN IF NOT EXISTS "IsCancelled" boolean NOT NULL DEFAULT FALSE;
 ALTER TABLE "Consumptions" ADD COLUMN IF NOT EXISTS "CancelledAt" timestamp with time zone NULL;
 ALTER TABLE "Consumptions" ADD COLUMN IF NOT EXISTS "CancelledBy" text NULL;
 ALTER TABLE "Consumptions" ADD COLUMN IF NOT EXISTS "CancellationReason" text NULL;
 CREATE INDEX IF NOT EXISTS "IX_Consumptions_DiningRoomId_ServiceDate_Type" ON "Consumptions"("DiningRoomId","ServiceDate","Type");
 """);
 public async Task<ConsumptionCancellationResult> CancelAsync(ClaimsPrincipal user,long id,string? reason) {
  if(!AccessProfiles.ManageConsumptions(user)||string.IsNullOrWhiteSpace(user.Identity?.Name))return new(false,"Tu perfil no permite eliminar servicios.");
  if(id<=0||string.IsNullOrWhiteSpace(reason)||reason.Trim().Length<5||reason.Length>300)return new(false,"Indica un motivo entre 5 y 300 caracteres.");
  var employeeId=await db.Consumptions.IgnoreQueryFilters().Where(c=>c.Id==id).Select(c=>(int?)c.EmployeeId).SingleOrDefaultAsync();
  if(employeeId==null)return new(false,"El registro no existe.");
  await using var tx=await db.Database.BeginTransactionAsync();
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({(long)employeeId.Value})");
  var row=await db.Consumptions.IgnoreQueryFilters().SingleOrDefaultAsync(c=>c.Id==id);
  if(row==null)return new(false,"El registro ya no existe.");
  await db.Entry(row).ReloadAsync();
  if(row.IsCancelled)return new(false,"El registro ya fue eliminado; no se realizó otro cambio.");
  row.IsCancelled=true;row.CancelledAt=DateTime.UtcNow;row.CancelledBy=user.Identity!.Name;row.CancellationReason=reason.Trim();
  db.AccountSecurityEvents.Add(new(){OccurredAt=row.CancelledAt.Value,Account=$"Servicio #{row.Id}",Actor=row.CancelledBy!,Action="Eliminación de servicio",Reason=$"Comedor {row.DiningRoomId} · {row.Type} · {row.ServiceDate:dd/MM/yyyy}. {reason.Trim()}"});
  await db.SaveChangesAsync();await tx.CommitAsync();
  return new(true,"Registro eliminado de los conteos y reportes. Se conserva su historial para auditoría.");
 }
}
