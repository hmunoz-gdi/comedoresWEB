namespace Comedores.Web;
public static class MealTypes {
 public static string Name(string type)=>type switch {"DYN"=>"Desayuno","COM"=>"Comida","CEN"=>"Cena",_=>"Servicio desconocido"};
 public static bool IsKnown(string type)=>type is "DYN" or "COM" or "CEN";
}
