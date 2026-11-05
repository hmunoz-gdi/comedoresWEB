using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
public class RegisterModel(AppDb db,MealService meals,EmployeePhotos photos):PageModel {
 [BindProperty(SupportsGet=true)] public string Number {get;set;}="";
 [BindProperty] public Guid RequestId {get;set;}
 [BindProperty] public int? ConfirmedSegmentCount {get;set;}
 [BindProperty] public string? ConfirmedServiceType {get;set;}
 [BindProperty] public string? SelectedServiceType {get;set;}
 [BindProperty] public int? CoordinatorRoomId {get;set;}
 public bool IsCoordinator=>User.IsInRole(AccessProfiles.Coordinator);
 public List<DiningRoom> CoordinatorRooms {get;set;}=[];
 [TempData] public string? Message {get;set;}
 public Employee? Employee {get;set;}
 public string? Room {get;set;}
 public int TodayCount {get;set;}
 public bool HasPhoto {get;set;}
 public bool CanRegister {get;set;}
 public bool OutsideHours {get;set;}
 public bool EmployeeNotFound {get;set;}
 public bool IsSimulated => meals.IsSimulated;
 public string ServerClock {get;set;}="";
 public string ServerDate {get;set;}="";
 public long ServerEpoch {get;set;}
 public string CurrentService {get;set;}="";
 public async Task OnGetAsync() {
  if(IsCoordinator)CoordinatorRooms=await db.DiningRooms.OrderBy(r=>r.Name).ToListAsync();
  var terminal=await meals.Terminal(meals.ResolveIp(HttpContext.Connection.RemoteIpAddress));
  Room=terminal?.DiningRoom.Name;
  var now=meals.Now;
  ServerClock=MealService.Local(now).ToString("HH:mm:ss");
  ServerDate=MealService.Local(now).ToString("dd/MM/yyyy");
  ServerEpoch=new DateTimeOffset(now).ToUnixTimeMilliseconds();
  var period=terminal==null?((MealWindow?)null,(string?)"Equipo sin comedor autorizado.",false):await meals.PeriodAtAsync(terminal.DiningRoomId,now);
  OutsideHours=!IsCoordinator && period.Item3;
  CurrentService=IsCoordinator?"Selección manual (sin restricción de horario)":period.Item1==null?period.Item2!:MealTypes.Name(period.Item1.Type);
  RequestId=Guid.NewGuid();
  if(string.IsNullOrEmpty(Number) && !Request.Query.ContainsKey("Number")) return;
  if(!EmployeeNumber.IsValid(Number)) {Message=EmployeeNumber.ValidationMessage;return;}
  Employee=await db.Employees.SingleOrDefaultAsync(x=>x.Number==Number);
  if(Employee==null) { EmployeeNotFound=true;Message="Empleado no encontrado en el catálogo.";return; }
  CanRegister=Employee.Active==true && (IsCoordinator||(period.Item1!=null && terminal?.DiningRoomId==AccessProfiles.OperatorRoom(User)));
  HasPhoto=photos.Find(Employee.Number)!=null;
  var date=DateOnly.FromDateTime(MealService.Local(meals.Now));
  TodayCount=await db.Consumptions.CountAsync(x=>x.EmployeeId==Employee.Id && x.ServiceDate==date);
 }
 public async Task<IActionResult> OnGetClockAsync() {
  Response.Headers.CacheControl="no-store, private";
  var terminal=await meals.Terminal(meals.ResolveIp(HttpContext.Connection.RemoteIpAddress));
  var windows=terminal==null?new List<MealWindow>():await db.MealWindows.AsNoTracking().Where(x=>x.DiningRoomId==terminal.DiningRoomId).ToListAsync();
  var now=meals.Now;
  return new JsonResult(new {utcMilliseconds=new DateTimeOffset(now).ToUnixTimeMilliseconds(),simulated=meals.IsSimulated,roomConfigured=terminal!=null,windows=windows.Select(w=>new {name=MealTypes.Name(w.Type),known=MealTypes.IsKnown(w.Type),startSeconds=w.Start.ToTimeSpan().TotalSeconds,endSeconds=w.End.ToTimeSpan().TotalSeconds})});
 }
 public async Task<IActionResult> OnGetLookupAsync(string number) {
  Response.Headers.CacheControl="no-store";
  if(!EmployeeNumber.IsValid(number)) return new JsonResult(new {ok=false,message=EmployeeNumber.ValidationMessage});
  var employee=await db.Employees.AsNoTracking().SingleOrDefaultAsync(x=>x.Number==number);
  if(employee is null) return new JsonResult(new {ok=false,notFound=true,message="Empleado no encontrado en el catálogo."});
  var terminal=await meals.Terminal(meals.ResolveIp(HttpContext.Connection.RemoteIpAddress));
  var now=meals.Now;
  var date=DateOnly.FromDateTime(MealService.Local(now));
  var period=terminal==null?((MealWindow?)null,(string?)"Equipo sin comedor autorizado.",false):await meals.PeriodAtAsync(terminal.DiningRoomId,now);
  var count=await db.Consumptions.CountAsync(x=>x.EmployeeId==employee.Id && x.ServiceDate==date);
  return new JsonResult(new {ok=true,number=employee.Number,name=employee.Name,area=employee.Area,position=employee.Position,workSchedule=employee.WorkSchedule,specialShift=employee.SpecialShift,todayCount=count,canRegister=employee.Active==true && (IsCoordinator||(period.Item1!=null && terminal?.DiningRoomId==AccessProfiles.OperatorRoom(User))),outsideHours=!IsCoordinator && period.Item3 && employee.Active==true,serverClock=MealService.Local(now).ToString("dd/MM/yyyy HH:mm:ss"),currentService=IsCoordinator?"Selección manual (sin restricción de horario)":period.Item1==null?period.Item2!:MealTypes.Name(period.Item1.Type),message=!IsCoordinator && terminal?.DiningRoomId!=AccessProfiles.OperatorRoom(User)?"El equipo no corresponde al comedor asignado a tu cuenta.":employee.Active!=true?"Empleado inactivo: consulte a RH.":!IsCoordinator && period.Item1==null?period.Item2!:"Verifica nombre y fotografía. Pulsa Enter para registrar.",requestId=Guid.NewGuid(),photoUrl=Url.Page("/Register","Photo",new {number})});
 }
 public async Task<IActionResult> OnPostQuickAsync() {
  Response.Headers.CacheControl="no-store";
  if(!ModelState.IsValid || !EmployeeNumber.IsValid(Number))return new JsonResult(new {ok=false,message=EmployeeNumber.ValidationMessage});
  var result=await meals.RegisterForUser(User,Number,meals.ResolveIp(HttpContext.Connection.RemoteIpAddress),RequestId,ConfirmedSegmentCount,ConfirmedServiceType,SelectedServiceType,CoordinatorRoomId);
  var employee=await db.Employees.SingleOrDefaultAsync(x=>x.Number==Number);
  var date=DateOnly.FromDateTime(MealService.Local(meals.Now));
  var count=employee==null?0:await db.Consumptions.CountAsync(x=>x.EmployeeId==employee.Id && x.ServiceDate==date);
  return new JsonResult(new {ok=result.Ok,message=result.Message,needsConfirmation=result.NeedsConfirmation,segmentCount=result.SegmentCount,serviceType=result.ServiceType,outsideHours=result.OutsideHours,todayCount=count});
 }
 public async Task<IActionResult> OnGetPhotoAsync(string number) {
  Response.Headers.CacheControl="no-store, private";
  Response.Headers["X-Content-Type-Options"]="nosniff";
  var path=photos.Find(number??"");
  if(path is null || !await db.Employees.AnyAsync(x=>x.Number==number)) return NotFound();
  byte[] bytes;
  try { bytes=await System.IO.File.ReadAllBytesAsync(path); }
  catch(IOException) { return NotFound(); }
  catch(UnauthorizedAccessException) {return NotFound();}
  var type=EmployeePhotos.ContentType(bytes);
  return type is null ? NotFound() : File(bytes,type);
 }
 public async Task<IActionResult> OnPostAsync() {
  if(!EmployeeNumber.IsValid(Number)) {Message=EmployeeNumber.ValidationMessage;return RedirectToPage();}
  if(!ModelState.IsValid) {Message="Solicitud inválida.";return RedirectToPage(new {Number});}
  Message=(await meals.RegisterForUser(User,Number,meals.ResolveIp(HttpContext.Connection.RemoteIpAddress),RequestId,ConfirmedSegmentCount,ConfirmedServiceType,SelectedServiceType,CoordinatorRoomId)).Message;
  return RedirectToPage(new {Number});
 }
}
