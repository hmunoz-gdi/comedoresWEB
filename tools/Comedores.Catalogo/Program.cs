using Comedores.Catalogo;
using Comedores.Web;
using Microsoft.EntityFrameworkCore;
using Npgsql;
if(args.Length!=2 || args[0] is not ("preview" or "reset")) {
 Console.Error.WriteLine("Uso: Comedores.Catalogo preview|reset ruta-del-csv");return 1;
}
try {
 var catalog=Catalog.Read(args[1]);
 Console.WriteLine($"CSV: {catalog.Employees.Count} empleados; {catalog.Active} activos; {catalog.Inactive} bajas.");
 Console.WriteLine($"Excluidos sin número: {catalog.MissingNumber}. Sin nombre en origen: {catalog.MissingName} (no se inventan nombres).");
 if(args[0]=="preview") {Console.WriteLine("Solo revisión: ninguna base fue modificada.");return 0;}
 var cs=Environment.GetEnvironmentVariable("ConnectionStrings__Comedores")??throw new InvalidOperationException("Falta la conexión local.");
 var connection=new NpgsqlConnectionStringBuilder(cs);
 if(connection.Database!="comedores" || connection.Host is not ("127.0.0.1" or "localhost"))
  throw new InvalidOperationException("Este procedimiento solo permite la base local comedores.");
 await using var db=new AppDb(new DbContextOptionsBuilder<AppDb>().UseNpgsql(cs).Options);
 var backupPath=Path.Combine(Directory.GetCurrentDirectory(),".local","backups",$"comedores-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.dump");
 await CatalogReplacement.ReplaceAsync(db,catalog,()=>Backup.CreateAsync(connection,backupPath));
 Console.WriteLine("Operación completada: consumos vacíos y catálogo cargado. Usuarios, permisos, comedores, equipos y horarios conservados.");
 Console.WriteLine("Respaldo: "+backupPath);
 return 0;
} catch(Exception error) {
 // No volcar excepciones de datos/conexión que podrían incluir información personal.
 Console.Error.WriteLine(error is InvalidDataException or FileNotFoundException or InvalidOperationException ? error.Message : "No se pudo completar la operación. No se confirmó ningún cambio. Revise CSV, conexión y utilidades PostgreSQL.");
 return 1;
}
