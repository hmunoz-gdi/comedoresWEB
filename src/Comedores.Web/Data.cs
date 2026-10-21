using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Web;
public class Employee {
 public int Id { get; set; }
 public string Number { get; set; } = "";
 public string Name { get; set; } = "";
 public string Area { get; set; } = "";
 public string Position { get; set; } = "";
 public string WorkSchedule { get; set; } = "";
 public bool SpecialShift { get; set; }
 public bool? Active { get; set; }
 public string Category => int.TryParse(Number, out var n) ? n < 40000 ? "Corporativo" : n < 49000 ? "Operativo" : "Mando" : "Sin clasificar";
}
public class DiningRoom { public int Id { get; set; } public string Name { get; set; } = ""; }
public class Terminal { public int Id { get; set; } public string Ip { get; set; } = ""; public int DiningRoomId { get; set; } public DiningRoom DiningRoom { get; set; } = null!; }
public class MealWindow { public int Id { get; set; } public int DiningRoomId { get; set; } public string Type { get; set; } = ""; public TimeOnly Start { get; set; } public TimeOnly End { get; set; } }
public class Consumption {
 public long Id { get; set; }
 public Guid RequestId { get; set; }
 public int EmployeeId { get; set; }
 public Employee Employee { get; set; } = null!;
 public int DiningRoomId { get; set; }
 public DiningRoom DiningRoom { get; set; } = null!;
 public DateTime OccurredAt { get; set; }
 public DateOnly ServiceDate { get; set; }
 public string Type { get; set; } = "";
 public bool SpecialShift { get; set; }
 public string EmployeeName { get; set; } = "";
 public string Position { get; set; } = "";
 public string Ip { get; set; } = "";
 public string Operator { get; set; } = "";
 public int DailySequence { get; set; }
}
public class AppDb(DbContextOptions<AppDb> options) : IdentityDbContext<IdentityUser>(options) {
 public DbSet<Employee> Employees => Set<Employee>();
 public DbSet<DiningRoom> DiningRooms => Set<DiningRoom>();
 public DbSet<Terminal> Terminals => Set<Terminal>();
 public DbSet<MealWindow> MealWindows => Set<MealWindow>();
 public DbSet<Consumption> Consumptions => Set<Consumption>();
 protected override void OnModelCreating(ModelBuilder b) {
  base.OnModelCreating(b);
  b.Entity<Employee>().HasIndex(x=>x.Number).IsUnique();
  b.Entity<Employee>().Ignore(x=>x.Category);
  b.Entity<Terminal>().HasIndex(x=>x.Ip).IsUnique();
  b.Entity<Consumption>().HasIndex(x=>x.RequestId).IsUnique();
  b.Entity<Consumption>().HasIndex(x=>new {x.EmployeeId,x.ServiceDate,x.DailySequence}).IsUnique();
 }
}
