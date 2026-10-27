using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using Comedores.Web;
using Microsoft.EntityFrameworkCore;
namespace Comedores.Catalogo;
public record Catalog(List<Employee> Employees,int MissingNumber,int MissingName) {
 public int Active=>Employees.Count(x=>x.Active==true);
 public int Inactive=>Employees.Count-Active;
 public static Catalog Read(string path) {
  using var parser=new TextFieldParser(path,new UTF8Encoding(false,true),true) {
   TextFieldType=FieldType.Delimited,HasFieldsEnclosedInQuotes=true,TrimWhiteSpace=true
  };
  parser.SetDelimiters(";");
  var headers=parser.ReadFields()??throw new InvalidDataException("CSV vacío.");
  string[] required=["EMP_REG","EMP_Num","EMP_Nombre","EMP_Apaterno","EMP_Amaterno","EMP_Area","EMP_Puesto","EMP_Horario_Laboral","EMP_TurnoEspecial","EMP_STATUS"];
  if(!headers.SequenceEqual(required)) throw new InvalidDataException("El CSV debe tener las diez columnas del catálogo, en su orden original y separadas por punto y coma.");
  List<Employee> rows=[];HashSet<string> numbers=[];int missing=0,unnamed=0;
  while(!parser.EndOfData) {
   var line=parser.LineNumber;
   string[] fields;
   try {fields=parser.ReadFields()!;} catch(MalformedLineException) {throw new InvalidDataException($"Formato CSV inválido en línea {line}. No se modificó la base.");}
   if(fields.Length!=10) throw new InvalidDataException($"Se esperaban diez columnas en línea {line}.");
   var number=fields[1].Trim();
   if(number.Length==0){missing++;continue;}
   if(!Regex.IsMatch(number,@"\A[0-9]{1,10}\z")||!numbers.Add(number))throw new InvalidDataException($"Número inválido o duplicado en línea {line}.");
   var status=fields[9].Trim();var special=fields[8].Trim().ToUpperInvariant();
   if(status is not ("0" or "1") || special is not ("SI" or "NO")) throw new InvalidDataException($"Estatus o turno especial inválido en línea {line}.");
   var name=string.Join(" ",fields.Skip(2).Take(3).Select(x=>x.Trim()).Where(x=>x.Length>0));
   if(name.Length==0)unnamed++;
   rows.Add(new Employee{Number=number,Name=name,Area=fields[5].Trim(),Position=fields[6].Trim(),WorkSchedule=fields[7].Trim(),SpecialShift=special=="SI",Active=status=="1"});
  }
  if(rows.Count==0)throw new InvalidDataException("El CSV no contiene empleados importables.");
  return new(rows,missing,unnamed);
 }
}
public static class CatalogReplacement {
 public static async Task ReplaceAsync(AppDb db,Catalog catalog,Func<Task> createVerifiedBackup) {
  if(catalog.Employees.Count==0)throw new InvalidDataException("No se permite reemplazar con un catálogo vacío.");
  await using var transaction=await db.Database.BeginTransactionAsync();
  // Bloquea escrituras de consumos/empleados mientras se respalda. pg_dump puede seguir leyendo.
  await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '15s'");
  await db.Database.ExecuteSqlRawAsync("LOCK TABLE \"Employees\", \"Consumptions\" IN SHARE ROW EXCLUSIVE MODE");
  await createVerifiedBackup(); // Una falla aquí impide cualquier eliminación.
  await db.Consumptions.ExecuteDeleteAsync();
  await db.Employees.ExecuteDeleteAsync();
  db.Employees.AddRange(catalog.Employees);
  await db.SaveChangesAsync();
  if(await db.Employees.CountAsync()!=catalog.Employees.Count || await db.Consumptions.AnyAsync())
   throw new InvalidOperationException("No se obtuvo el resultado esperado; se revierte la operación.");
  await transaction.CommitAsync();
 }
}
