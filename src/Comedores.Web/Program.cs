using Comedores.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Comedores")
    ?? throw new InvalidOperationException("Configure ConnectionStrings__Comedores.");
builder.Services.AddDbContext<AppDb>(o=>o.UseNpgsql(connection));
builder.Services.AddIdentity<IdentityUser, IdentityRole>(o=> {
 o.Password.RequiredLength=12; o.Lockout.MaxFailedAccessAttempts=5;
}).AddEntityFrameworkStores<AppDb>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o=> { o.LoginPath="/Login"; o.Cookie.HttpOnly=true; });
builder.Services.AddRazorPages(o=> { o.Conventions.AuthorizeFolder("/"); o.Conventions.AllowAnonymousToPage("/Login"); o.Conventions.AllowAnonymousToPage("/Error"); });
builder.Services.AddScoped<MealService>();
builder.Services.AddScoped<EmployeePhotos>();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error"); app.UseHsts(); app.UseHttpsRedirection(); }
if (app.Environment.IsDevelopment()) {
 using var scope=app.Services.CreateScope();
 var db=scope.ServiceProvider.GetRequiredService<AppDb>();
 await db.Database.EnsureCreatedAsync();
 if (!await db.DiningRooms.AnyAsync()) {
  db.DiningRooms.AddRange(new DiningRoom {Id=1,Name="SUBESTACION"},new DiningRoom {Id=2,Name="PLATAFORMA"},new DiningRoom {Id=3,Name="CREI"});
  foreach(var id in new[]{1,2,3}) {
   db.MealWindows.AddRange(new MealWindow {DiningRoomId=id,Type="DYN",Start=new(8,0),End=new(11,0)},new MealWindow {DiningRoomId=id,Type="COM",Start=new(14,0),End=new(18,0)},new MealWindow {DiningRoomId=id,Type="CEN",Start=id==3?new(19,0):new(20,30),End=new(22,0)});
  }
  db.Terminals.AddRange(new Terminal{Ip="172.16.66.230",DiningRoomId=1},new Terminal{Ip="172.16.60.54",DiningRoomId=2},new Terminal{Ip="172.16.70.68",DiningRoomId=3},new Terminal{Ip="172.18.208.1",DiningRoomId=3},new Terminal{Ip="172.18.219.13",DiningRoomId=3});
  db.Employees.AddRange(new Employee{Number="40001",Name="Empleado de demostración",Area="Área de prueba",Position="Operativo",WorkSchedule="Pendiente de RH",Active=true},new Employee{Number="49001",Name="Empleado de turno especial (demo)",Area="Área de prueba",Position="Mando",WorkSchedule="Pendiente de RH",Active=true,SpecialShift=true});
  await db.SaveChangesAsync();
 }
 await LocalProfile.ImportAsync(db,app.Environment);
 var password=app.Configuration["Demo:Password"];
 if(!string.IsNullOrEmpty(password)) {
  var users=scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
  if(await users.FindByNameAsync("demo")==null) {
   var result=await users.CreateAsync(new IdentityUser {UserName="demo"},password);
   if(!result.Succeeded) throw new InvalidOperationException("La contraseña demo no cumple la política.");
  }
 }
}
app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.MapRazorPages();
app.Run();
