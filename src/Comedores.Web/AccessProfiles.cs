using System.Security.Claims;
namespace Comedores.Web;
public static class AccessProfiles {
 public const string Operator="Operador",Catalog="Catalogo",Coordinator="Coordinador",Admin="Administrador";
 public const string RoomClaim="ComedorId";
 public static int? OperatorRoom(ClaimsPrincipal user)=>int.TryParse(user.FindFirstValue(RoomClaim),out var id)?id:null;
 public static readonly string[] All=[Operator,Catalog,Coordinator,Admin];
 public static bool Services(ClaimsPrincipal user)=>user.IsInRole(Operator)||user.IsInRole(Coordinator);
 public static bool Employees(ClaimsPrincipal user)=>user.IsInRole(Catalog);
 public static bool Reports(ClaimsPrincipal user)=>Services(user);
}
