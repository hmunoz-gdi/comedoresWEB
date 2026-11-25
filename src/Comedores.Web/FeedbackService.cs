using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web;
public class FeedbackEntry {
 public long Id{get;set;}
 public Guid RequestId{get;set;}
 public string Fingerprint{get;set;}="";
 public DateTime CreatedAt{get;set;}
 public int EmployeeId{get;set;}
 public string EmployeeNumber{get;set;}="";
 public string EmployeeName{get;set;}="";
 public int DiningRoomId{get;set;}
 public string Kind{get;set;}="";
 public string Message{get;set;}="";
 public string Status{get;set;}="Pendiente";
 public string ReceivedBy{get;set;}="";
 public string Ip{get;set;}="";
 public int Version{get;set;}=1;
 public string Folio=>$"QSR-{Id:000000}";
}
public class FeedbackFollowUp {
 public long Id{get;set;}
 public long FeedbackEntryId{get;set;}
 public DateTime OccurredAt{get;set;}
 public string Actor{get;set;}="";
 public string Ip{get;set;}="";
 public string PreviousStatus{get;set;}="";
 public string Status{get;set;}="";
 public string Note{get;set;}="";
}
public record FeedbackResult(bool Ok,string Message,long? Id=null);
public class FeedbackService(AppDb db,MealService meals){
 public static readonly string[] Kinds=["Queja","Sugerencia","Reconocimiento"];
 public static readonly string[] Statuses=["Pendiente","En atención","Resuelto"];
 public static bool CanManage(ClaimsPrincipal user)=>user.IsInRole(AccessProfiles.Coordinator)||user.IsInRole(AccessProfiles.Admin);
 public static bool CanSubmit(ClaimsPrincipal user)=>AccessProfiles.Services(user)||user.IsInRole(AccessProfiles.Admin);
 public async Task<FeedbackResult> SubmitAsync(ClaimsPrincipal user,string ip,Guid requestId,string? number,string? kind,string? message,int? roomId){
  if(!CanSubmit(user)||user.Identity?.IsAuthenticated!=true)return new(false,"Tu perfil no permite recibir comentarios.");
  var text=message?.Trim()??"";
  if(requestId==Guid.Empty||!EmployeeNumber.IsValid(number??"")||!Kinds.Contains(kind??"")||text.Length is <10 or >3000||text.Contains('\0'))return new(false,"Indica un número válido, el tipo y un mensaje entre 10 y 3000 caracteres.");
  var denied=await meals.OperatorAccessErrorAsync(user,ip);if(denied!=null)return new(false,denied);
  var room=CanManage(user)?await db.DiningRooms.SingleOrDefaultAsync(r=>r.Id==roomId):(await meals.Terminal(ip))?.DiningRoom;
  if(room==null)return new(false,"Selecciona un comedor válido o utiliza una terminal autorizada.");
  var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{Number=number,Kind=kind,Message=text,Room=room.Id,User=user.FindFirstValue(ClaimTypes.NameIdentifier)??user.Identity.Name}))));
  await using var tx=await db.Database.BeginTransactionAsync();
  var key=System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(SHA256.HashData(requestId.ToByteArray()))|long.MinValue;
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})");
  var previous=await db.FeedbackEntries.AsNoTracking().SingleOrDefaultAsync(f=>f.RequestId==requestId);
  if(previous!=null)return previous.Fingerprint==fingerprint?new(true,$"Comentario recibido. Folio {previous.Folio}.",previous.Id):new(false,"La solicitud ya se utilizó. Recarga el formulario antes de enviar otro comentario.");
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({number!}))");
  var employee=await db.Employees.AsNoTracking().SingleOrDefaultAsync(e=>e.Number==number);
  if(employee?.Active!=true)return new(false,"El número de empleado no existe o está dado de baja. Solicita al operador revisar el catálogo.");
  var entry=new FeedbackEntry{RequestId=requestId,Fingerprint=fingerprint,CreatedAt=DateTime.UtcNow,EmployeeId=employee.Id,EmployeeNumber=employee.Number,EmployeeName=employee.Name,DiningRoomId=room.Id,Kind=kind!,Message=text,ReceivedBy=user.Identity.Name??"",Ip=ip};
  db.FeedbackEntries.Add(entry);await db.SaveChangesAsync();
  db.AuditLogs.Add(new(){Actor=entry.ReceivedBy,Ip=ip,Action="Recepción de comentario",Target=entry.Folio,Detail=$"{kind} · {room.Name} · Número proporcionado: {employee.Number}. Identificación declarada por el comensal."});
  await db.SaveChangesAsync();await tx.CommitAsync();return new(true,$"Gracias por compartir tu opinión. Conserva tu folio: {entry.Folio}.",entry.Id);
 }
 public async Task<FeedbackResult> FollowUpAsync(ClaimsPrincipal user,string ip,long id,int version,string? status,string? note){
  if(!CanManage(user)||user.Identity?.IsAuthenticated!=true)return new(false,"Tu perfil no permite atender comentarios.");
  var text=note?.Trim()??"";
  if(id<=0||version<1||!Statuses.Contains(status??"")||text.Length is <5 or >3000||text.Contains('\0'))return new(false,"Selecciona un estado y escribe una nota entre 5 y 3000 caracteres.");
  await using var tx=await db.Database.BeginTransactionAsync();
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({7421},{checked((int)(id%int.MaxValue))})");
  var entry=await db.FeedbackEntries.SingleOrDefaultAsync(f=>f.Id==id);if(entry==null)return new(false,"El folio no existe.");await db.Entry(entry).ReloadAsync();
  if(entry.Version!=version)return new(false,"Otro usuario ya actualizó este folio. Revisa su historial antes de guardar nuevamente.");
  var previous=entry.Status;entry.Status=status!;entry.Version++;
  db.FeedbackFollowUps.Add(new(){FeedbackEntryId=id,OccurredAt=DateTime.UtcNow,Actor=user.Identity.Name??"",Ip=ip,PreviousStatus=previous,Status=status!,Note=text});
  db.AuditLogs.Add(new(){Actor=user.Identity.Name??"",Ip=ip,Action="Atención de comentario",Target=entry.Folio,Detail=$"Estado anterior: {previous} · Estado actual: {status}. Nota conservada en el historial del folio."});
  await db.SaveChangesAsync();await tx.CommitAsync();return new(true,"Atención guardada en el historial del folio.",id);
 }
 public static Task EnsureSchemaAsync(AppDb db)=>db.Database.ExecuteSqlRawAsync("""
 CREATE TABLE IF NOT EXISTS "FeedbackEntries" (
 "Id" bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,"RequestId" uuid NOT NULL,"Fingerprint" text NOT NULL,"CreatedAt" timestamp with time zone NOT NULL,
 "EmployeeId" integer NOT NULL REFERENCES "Employees"("Id") ON DELETE RESTRICT,"EmployeeNumber" text NOT NULL,"EmployeeName" text NOT NULL,
 "DiningRoomId" integer NOT NULL REFERENCES "DiningRooms"("Id") ON DELETE RESTRICT,"Kind" text NOT NULL,"Message" text NOT NULL,"Status" text NOT NULL,
 "ReceivedBy" text NOT NULL,"Ip" text NOT NULL,"Version" integer NOT NULL DEFAULT 1);
 CREATE UNIQUE INDEX IF NOT EXISTS "IX_FeedbackEntries_RequestId" ON "FeedbackEntries"("RequestId");
 CREATE INDEX IF NOT EXISTS "IX_FeedbackEntries_CreatedAt_Id" ON "FeedbackEntries"("CreatedAt","Id");
 CREATE INDEX IF NOT EXISTS "IX_FeedbackEntries_DiningRoomId_Status" ON "FeedbackEntries"("DiningRoomId","Status");
 CREATE TABLE IF NOT EXISTS "FeedbackFollowUps" (
 "Id" bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,"FeedbackEntryId" bigint NOT NULL REFERENCES "FeedbackEntries"("Id") ON DELETE RESTRICT,
 "OccurredAt" timestamp with time zone NOT NULL,"Actor" text NOT NULL,"Ip" text NOT NULL,"PreviousStatus" text NOT NULL,"Status" text NOT NULL,"Note" text NOT NULL);
 CREATE INDEX IF NOT EXISTS "IX_FeedbackFollowUps_FeedbackEntryId_OccurredAt" ON "FeedbackFollowUps"("FeedbackEntryId","OccurredAt");
 """);
}
