using System.Globalization;
using System.Text;
namespace Comedores.Web;
public static class ReportCsv {
 // CSV para Excel en español: UTF-8 con BOM, separador ; y campos entre comillas.
 public static byte[] Build(IEnumerable<Consumption> consumptions,bool summary) {
  var rows=consumptions.ToList();var output=new StringBuilder();
  void Line(params string[] values)=>output.Append(string.Join(';',values.Select(Cell))).Append("\r\n");
  string Number(int value)=>value.ToString(CultureInfo.InvariantCulture);
  if(summary) {
   Line("Número empleado","Nombre","Puesto","Fecha","Comedor","Desayunos","Horas desayuno","Comidas","Horas comida","Cenas","Horas cena","Adicionales","Turno especial");
   foreach(var group in rows.GroupBy(x=>new{x.EmployeeId,x.ServiceDate,x.DiningRoomId,x.SpecialShift})) {
    var first=group.First();var cells=new List<string>{first.Employee.Number,first.EmployeeName,first.Position,first.ServiceDate.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture),first.DiningRoom.Name};
    foreach(var type in new[]{"DYN","COM","CEN"}) {
     var segment=group.Where(x=>x.Type==type).OrderBy(x=>x.OccurredAt).ToList();cells.Add(Number(segment.Count));cells.Add(string.Join(", ",segment.Select(x=>MealService.Local(x.OccurredAt).ToString("HH:mm:ss",CultureInfo.InvariantCulture))));
    }
    cells.Add(Number(group.Count(x=>x.DailySequence>1)));cells.Add(first.SpecialShift?"Sí":"No");Line(cells.ToArray());
   }
  }else{
   Line("Número empleado","Nombre","Puesto","Fecha","Hora","Código servicio","Servicio","Comedor","Número del día","Clasificación","Turno especial");
   foreach(var row in rows)Line(row.Employee.Number,row.EmployeeName,row.Position,row.ServiceDate.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture),MealService.Local(row.OccurredAt).ToString("HH:mm:ss",CultureInfo.InvariantCulture),row.Type,MealTypes.Name(row.Type),row.DiningRoom.Name,Number(row.DailySequence),row.DailySequence>1?"Adicional":"Primero",row.SpecialShift?"Sí":"No");
  }
  return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(output.ToString())).ToArray();
 }
 public static string Cell(string value) {
  // Impide que texto del catálogo sea interpretado como fórmula al abrir el archivo.
  var start=value.TrimStart();
  if((start.Length>0 && "=+-@".Contains(start[0])) || value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n'))value="'"+value;
  return "\""+value.Replace("\"","\"\"")+"\"";
 }
}
