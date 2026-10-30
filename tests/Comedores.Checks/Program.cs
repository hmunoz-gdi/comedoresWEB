using Comedores.Web;
using Comedores.Catalogo;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using Npgsql;
using System.Text.Json;
var name="comedores_check_"+Guid.NewGuid().ToString("N");
var cs=$"Host=127.0.0.1;Username=comedores;Database={name}";
var options=new DbContextOptionsBuilder<AppDb>().UseNpgsql(cs).Options;
AppDb Db()=>new(options);
var checks=0;
void Check(bool condition,string title){if(!condition)throw new Exception(title);checks++;Console.WriteLine("PASS: "+title);}
MealService Service(AppDb db,string time="2026-10-19T14:30:00Z")=>new(db,new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Demo:Now",time}}).Build(),new DevEnvironment());
try {
 await using(var db=Db()) {
  await db.Database.EnsureCreatedAsync();
  db.DiningRooms.AddRange(new DiningRoom{Id=1,Name="SUBESTACION"},new DiningRoom{Id=2,Name="PLATAFORMA"});
  db.Terminals.AddRange(new Terminal{Ip="10.0.0.1",DiningRoomId=1},new Terminal{Ip="10.0.0.2",DiningRoomId=2});
  db.MealWindows.AddRange(new MealWindow{DiningRoomId=1,Type="DYN",Start=new(8,0),End=new(11,0)},new MealWindow{DiningRoomId=2,Type="DYN",Start=new(8,0),End=new(11,0)});
  db.Employees.AddRange(new Employee{Id=1,Number="40001",Name="Prueba",Active=true},new Employee{Id=2,Number="49001",Name="Prueba especial",Active=true,SpecialShift=true},new Employee{Id=3,Number="30001",Name="Prueba inactiva",Active=false});
  await db.SaveChangesAsync();
 }
 // En estos casos existentes se simula la aceptación explícita del operador cuando aparece un aviso.
 async Task<string> Register(string number,string ip,Guid id,string time="2026-10-19T14:30:00Z") {
  await using var db=Db();var service=Service(db,time);var result=await service.RegisterDetailed(number,ip,"pruebas",id);
  while(result.NeedsConfirmation) result=await service.RegisterDetailed(number,ip,"pruebas",id,result.SegmentCount,result.ServiceType);
  return result.Message;
 }
 foreach(var invalid in new[]{"", "12a34", "123456", "12 34", "-123", "1.2", "１２３", "123\n"})
  Check((await Register(invalid,"10.0.0.1",Guid.NewGuid()))==EmployeeNumber.ValidationMessage,"Se rechaza número inválido sin registrar consumo");
 Check(EmployeeNumber.IsValid("1") && EmployeeNumber.IsValid("00123") && EmployeeNumber.IsValid("41560"),"Se aceptan de uno a cinco dígitos y se conservan ceros iniciales");
 var request=Guid.NewGuid();
 await Register("40001","10.0.0.1",request);
 await Register("40001","10.0.0.1",request);
 Check((await Register("40001","10.0.0.1",request,"2026-10-19T18:00:00Z")).Contains("no se duplicó"),"Un reintento de una solicitud guardada se reconoce aun después de cerrar el horario");
 await Register("40001","10.0.0.2",Guid.NewGuid());
 await using(var db=Db()) {
  var rows=await db.Consumptions.OrderBy(x=>x.DailySequence).ToListAsync();
  Check(rows.Count==2,"Reenvío idempotente sin duplicados");
  Check(rows[0].DailySequence==1 && rows[1].DailySequence==2 && rows[1].DiningRoomId==2,"Conteo entre comedores y repetición del mismo tipo");
 }
 await Task.WhenAll(Enumerable.Range(0,5).Select(_=>Register("40001","10.0.0.1",Guid.NewGuid())));
 await using(var db=Db()) Check((await db.Consumptions.Where(x=>x.EmployeeId==1).Select(x=>x.DailySequence).OrderBy(x=>x).ToListAsync()).SequenceEqual(Enumerable.Range(1,7)),"Registros simultáneos sin perder consecutivos");
 await Register("49001","10.0.0.1",Guid.NewGuid());await Register("49001","10.0.0.1",Guid.NewGuid());
 await using(var db=Db()) Check(await db.Consumptions.AnyAsync(x=>x.EmployeeId==2 && x.SpecialShift && x.DailySequence==2),"Adicional de turno especial identificado sin importes");
 await Register("40001","10.0.0.1",Guid.NewGuid(),"2026-10-20T14:30:00Z");
 await using(var db=Db()) Check(await db.Consumptions.AnyAsync(x=>x.ServiceDate==new DateOnly(2026,10,20)&&x.DailySequence==1),"Nuevo día reinicia el consecutivo");
 Check((await Register("40001","10.0.0.9",Guid.NewGuid())).Contains("Equipo sin"),"IP desconocida rechazada");
 Check((await Register("30001","10.0.0.1",Guid.NewGuid())).Contains("no tiene habilitado"),"Empleado inactivo rechazado");
 Check((await Register("99999","10.0.0.1",Guid.NewGuid())).Contains("no está"),"Empleado inexistente rechazado");
 Check((await Register("40001","10.0.0.1",Guid.NewGuid(),"2026-10-19T18:00:00Z")).Contains("Fuera del horario"),"Fuera de horario rechazado");
 await using(var db=Db()) {
  var outside=await Service(db,"2026-10-19T18:00:00Z").RegisterDetailed("40001","10.0.0.1","pruebas",Guid.NewGuid());
  Check(outside.OutsideHours && !outside.Ok && !outside.NeedsConfirmation,"Fuera de horario devuelve una causa explícita para el diálogo");
 }
 Check((await Register("40001","10.0.0.1",Guid.NewGuid(),"2026-10-19T14:00:00Z")).Contains("Servicio registrado"),"Inicio de horario incluido");
 Check((await Register("40001","10.0.0.1",Guid.NewGuid(),"2026-10-19T17:00:00Z")).Contains("Servicio registrado"),"Fin de horario incluido");
 Check(new Employee{Number="39999"}.Category=="Corporativo" && new Employee{Number="40000"}.Category=="Operativo" && new Employee{Number="48999"}.Category=="Operativo" && new Employee{Number="49000"}.Category=="Mando","Fronteras de categorías confirmadas");
 await using(var db=Db()) {
  var report=new Comedores.Web.Pages.ReportsModel(db,Service(db)){From=new(2026,10,19),To=new(2026,10,19),RoomId=2,Category="Operativo"};
  await report.OnGetAsync();
  Check(report.Rows.Count==1 && report.Rows[0].DailySequence==2,"Reporte filtrado conserva adicional generado en otro comedor");
 }
 var privateRoot=Path.Combine(Path.GetTempPath(),"comedores_photos_"+Guid.NewGuid().ToString("N"));
 var env=new DevEnvironment {ContentRootPath=Path.Combine(privateRoot,"src","app")};
 var local=LocalProfile.LocalDirectory(env);
 Directory.CreateDirectory(Path.Combine(local,"fotos"));
 try {
  var png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRn8AAAAASUVORK5CYII=");
  await File.WriteAllBytesAsync(Path.Combine(local,"fotos","40001.png"),png);
  var photos=new EmployeePhotos(env);
  Check(photos.Find("40001")!=null && EmployeePhotos.ContentType(png)=="image/png","Foto PNG local encontrada y tipo detectado");
  var external=Path.Combine(privateRoot,"shared");Directory.CreateDirectory(external);
  await File.WriteAllBytesAsync(Path.Combine(external,"49001.png"),png);
  var configured=new EmployeePhotos(env,new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"EmployeePhotos:Directory",external}}).Build());
  Check(configured.Find("49001")!=null && configured.Find("40001")==null,"Carpeta de fotos configurable usa el directorio indicado");
  Check(photos.Find("../../40001")==null && photos.Find("99999")==null,"Ruta ajena y foto inexistente no se exponen");
  Check(EmployeePhotos.ContentType(System.Text.Encoding.UTF8.GetBytes("<script>no</script>"))==null,"Archivo ajeno a imagen rechazado");
  await using var db=Db();
  var original=await db.Employees.SingleAsync(x=>x.Number=="40001");
  await File.WriteAllTextAsync(Path.Combine(local,"empleado.json"),JsonSerializer.Serialize(new LocalProfile.Profile {Number="40001",Name="No sobrescribir",WorkSchedule="Prueba",EnabledForDemo=true}));
  await LocalProfile.ImportAsync(db,env);
  Check(original.Name=="Prueba" && await db.Employees.CountAsync(x=>x.Number=="40001")==1,"Perfil local no sobrescribe ni duplica empleados existentes");
  await db.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"Employees\"','Id'),(SELECT MAX(\"Id\") FROM \"Employees\"))");
  await File.WriteAllTextAsync(Path.Combine(local,"empleado.json"),JsonSerializer.Serialize(new LocalProfile.Profile {Number="41560",Name="Empleado de prueba",Area="Prueba",Position="Prueba",WorkSchedule="Lunes a viernes 09:00 a 17:00",EnabledForDemo=true}));
  await LocalProfile.ImportAsync(db,env); await LocalProfile.ImportAsync(db,env);
  Check(await db.Employees.CountAsync(x=>x.Number=="41560" && x.Active==true)==1,"Perfil local agrega empleado en base existente sin duplicar");
 } finally {Directory.Delete(privateRoot,true);}
 await using(var db=Db()) {
  db.DiningRooms.Add(new DiningRoom {Id=3,Name="CREI"});
  db.Terminals.Add(new Terminal {Ip="10.0.0.3",DiningRoomId=3});
  foreach(var room in new[]{1,2,3}) {
   if(room==3)db.MealWindows.Add(new MealWindow{DiningRoomId=room,Type="DYN",Start=new(8,0),End=new(11,0)});
   db.MealWindows.AddRange(new MealWindow{DiningRoomId=room,Type="COM",Start=new(14,0),End=new(18,0)},new MealWindow{DiningRoomId=room,Type="CEN",Start=room==3?new(19,0):new(20,30),End=new(22,0)});
  }
  await db.SaveChangesAsync();
 }
 DateTime UtcAt(TimeOnly time,int day=21)=>new DateTimeOffset(2026,10,day,time.Hour,time.Minute,time.Second,TimeSpan.FromHours(-6)).UtcDateTime;
 foreach(var room in new[]{1,2,3}) {
  await using var db=Db();
  foreach(var window in await db.MealWindows.Where(x=>x.DiningRoomId==room).ToListAsync()) {
   var midpoint=window.Start.Add((window.End-window.Start)/2);
   bool correct=true;
   foreach(var localTime in new[]{window.Start,midpoint,window.End}) {
    var utc=UtcAt(localTime);var id=Guid.NewGuid();
    var message=await Register("40001",$"10.0.0.{room}",id,utc.ToString("O"));
    var saved=await db.Consumptions.AsNoTracking().SingleOrDefaultAsync(x=>x.RequestId==id);
    correct &= message.Contains(MealTypes.Name(window.Type)) && message.Contains(localTime.ToString("HH:mm:ss")) && saved!=null && saved.Type==window.Type && saved.OccurredAt==utc && saved.ServiceDate==new DateOnly(2026,10,21);
   }
   Check(correct,$"Comedor {room}, {window.Type}: inicio, interior y fin guardan tipo y hora exactos");
   var before=await db.Consumptions.CountAsync();
   var early=await Register("40001",$"10.0.0.{room}",Guid.NewGuid(),UtcAt(window.Start.Add(TimeSpan.FromSeconds(-1))).ToString("O"));
   var late=await Register("40001",$"10.0.0.{room}",Guid.NewGuid(),UtcAt(window.End.Add(TimeSpan.FromSeconds(1))).ToString("O"));
   Check(early.Contains("Fuera del horario") && late.Contains("Fuera del horario") && await db.Consumptions.CountAsync()==before,$"Comedor {room}, {window.Type}: fuera de rango no guarda consumos");
  }
 }
 await using(var db=Db()) {
  var report=new Comedores.Web.Pages.ReportsModel(db,Service(db)){From=new(2026,10,21),To=new(2026,10,21)};
  await report.OnGetAsync();
  Check(report.Rows.Count==27 && new[]{"DYN","COM","CEN"}.All(type=>report.Rows.Count(x=>x.Type==type)==9),"Reporte conserva tipos y las 27 horas registradas en tres comedores");
  var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Demo:Now",UtcAt(new(10,0),22).ToString("O")}}).Build();
  var changing=new MealService(db,config,new DevEnvironment());
  var consulted=await changing.PeriodAtAsync(1,changing.Now);
  config["Demo:Now"]=UtcAt(new(14,10),22).ToString("O");
  var id=Guid.NewGuid();await changing.Register("40001","10.0.0.1","pruebas",id);
  var saved=await db.Consumptions.SingleAsync(x=>x.RequestId==id);
  Check(consulted.Window?.Type=="DYN" && saved.Type=="COM" && saved.OccurredAt==UtcAt(new(14,10),22),"Se clasifica al confirmar, aunque se haya consultado al empleado en otro horario");
  var real=new MealService(db,new ConfigurationBuilder().Build(),new DevEnvironment());
  Check(!real.IsSimulated && Math.Abs((real.Now-DateTime.UtcNow).TotalSeconds)<5,"Sin simulación explícita se utiliza hora real UTC");
  var overlap=new MealWindow {DiningRoomId=3,Type="COM",Start=new(8,30),End=new(9,30)};db.MealWindows.Add(overlap);await db.SaveChangesAsync();
  var overlapMessage=await Register("40001","10.0.0.3",Guid.NewGuid(),UtcAt(new(9,0)).ToString("O"));
  Check(overlapMessage.Contains("superponen"),"Horarios superpuestos no clasifican arbitrariamente el consumo");
  db.MealWindows.Remove(overlap);await db.SaveChangesAsync();
 }
 await using(var db=Db()) {
  var morning=Service(db,UtcAt(new(9,0),23).ToString("O"));
  var first=await morning.RegisterDetailed("40001","10.0.0.1","pruebas",Guid.NewGuid());
  Check(first.Ok && !first.NeedsConfirmation,"Primer servicio del segmento no requiere aviso de repetición");
  var request2=Guid.NewGuid();
  var warning=await morning.RegisterDetailed("40001","10.0.0.1","pruebas",request2);
  Check(warning.NeedsConfirmation && warning.SegmentCount==1 && warning.ServiceType=="DYN" && !await db.Consumptions.AnyAsync(x=>x.RequestId==request2),"Aviso informa un desayuno previo; cancelar no guarda otro consumo");
  var accepted=await morning.RegisterDetailed("40001","10.0.0.1","pruebas",request2,warning.SegmentCount,warning.ServiceType);
  Check(accepted.Ok && await db.Consumptions.AnyAsync(x=>x.RequestId==request2 && x.DailySequence==2),"Continuar guarda el segundo servicio y conserva el consecutivo diario");
  var staleId=Guid.NewGuid();
  var stale=await morning.RegisterDetailed("40001","10.0.0.1","pruebas",staleId,1,"DYN");
  var wrongType=await morning.RegisterDetailed("40001","10.0.0.1","pruebas",staleId,2,"COM");
  Check(stale.NeedsConfirmation && stale.SegmentCount==2 && wrongType.NeedsConfirmation && !await db.Consumptions.AnyAsync(x=>x.RequestId==staleId),"Confirmación antigua o de otro segmento vuelve a pedir autorización sin guardar");
  var elsewhere=await morning.RegisterDetailed("40001","10.0.0.2","pruebas",Guid.NewGuid());
  Check(elsewhere.NeedsConfirmation && elsewhere.SegmentCount==2,"Aviso cuenta servicios del segmento en todos los comedores");
  var replay=await morning.RegisterDetailed("40001","10.0.0.1","pruebas",request2);
  Check(replay.Ok && !replay.NeedsConfirmation,"Reintentar la misma solicitud guardada no pide confirmación ni duplica");
  var lunch=await Service(db,UtcAt(new(15,0),23).ToString("O")).RegisterDetailed("40001","10.0.0.1","pruebas",Guid.NewGuid());
  Check(lunch.Ok && !lunch.NeedsConfirmation,"Primera comida tras dos desayunos es otro segmento y no avisa repetición");
  await morning.RegisterDetailed("49001","10.0.0.1","pruebas",Guid.NewGuid());
  var special=await morning.RegisterDetailed("49001","10.0.0.1","pruebas",Guid.NewGuid());
  Check(special.NeedsConfirmation && special.SegmentCount==1,"Turno especial también recibe aviso al repetir segmento, sin calcular cobros");
 }
 var catalogRoot=Path.Combine(Path.GetTempPath(),"comedores_catalog_"+Guid.NewGuid().ToString("N"));
 Directory.CreateDirectory(catalogRoot);
 try {
  var csvPath=Path.Combine(catalogRoot,"fixture.csv");
  const string header="EMP_REG;EMP_Num;EMP_Nombre;EMP_Apaterno;EMP_Amaterno;EMP_Area;EMP_Puesto;EMP_Horario_Laboral;EMP_TurnoEspecial;EMP_STATUS\n";
  await File.WriteAllTextAsync(csvPath,header+"1;41560;Nombre;Muñoz;Prueba;\"Área; sistemas\";Puesto;Horario;NO;1\n2;49010;Otra;Persona;;Área;Puesto;Horario;SI;0\n3;;;;;;;;NO;1\n4;45001;;;;Área;Puesto;;NO;1\n");
  var parsed=Catalog.Read(csvPath);
  Check(parsed.Employees.Count==3 && parsed.Active==2 && parsed.Inactive==1 && parsed.MissingNumber==1 && parsed.MissingName==1 && parsed.Employees[0].Area=="Área; sistemas","CSV maneja acentos, campos citados, bajas y exclusiones explícitas");
  await File.WriteAllTextAsync(csvPath,header+"1;123;Prueba;;;;;Horario;NO;1\n2;123;Duplicado;;;;;Horario;NO;1\n");
  try {Catalog.Read(csvPath);Check(false,"CSV duplicado debe rechazarse");}catch(InvalidDataException){Check(true,"CSV duplicado se rechaza antes de tocar la base");}
  await File.WriteAllTextAsync(csvPath,header+"1;123;Prueba;;;;;Horario;NO;desconocido\n");
  try {Catalog.Read(csvPath);Check(false,"Estatus desconocido debe rechazarse");}catch(InvalidDataException){Check(true,"Estatus desconocido no se convierte a activo silenciosamente");}
  int oldMeals,oldEmployees;string protectedBefore;
  async Task<string> Snapshot(AppDb db)=>JsonSerializer.Serialize(new {Users=await db.Users.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Rooms=await db.DiningRooms.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Terminals=await db.Terminals.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Ip,x.DiningRoomId}).ToListAsync(),Windows=await db.MealWindows.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
  await using(var db=Db()) {
   db.Users.Add(new IdentityUser {Id="keep-user",UserName="operador-prueba",NormalizedUserName="OPERADOR-PRUEBA",PasswordHash="fixture-hash"});
   await db.SaveChangesAsync();oldMeals=await db.Consumptions.CountAsync();oldEmployees=await db.Employees.CountAsync();protectedBefore=await Snapshot(db);
  }
  await using(var db=Db()) {
   try {await CatalogReplacement.ReplaceAsync(db,parsed,()=>throw new IOException("Respaldo de prueba fallido"));Check(false,"Respaldo fallido debe bloquear borrado");}
   catch(IOException){Check(await db.Consumptions.CountAsync()==oldMeals && await db.Employees.CountAsync()==oldEmployees,"Si falla el respaldo, empleados y consumos permanecen");}
  }
  await using(var db=Db()) {
   var invalid=new Catalog([new Employee{Number="222"},new Employee{Number="222"}],0,0);
   try {await CatalogReplacement.ReplaceAsync(db,invalid,()=>Task.CompletedTask);Check(false,"Carga fallida debe revertirse");}
   catch(DbUpdateException){await using var verify=Db();Check(await verify.Consumptions.CountAsync()==oldMeals && await verify.Employees.CountAsync()==oldEmployees,"Fallo después de borrar revierte empleados y consumos");}
  }
  var backupPath=Path.Combine(catalogRoot,"backup.dump");
  await using(var db=Db()) {
   await CatalogReplacement.ReplaceAsync(db,parsed,()=>Backup.CreateAsync(new NpgsqlConnectionStringBuilder(cs),backupPath));
   Check(await db.Consumptions.CountAsync()==0 && await db.Employees.CountAsync()==3,"Reinicio elimina consumos e importa catálogo completo");
   Check(await Snapshot(db)==protectedBefore,"Reinicio conserva cuentas, hash, comedores, terminales y horarios");
  }
  var restoreName=name+"_restore";
  await using(var admin=new NpgsqlConnection("Host=127.0.0.1;Username=comedores;Database=postgres")) {
   await admin.OpenAsync();await using var command=new NpgsqlCommand($"CREATE DATABASE \"{restoreName}\"",admin);await command.ExecuteNonQueryAsync();
  }
  var restoreOptions=new DbContextOptionsBuilder<AppDb>().UseNpgsql($"Host=127.0.0.1;Username=comedores;Database={restoreName}").Options;
  await using var restored=new AppDb(restoreOptions);
  try {
   var binary=Path.Combine(Environment.GetEnvironmentVariable("COMEDORES_PG_BIN")??"","pg_restore");
   var psi=new ProcessStartInfo(binary){RedirectStandardInput=true,RedirectStandardError=true,RedirectStandardOutput=true};
   foreach(var argument in new[]{"--exit-on-error","--no-owner","--no-privileges","--host","127.0.0.1","--username","comedores","--dbname",restoreName})psi.ArgumentList.Add(argument);
   using var process=Process.Start(psi)!;var errors=process.StandardError.ReadToEndAsync();var output=process.StandardOutput.ReadToEndAsync();
   await using(var source=File.OpenRead(backupPath)){await source.CopyToAsync(process.StandardInput.BaseStream);}process.StandardInput.Close();await process.WaitForExitAsync();await errors;await output;
   Check(process.ExitCode==0 && await restored.Employees.CountAsync()==oldEmployees && await restored.Consumptions.CountAsync()==oldMeals && await Snapshot(restored)==protectedBefore,"Respaldo se restaura en otra base y recupera datos previos y cuenta");
  } finally {await restored.Database.EnsureDeletedAsync();}
  var realCsv=Environment.GetEnvironmentVariable("COMEDORES_TEST_CSV");
  if(!string.IsNullOrEmpty(realCsv)) {
   var full=Catalog.Read(realCsv);
   await using var target=Db();
   await CatalogReplacement.ReplaceAsync(target,full,()=>Backup.CreateAsync(new NpgsqlConnectionStringBuilder(cs),Path.Combine(catalogRoot,"full-before.dump")));
   Check(await target.Employees.CountAsync()==full.Employees.Count && await target.Employees.CountAsync(x=>x.Active==true)==full.Active && !await target.Consumptions.AnyAsync() && await Snapshot(target)==protectedBefore,"Carga completa del CSV en base temporal conserva configuración y coincide en totales");
  }
 } finally {Directory.Delete(catalogRoot,true);}
 Console.WriteLine($"{checks} comprobaciones completadas.");
} finally { await using var db=Db(); await db.Database.EnsureDeletedAsync(); NpgsqlConnection.ClearAllPools(); }
class DevEnvironment:IWebHostEnvironment {
 public string EnvironmentName{get;set;}="Development";public string ApplicationName{get;set;}="Checks";public string WebRootPath{get;set;}="";public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();public string ContentRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
}
