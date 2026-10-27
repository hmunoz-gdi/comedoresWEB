using System.Diagnostics;
using Npgsql;
namespace Comedores.Catalogo;
public static class Backup {
 public static async Task CreateAsync(NpgsqlConnectionStringBuilder connection,string path) {
  Directory.CreateDirectory(Path.GetDirectoryName(path)!);
  if(!OperatingSystem.IsWindows())File.SetUnixFileMode(Path.GetDirectoryName(path)!,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
  var binaryFolder=Environment.GetEnvironmentVariable("COMEDORES_PG_BIN");
  string Binary(string name)=>string.IsNullOrWhiteSpace(binaryFolder)?name:Path.Combine(binaryFolder,name+(OperatingSystem.IsWindows()?".exe":""));
  ProcessStartInfo Start(string name) {
   var start=new ProcessStartInfo(Binary(name)){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true};
   start.Environment["PGPASSWORD"]=connection.Password??"";
   start.Environment["PGCONNECT_TIMEOUT"]="10";
   return start;
  }
  var dump=Start("pg_dump");
  foreach(var argument in new[]{"--no-password","--format=custom","--host",connection.Host??"127.0.0.1","--port",connection.Port.ToString(),"--username",connection.Username!,"--dbname",connection.Database!})dump.ArgumentList.Add(argument);
  try {
   using(var process=Process.Start(dump)??throw new InvalidOperationException("No se pudo iniciar pg_dump.")) {
    var errors=process.StandardError.ReadToEndAsync();
    await using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
     if(!OperatingSystem.IsWindows())File.SetUnixFileMode(path,UnixFileMode.UserRead|UnixFileMode.UserWrite);
     await process.StandardOutput.BaseStream.CopyToAsync(output);
    }
    await process.WaitForExitAsync();await errors;
    if(process.ExitCode!=0)throw new InvalidOperationException("Falló el respaldo. No se borró ningún registro.");
   }
   await using(var source=File.OpenRead(path)) {
    var magic=new byte[5];await source.ReadExactlyAsync(magic);
    if(!magic.SequenceEqual("PGDMP"u8.ToArray()) || source.Length<100)throw new InvalidOperationException("El respaldo no es válido.");
   }
   var list=Start("pg_restore");list.RedirectStandardInput=true;list.ArgumentList.Add("--list");
   using(var process=Process.Start(list)??throw new InvalidOperationException("No se pudo comprobar el respaldo.")) {
    var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();
    await using(var source=File.OpenRead(path)) {await source.CopyToAsync(process.StandardInput.BaseStream);}
    process.StandardInput.Close();await process.WaitForExitAsync();
    var toc=await output;await errors;
    if(process.ExitCode!=0 || !toc.Contains("TABLE DATA public AspNetUsers") || !toc.Contains("TABLE DATA public Employees"))throw new InvalidOperationException("No se pudo verificar el índice del respaldo. No se borró ningún registro.");
   }
   Console.WriteLine("Respaldo creado e índice comprobado antes de limpiar.");
  } catch {
   if(File.Exists(path))File.Move(path,path+".incompleto");
   throw;
  }
 }
}
