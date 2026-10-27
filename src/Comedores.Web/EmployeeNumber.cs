using System.Text.RegularExpressions;
namespace Comedores.Web;
public static class EmployeeNumber {
 public const string ValidationMessage="El número de empleado debe contener solo números, de 1 a 5 dígitos.";
 // Se conserva como texto para no perder ceros iniciales.
 public static bool IsValid(string? number)=>number is not null && Regex.IsMatch(number,@"\A[0-9]{1,5}\z");
}
