using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
[RequestSizeLimit(65536)]
public class FeedbackModel(AppDb db,MealService meals,FeedbackService feedback,EmployeePhotos photos):PageModel {
 [BindProperty,Required(ErrorMessage="Escribe tu número de empleado."),RegularExpression("^[0-9]{1,5}$",ErrorMessage="El número admite de 1 a 5 dígitos.")] public string Number{get;set;}="";
 [BindProperty,Required(ErrorMessage="Selecciona el tipo de comentario.")] public string Kind{get;set;}="";
 [BindProperty,Required(ErrorMessage="Selecciona el tema de tu comentario.")] public string Topic{get;set;}="";
 [BindProperty,Required(ErrorMessage="Escribe tu comentario."),StringLength(500,MinimumLength=10,ErrorMessage="El comentario debe tener entre 10 y 500 caracteres.")] public string Message{get;set;}="";
 [BindProperty] public Guid RequestId{get;set;}=Guid.NewGuid();
 [BindProperty(SupportsGet=true)] public int? RoomId{get;set;}
 [TempData] public string? Notice{get;set;}
 public bool Manual=>FeedbackService.CanManage(User);
 public string RoomName{get;set;}="Sin comedor autorizado";
 public List<DiningRoom> Rooms{get;set;}=[];
 public async Task<IActionResult> OnGetLookupAsync(string number){
  if(!FeedbackService.CanSubmit(User))return Forbid();
  var denied=await meals.OperatorAccessErrorAsync(User,meals.ResolveIp(HttpContext.Connection.RemoteIpAddress));
  if(denied!=null)return new JsonResult(new{ok=false,message=denied});
  if(!EmployeeNumber.IsValid(number??""))return new JsonResult(new{ok=false,message=EmployeeNumber.ValidationMessage});
  var employee=await db.Employees.AsNoTracking().SingleOrDefaultAsync(e=>e.Number==number);
  if(employee==null)return new JsonResult(new{ok=false,message="Empleado no encontrado en el catálogo."});
  if(employee.Active!=true)return new JsonResult(new{ok=false,message="Empleado dado de baja. Solicita al operador revisar el catálogo."});
  return new JsonResult(new{ok=true,number=employee.Number,name=employee.Name,photoUrl=Url.Page("/Feedback","Photo",new{number=employee.Number})});
 }
 public async Task<IActionResult> OnGetPhotoAsync(string number){
  Response.Headers.CacheControl="no-store, private";Response.Headers["X-Content-Type-Options"]="nosniff";
  if(!FeedbackService.CanSubmit(User))return Forbid();
  if(await meals.OperatorAccessErrorAsync(User,meals.ResolveIp(HttpContext.Connection.RemoteIpAddress))!=null)return Forbid();
  var path=photos.Find(number??"");
  if(path==null||!await db.Employees.AnyAsync(e=>e.Number==number))return NotFound();
  try{var bytes=await System.IO.File.ReadAllBytesAsync(path);var type=EmployeePhotos.ContentType(bytes);return type==null?NotFound():File(bytes,type);}
  catch(IOException){return NotFound();}catch(UnauthorizedAccessException){return NotFound();}
 }
 public async Task OnGetAsync(){await LoadAsync();}
 async Task LoadAsync(){
  if(Manual)Rooms=await db.DiningRooms.OrderBy(r=>r.Name).ToListAsync();
  else RoomName=(await meals.Terminal(meals.ResolveIp(HttpContext.Connection.RemoteIpAddress)))?.DiningRoom.Name??RoomName;
 }
 public async Task<IActionResult> OnPostAsync(){
  if(!FeedbackService.CanSubmit(User))return Forbid();
  if(!ModelState.IsValid){await LoadAsync();return Page();}
  var result=await feedback.SubmitAsync(User,meals.ResolveIp(HttpContext.Connection.RemoteIpAddress),RequestId,Number,Kind,Topic,Message,RoomId);
  if(!result.Ok){ModelState.AddModelError("",result.Message);await LoadAsync();return Page();}
  Notice=result.Message;return RedirectToPage(new{RoomId=Manual?RoomId:null});
 }
}
