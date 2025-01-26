namespace WebGPSWialon.Models
{
    public class CleanupService
    {
        private readonly string _imagesDirectory;

        public CleanupService()
        {
            _imagesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");
        }

        public void DeleteImages()
        {
            if (Directory.Exists(_imagesDirectory))
            {
                var files = Directory.GetFiles(_imagesDirectory);
                foreach (var file in files)
                {
                    try
                    {
                        System.IO.File.Delete(file); // Eliminar los archivos de la carpeta
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al eliminar la imagen: {ex.Message}");
                    }
                }
            }
        }
    }
}
