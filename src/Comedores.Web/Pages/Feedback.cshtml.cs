using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
[RequestSizeLimit(65536)]
public class FeedbackModel(AppDb db,MealService meals,FeedbackService feedback):PageModel {
 [BindProperty,Required(ErrorMessage="Escribe tu número de empleado."),RegularExpression("^[0-9]{1,5}$",ErrorMessage="El número admite de 1 a 5 dígitos.")] public string Number{get;set;}="";
 [BindProperty,Required(ErrorMessage="Selecciona el tipo de comentario.")] public string Kind{get;set;}="";
 [BindProperty,Required(ErrorMessage="Selecciona el tema de tu comentario.")] public string Topic{get;set;}="";
 [BindProperty,Required(ErrorMessage="Escribe tu comentario."),StringLength(3000,MinimumLength=10,ErrorMessage="El comentario debe tener entre 10 y 3000 caracteres.")] public string Message{get;set;}="";
 [BindProperty] public Guid RequestId{get;set;}=Guid.NewGuid();
 [BindProperty(SupportsGet=true)] public int? RoomId{get;set;}
 [TempData] public string? Notice{get;set;}
 public bool Manual=>FeedbackService.CanManage(User);
 public string RoomName{get;set;}="Sin comedor autorizado";
 public List<DiningRoom> Rooms{get;set;}=[];
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
