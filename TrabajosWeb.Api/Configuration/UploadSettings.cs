namespace TrabajosWeb.Api.Configuration;

public class UploadSettings
{
    public const string SectionName = "Uploads";

    public string Carpeta { get; set; } = "uploads";
    public int TamanoMaximoMb { get; set; } = 200;
    public string[] ExtensionesImagen { get; set; } = Array.Empty<string>();
    public string[] ExtensionesVideo { get; set; } = Array.Empty<string>();

    public long TamanoMaximoBytes => TamanoMaximoMb * 1024L * 1024L;
}