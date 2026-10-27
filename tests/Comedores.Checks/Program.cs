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
 async Task<string> Register(string number,string ip,Guid id,string time="2026-10-19T14:30:00Z") {await using var db=Db();return await Service(db,time).Register(number,ip,"pruebas",id);}
 foreach(var invalid in new[]{"", "12a34", "123456", "12 34", "-123", "1.2", "１２３", "123\n"})
  Check((await Register(invalid,"10.0.0.1",Guid.NewGuid()))==EmployeeNumber.ValidationMessage,"Se rechaza número inválido sin registrar consumo");
 Check(EmployeeNumber.IsValid("1") && EmployeeNumber.IsValid("00123") && EmployeeNumber.IsValid("41560"),"Se aceptan de uno a cinco dígitos y se conservan ceros iniciales");
 var request=Guid.NewGuid();
 await Register("40001","10.0.0.1",request);
 await Register("40001","10.0.0.1",request);
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
 Check((await Register("40001","10.0.0.1",Guid.NewGuid(),"2026-10-19T18:00:00Z")).Contains("No hay"),"Fuera de horario rechazado");
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
