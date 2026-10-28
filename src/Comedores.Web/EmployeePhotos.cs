using System.Text.RegularExpressions;
namespace Comedores.Web;
public sealed class EmployeePhotos(IWebHostEnvironment environment, IConfiguration? configuration=null) {
 public string? Find(string number) {
  if(!Regex.IsMatch(number,@"\A[0-9]{1,10}\z")) return null;
  var folder=configuration?["EmployeePhotos:Directory"];
  if(string.IsNullOrWhiteSpace(folder)) folder=Path.Combine(LocalProfile.LocalDirectory(environment),"fotos");
  foreach(var extension in new[]{".jpg",".JPG",".jpeg",".JPEG",".png",".PNG"}) {
   var file=Path.Combine(folder,number+extension);
   try {if(File.Exists(file) && new FileInfo(file).Length is >0 and <=5*1024*1024) return file;}
   catch(IOException) {return null;}
   catch(UnauthorizedAccessException) {return null;}
  }
  return null;
 }
 public static string? ContentType(byte[] bytes) {
  if(bytes.Length>=3 && bytes[0]==0xff && bytes[1]==0xd8 && bytes[2]==0xff) return "image/jpeg";
  if(bytes.Length>=8 && bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})) return "image/png";
  return null;
 }
}
