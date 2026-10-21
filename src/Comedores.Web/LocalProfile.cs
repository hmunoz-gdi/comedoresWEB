using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web;
public static class LocalProfile {
 public static string LocalDirectory(IWebHostEnvironment env) => Path.GetFullPath(Path.Combine(env.ContentRootPath,"..","..",".local"));
 public static async Task ImportAsync(AppDb db,IWebHostEnvironment env) {
  var path=Path.Combine(LocalDirectory(env),"empleado.json");
  if(!File.Exists(path)) return;
  var profile=JsonSerializer.Deserialize<Profile>(await File.ReadAllTextAsync(path))
      ?? throw new InvalidOperationException("El perfil local está vacío.");
  if(!Regex.IsMatch(profile.Number??"",@"\A[0-9]{1,10}\z") || string.IsNullOrWhiteSpace(profile.Name) || string.IsNullOrWhiteSpace(profile.WorkSchedule))
   throw new InvalidOperationException("Revise número, nombre y horario en .local/empleado.json.");
  if(await db.Employees.AnyAsync(x=>x.Number==profile.Number)) return;
  db.Employees.Add(new Employee {Number=profile.Number!,Name=profile.Name,Area=profile.Area,Position=profile.Position,WorkSchedule=profile.WorkSchedule,SpecialShift=profile.SpecialShift,Active=profile.EnabledForDemo});
  await db.SaveChangesAsync();
 }
 public sealed class Profile {
  public string Number {get;set;}="";
  public string Name {get;set;}="";
  public string Area {get;set;}="";
  public string Position {get;set;}="";
  public string WorkSchedule {get;set;}="";
  public bool SpecialShift {get;set;}
  public bool EnabledForDemo {get;set;}
 }
}
