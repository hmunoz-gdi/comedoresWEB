using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
[RequestSizeLimit(6*1024*1024)]
public class EmployeesModel(AppDb db, EmployeePhotos photos, ILogger<EmployeesModel> logger):PageModel {
 [BindProperty(SupportsGet=true)] public string? Number{get;set;}
 [BindProperty] public Input Data{get;set;}=new();
 [BindProperty] public string? Original{get;set;}
 public Employee? Employee{get;set;}
 static string Version(Employee e)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new{e.Number,e.Name,e.Area,e.Position,e.WorkSchedule,e.Active,e.SpecialShift}))));
 [TempData] public string? Message{get;set;}
 public sealed class Input {
  [Required,RegularExpression("^[0-9]{1,5}$",ErrorMessage="Solo números, máximo 5 dígitos.")] public string Number{get;set;}="";
  [Required,StringLength(150)] public string Name{get;set;}="";
  [Required,StringLength(500)] public string Area{get;set;}="";
  [Required,StringLength(150)] public string Position{get;set;}="";
  [Required,StringLength(150)] public string WorkSchedule{get;set;}="";
  public bool SpecialShift{get;set;}
  public bool Active{get;set;}=true;
 }
 public async Task OnGetAsync(){
  if(string.IsNullOrEmpty(Number))return;
  if(!EmployeeNumber.IsValid(Number)){Message="Solo números, máximo 5 dígitos.";return;}
  Employee=await db.Employees.AsNoTracking().SingleOrDefaultAsync(e=>e.Number==Number);
  if(Employee==null){Data.Number=Number;Message="Empleado no encontrado. Puedes darlo de alta con Nuevo.";return;}
  Original=Version(Employee);
  Data=new(){Number=Employee.Number,Name=Employee.Name,Area=Employee.Area,Position=Employee.Position,WorkSchedule=Employee.WorkSchedule,SpecialShift=Employee.SpecialShift,Active=Employee.Active==true};
 }
 public async Task<IActionResult> OnPostSaveAsync(string operation){
  ModelState.Remove(nameof(Number));
  if(operation!="create"&&operation!="update")return BadRequest();
  if(!ModelState.IsValid){Message="Revisa los campos obligatorios y sus longitudes.";Employee=await db.Employees.AsNoTracking().SingleOrDefaultAsync(e=>e.Number==Data.Number);return Page();}
  await using var tx=await db.Database.BeginTransactionAsync();
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({Data.Number}))");
  var employee=await db.Employees.SingleOrDefaultAsync(e=>e.Number==Data.Number);
  if(operation=="create"&&employee!=null){Message="Ese número ya existe. Consulta al empleado para modificarlo.";return RedirectToPage(new{Number=Data.Number});}
  if(operation=="update"&&employee==null){Message="El empleado ya no existe. Consulta nuevamente.";return RedirectToPage(new{Number=Data.Number});}
  if(operation=="update"&&Original!=Version(employee!)){Message="Otro usuario cambió este empleado. Consulta nuevamente antes de modificarlo.";return RedirectToPage(new{Number=Data.Number});}
  if(employee==null){employee=new(){Number=Data.Number};db.Employees.Add(employee);}
  employee.Name=Data.Name.Trim();employee.Area=Data.Area.Trim();employee.Position=Data.Position.Trim();employee.WorkSchedule=Data.WorkSchedule.Trim();employee.SpecialShift=Data.SpecialShift;employee.Active=Data.Active;
  if(new[]{employee.Name,employee.Area,employee.Position,employee.WorkSchedule}.Any(string.IsNullOrWhiteSpace)){Message="No se permiten campos vacíos.";return RedirectToPage(new{Number=Data.Number});}
  try{await db.SaveChangesAsync();await tx.CommitAsync();}
  catch(DbUpdateException ex){logger.LogWarning(ex,"No se pudo guardar el empleado");Message="No fue posible guardar. Consulta nuevamente antes de reintentar.";return RedirectToPage(new{Number=Data.Number});}
  Message=operation=="create"?"Empleado registrado. Ya puedes consultarlo en Registrar servicio. Ahora puedes guardar su fotografía.":"Datos del empleado actualizados.";
  return RedirectToPage(new{Number=Data.Number});
 }
 public async Task<IActionResult> OnPostDeactivateAsync(string employeeNumber){
  if(!EmployeeNumber.IsValid(employeeNumber))return BadRequest();
  await using var tx=await db.Database.BeginTransactionAsync();
  await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({employeeNumber}))");
  var employee=await db.Employees.SingleOrDefaultAsync(e=>e.Number==employeeNumber);
  if(employee!=null&&Original!=Version(employee)){Message="El empleado cambió. Consulta nuevamente antes de darlo de baja.";return RedirectToPage(new{Number=employeeNumber});}
  if(employee!=null){employee.Active=false;await db.SaveChangesAsync();await tx.CommitAsync();Message="Empleado dado de baja. Se conserva su historial de consumos y fotografía.";}
  return RedirectToPage(new{Number=employeeNumber});
 }
 public async Task<IActionResult> OnPostPhotoAsync(string employeeNumber,IFormFile? photo){
  if(!EmployeeNumber.IsValid(employeeNumber)||!await db.Employees.AnyAsync(e=>e.Number==employeeNumber))return BadRequest();
  if(photo==null||photo.Length is <=0 or >5*1024*1024)return new JsonResult(new{ok=false,message="Selecciona o captura una fotografía JPEG de máximo 5 MB."}){StatusCode=400};
  using var ms=new MemoryStream();await photo.CopyToAsync(ms);var bytes=ms.ToArray();
  if(EmployeePhotos.ContentType(bytes)!="image/jpeg"||bytes.Length<4||bytes[^2]!=0xff||bytes[^1]!=0xd9)return new JsonResult(new{ok=false,message="La fotografía debe ser un archivo JPEG válido."}){StatusCode=400};
  try{await photos.SaveAsync(employeeNumber,bytes);return new JsonResult(new{ok=true,message="Fotografía guardada. Ya está disponible en Registrar servicio."});}
  catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){logger.LogWarning(ex,"No fue posible guardar fotografía");return new JsonResult(new{ok=false,message="No se pudo guardar la foto. Revisa la conexión y los permisos de escritura en la carpeta del servidor. Los datos del empleado permanecen guardados; puedes reintentar la foto."}){StatusCode=503};}
 }
}
