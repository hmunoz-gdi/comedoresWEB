using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web.Pages;
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
[RequestSizeLimit(65536)]
public class FeedbackDetailModel(AppDb db,FeedbackService feedback,MealService meals):PageModel {
 [BindProperty(SupportsGet=true)]public long Id{get;set;}
 [BindProperty]public int Version{get;set;}
 [BindProperty]public string Status{get;set;}="";
 [BindProperty]public string Note{get;set;}="";
 [TempData]public string? Notice{get;set;}
 public FeedbackEntry Entry{get;set;}=null!;
 public string RoomName{get;set;}="";
 public List<FeedbackFollowUp> History{get;set;}=[];
 public async Task<IActionResult> OnGetAsync(){
  if(!FeedbackService.CanManage(User))return Forbid();Entry=(await db.FeedbackEntries.AsNoTracking().SingleOrDefaultAsync(f=>f.Id==Id))!;if(Entry==null)return NotFound();
  RoomName=await db.DiningRooms.Where(r=>r.Id==Entry.DiningRoomId).Select(r=>r.Name).SingleAsync();History=await db.FeedbackFollowUps.AsNoTracking().Where(h=>h.FeedbackEntryId==Id).OrderBy(h=>h.OccurredAt).ThenBy(h=>h.Id).ToListAsync();Version=Entry.Version;Status=Entry.Status;return Page();
 }
 public async Task<IActionResult> OnPostAsync(){
  if(!FeedbackService.CanManage(User))return Forbid();
  if(!ModelState.IsValid)return BadRequest("Datos de atención inválidos.");
  var result=await feedback.FollowUpAsync(User,meals.ResolveIp(HttpContext.Connection.RemoteIpAddress),Id,Version,Status,Note);
  if(!result.Ok){ModelState.AddModelError("",result.Message);var page=await OnGetAsync();ModelState.Remove(nameof(Version));return page;}
  Notice=result.Message;return RedirectToPage(new{Id});
 }
}
