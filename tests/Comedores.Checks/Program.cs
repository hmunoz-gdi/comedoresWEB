using Comedores.Web;
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
 Console.WriteLine($"{checks} comprobaciones completadas.");
} finally { await using var db=Db(); await db.Database.EnsureDeletedAsync(); NpgsqlConnection.ClearAllPools(); }
class DevEnvironment:IWebHostEnvironment {
 public string EnvironmentName{get;set;}="Development";public string ApplicationName{get;set;}="Checks";public string WebRootPath{get;set;}="";public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();public string ContentRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
}
