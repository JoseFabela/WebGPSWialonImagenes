namespace WebGPSWialon.Models
{
    public class ImagenesViewModel
    {
        public long UnitId { get; set; }
        public long TimeFrom { get; set; }
        public long TimeTo { get; set; }
        public List<string> ImagenUrls { get; set; } = new List<string>(); // Inicializa como lista vacía
        public List<ImageData> Imagenes { get; set; } = new List<ImageData>();

    }
    [Serializable]
    public class ImageData
    {
        public string ImageUrl { get; set; }
        public DateTime CapturedAt { get; set; }
    }

}
