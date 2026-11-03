using System.Text.RegularExpressions;
namespace Comedores.Web;
public sealed class EmployeePhotos(IWebHostEnvironment environment, IConfiguration? configuration=null) {
 public string DirectoryPath => string.IsNullOrWhiteSpace(configuration?["EmployeePhotos:Directory"])?Path.Combine(LocalProfile.LocalDirectory(environment),"fotos"):configuration!["EmployeePhotos:Directory"]!;
 public async Task SaveAsync(string number,byte[] jpeg) {
  if(!EmployeeNumber.IsValid(number))throw new ArgumentException("Número inválido.");
  Directory.CreateDirectory(DirectoryPath);
  var destination=Path.Combine(DirectoryPath,number+".jpg");
  var temporary=Path.Combine(DirectoryPath,"."+number+"-"+Guid.NewGuid().ToString("N")+".tmp");
  try {
   await File.WriteAllBytesAsync(temporary,jpeg);
   if(File.Exists(destination)) {
    var backup=Path.Combine(DirectoryPath,".photo-backups");Directory.CreateDirectory(backup);
    File.Copy(destination,Path.Combine(backup,number+"-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+"-"+Guid.NewGuid().ToString("N")+".jpg"));
   }
   File.Move(temporary,destination,true);
  }finally {if(File.Exists(temporary))File.Delete(temporary);}
 }
 public string? Find(string number) {
  if(!Regex.IsMatch(number,@"\A[0-9]{1,10}\z")) return null;
  var folder=DirectoryPath;
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
