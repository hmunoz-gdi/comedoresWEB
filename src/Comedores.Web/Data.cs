using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
namespace Comedores.Web;
public class Employee {
 public int Id { get; set; }
 public string Number { get; set; } = "";
 public string Name { get; set; } = "";
 public string Area { get; set; } = "";
 public string Position { get; set; } = "";
 public string WorkSchedule { get; set; } = "";
 public bool SpecialShift { get; set; }
 public bool? Active { get; set; }
 public string Category => int.TryParse(Number, out var n) ? n < 40000 ? "Corporativo" : n < 49000 ? "Operativo" : "Mando" : "Sin clasificar";
}
public class DiningRoom { public int Id { get; set; } public string Name { get; set; } = ""; }
public class Terminal { public int Id { get; set; } public string Ip { get; set; } = ""; public int DiningRoomId { get; set; } public DiningRoom DiningRoom { get; set; } = null!; }
public class MealWindow { public int Id { get; set; } public int DiningRoomId { get; set; } public string Type { get; set; } = ""; public TimeOnly Start { get; set; } public TimeOnly End { get; set; } }
public class Consumption {
 public long Id { get; set; }
 public Guid RequestId { get; set; }
 public int EmployeeId { get; set; }
 public Employee Employee { get; set; } = null!;
 public int DiningRoomId { get; set; }
 public DiningRoom DiningRoom { get; set; } = null!;
 public DateTime OccurredAt { get; set; }
 public DateOnly ServiceDate { get; set; }
 public string Type { get; set; } = "";
 public bool SpecialShift { get; set; }
 public string EmployeeName { get; set; } = "";
 public string Position { get; set; } = "";
 public string Ip { get; set; } = "";
 public string Operator { get; set; } = "";
 public int DailySequence { get; set; }
 public bool IsCancelled {get;set;}
 public DateTime? CancelledAt {get;set;}
 public string? CancelledBy {get;set;}
 public string? CancellationReason {get;set;}
 public bool IsTakeaway=>PickupBatchId.HasValue && CollectedByNumber!=null && Employee.Number!=CollectedByNumber;
 public Guid? PickupBatchId{get;set;}
 public string? CollectedByNumber{get;set;}
 public string? CollectedByName{get;set;}
}
public class PickupBatch {
 public Guid Id{get;set;}
 public string CollectorNumber{get;set;}="";
 public string CollectorName{get;set;}="";
 public int DiningRoomId{get;set;}
 public string Type{get;set;}="";
 public DateTime OccurredAt{get;set;}
 public string Operator{get;set;}="";
 public string Ip{get;set;}="";
 public string Fingerprint{get;set;}="";
 public int Count{get;set;}
}
public class AccountSecurityEvent {
 public long Id{get;set;}
 public DateTime OccurredAt{get;set;}
 public string Account{get;set;}="";
 public string Actor{get;set;}="";
 public string Action{get;set;}="";
 public string Reason{get;set;}="";
}
public class AppDb(DbContextOptions<AppDb> options, IHttpContextAccessor? contextAccessor=null) : IdentityDbContext<IdentityUser>(options) {
 public DbSet<FeedbackEntry> FeedbackEntries=>Set<FeedbackEntry>();
 public DbSet<FeedbackFollowUp> FeedbackFollowUps=>Set<FeedbackFollowUp>();
 public DbSet<AuditLog> AuditLogs=>Set<AuditLog>();
 public DbSet<PickupBatch> PickupBatches=>Set<PickupBatch>();
 public DbSet<AccountSecurityEvent> AccountSecurityEvents=>Set<AccountSecurityEvent>();
 public DbSet<Employee> Employees => Set<Employee>();
 public DbSet<DiningRoom> DiningRooms => Set<DiningRoom>();
 public DbSet<Terminal> Terminals => Set<Terminal>();
 public DbSet<MealWindow> MealWindows => Set<MealWindow>();
 public DbSet<Consumption> Consumptions => Set<Consumption>();
 public override Task<int> SaveChangesAsync(CancellationToken cancellationToken=default)=>SaveChangesAsync(true,cancellationToken);
 public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,CancellationToken cancellationToken=default){
  ChangeTracker.DetectChanges();
  var context=contextAccessor?.HttpContext;
  var entries=context?.User.Identity?.IsAuthenticated==true?ChangeTracker.Entries().Where(e=>e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).Where(e=>e.Entity is Employee or Consumption or PickupBatch or AccountSecurityEvent).Select(e=>(Entry:e,State:e.State,Fields:string.Join("; ",e.Properties.Where(p=>p.IsModified).Select(p=>e.Entity is Employee?$"{p.Metadata.Name}: {p.OriginalValue??"vacío"} → {p.CurrentValue??"vacío"}":p.Metadata.Name)))).ToList():[];
  if(entries.Count==0)return await base.SaveChangesAsync(acceptAllChangesOnSuccess,cancellationToken);
  // La operación y su auditoría se confirman juntas, también cuando el servicio ya abrió una transacción.
  await using var ownTransaction=Database.CurrentTransaction==null?await Database.BeginTransactionAsync(cancellationToken):null;
  var result=await base.SaveChangesAsync(acceptAllChangesOnSuccess,cancellationToken);
  await using var auditDb=new AppDb(new DbContextOptionsBuilder<AppDb>().UseNpgsql(Database.GetDbConnection()).Options);
  await auditDb.Database.UseTransactionAsync(Database.CurrentTransaction!.GetDbTransaction(),cancellationToken);
  var employeeIds=entries.Select(x=>x.Entry.Entity).OfType<Consumption>().Select(c=>c.EmployeeId).Distinct().ToList();
  var employeeNumbers=employeeIds.Count==0?new Dictionary<int,string>():await auditDb.Employees.Where(e=>employeeIds.Contains(e.Id)).ToDictionaryAsync(e=>e.Id,e=>e.Number,cancellationToken);
  var roomIds=entries.Select(x=>x.Entry.Entity).OfType<Consumption>().Select(c=>c.DiningRoomId).Concat(entries.Select(x=>x.Entry.Entity).OfType<PickupBatch>().Select(g=>g.DiningRoomId)).Distinct().ToList();
  var roomNames=roomIds.Count==0?new Dictionary<int,string>():await auditDb.DiningRooms.Where(r=>roomIds.Contains(r.Id)).ToDictionaryAsync(r=>r.Id,r=>r.Name,cancellationToken);
  foreach(var item in entries){
   var log=AuditLog.Request(context!,"","",item.Fields);
   switch(item.Entry.Entity){
    case Employee e:log.Action=item.State==EntityState.Added?"Alta de empleado":e.Active!=true?"Baja de empleado":"Modificación de empleado";log.Target="Empleado "+e.Number;if(item.State==EntityState.Added)log.Detail=$"Nombre: {e.Name} · Área: {e.Area} · Puesto: {e.Position} · Horario: {e.WorkSchedule} · Activo: {e.Active} · Turno especial: {e.SpecialShift}";break;
    case Consumption c:log.Action=item.State==EntityState.Added?"Registro de servicio":"Consumo cancelado";log.Target="Consumo #"+item.Entry.Property("Id").CurrentValue;log.Target+=" · Empleado "+employeeNumbers.GetValueOrDefault(c.EmployeeId,c.EmployeeId.ToString());log.Detail=$"Empleado {employeeNumbers.GetValueOrDefault(c.EmployeeId,c.EmployeeId.ToString())} · {roomNames.GetValueOrDefault(c.DiningRoomId,c.DiningRoomId.ToString())} · {MealTypes.Name(c.Type)}";log.SourceKey=$"consumption:{item.Entry.Property("Id").CurrentValue}:"+(item.State==EntityState.Added?"registro":"cancelacion");break;
    case PickupBatch g:log.Action="Recogida de grupo";log.Target=g.Id.ToString();log.Detail=$"Responsable {g.CollectorNumber} · {g.Count} servicios · {roomNames.GetValueOrDefault(g.DiningRoomId,g.DiningRoomId.ToString())} · {MealTypes.Name(g.Type)}";break;
    case AccountSecurityEvent e:log.Action=e.Action;log.Target=e.Account;log.Detail=e.Reason;log.SourceKey="security:"+item.Entry.Property("Id").CurrentValue;break;
   }
   auditDb.AuditLogs.Add(log);
  }
  await auditDb.SaveChangesAsync(cancellationToken);if(ownTransaction!=null)await ownTransaction.CommitAsync(cancellationToken);return result;
 }
 protected override void OnModelCreating(ModelBuilder b) {
  base.OnModelCreating(b);
  b.Entity<FeedbackEntry>().Ignore(x=>x.Folio);
  b.Entity<FeedbackEntry>().HasIndex(x=>x.RequestId).IsUnique();
  b.Entity<FeedbackEntry>().HasIndex(x=>new{x.CreatedAt,x.Id});
  b.Entity<FeedbackEntry>().HasIndex(x=>new{x.DiningRoomId,x.Status});
  b.Entity<FeedbackEntry>().HasOne<Employee>().WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<FeedbackEntry>().HasOne<DiningRoom>().WithMany().HasForeignKey(x=>x.DiningRoomId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<FeedbackFollowUp>().HasOne<FeedbackEntry>().WithMany().HasForeignKey(x=>x.FeedbackEntryId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<FeedbackFollowUp>().HasIndex(x=>new{x.FeedbackEntryId,x.OccurredAt});
  b.Entity<AuditLog>().HasIndex(x=>x.SourceKey).IsUnique();
  b.Entity<AuditLog>().HasIndex(x=>new{x.OccurredAt,x.Id});
  b.Entity<AuditLog>().HasIndex(x=>new{x.Actor,x.OccurredAt});
  b.Entity<Employee>().HasIndex(x=>x.Number).IsUnique();
  b.Entity<Employee>().Ignore(x=>x.Category);
  b.Entity<Terminal>().HasIndex(x=>x.Ip).IsUnique();
  b.Entity<Consumption>().Ignore(x=>x.IsTakeaway);
  b.Entity<Consumption>().HasQueryFilter(x=>!x.IsCancelled);
  b.Entity<Consumption>().Property(x=>x.IsCancelled).HasDefaultValue(false);
  b.Entity<Consumption>().HasIndex(x=>new{x.DiningRoomId,x.ServiceDate,x.Type});
  b.Entity<Consumption>().HasOne<PickupBatch>().WithMany().HasForeignKey(x=>x.PickupBatchId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Consumption>().HasIndex(x=>x.RequestId).IsUnique();
  b.Entity<Consumption>().HasIndex(x=>new {x.EmployeeId,x.ServiceDate,x.DailySequence}).IsUnique();
 }
}
