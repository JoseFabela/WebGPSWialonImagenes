using Microsoft.AspNetCore.Mvc;
using System.Text;
using WebGPSWialon.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Xabe.FFmpeg;

namespace WebGPSWialon.Controllers
{
    public class MapController : Controller
    {
        private readonly string WialonToken = "0752d7c746ac950fcee4b37b52a5088b8A9341CE09D28EBEB9054832AD5AA6F470B8CBD2"; // Reemplaza con tu token de Wialon
        private readonly string BaseUrl = "https://hst-api.wialon.us/wialon/ajax.html";
        private static readonly HttpClient client = new HttpClient();
        // Acción para la vista Añadir
        public IActionResult Añadir()
        {
            return View();
        }
        // Vista inicial
        // Acción GET para mostrar las imágenes en la vista Index
        public IActionResult Index()
        {
            // Crea un modelo vacío (para cuando se accede sin datos)
            var model = new ImagenesViewModel
            {
                ImagenUrls = new List<string>()
            };

            return View(model);
        }


        // Acción POST para procesar los parámetros enviados desde el formulario
        [HttpPost]
        public async Task<ActionResult> ObtenerImagenes(ImagenesViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Lógica para obtener las URLs de las imágenes con los parámetros proporcionados
                model.ImagenUrls = await GetIdImages(model.UnitId, model.TimeFrom, model.TimeTo);

                // Almacena las URLs de las imágenes en TempData (esto se pasará a la acción Index)
                TempData["ImagenUrls"] = JsonConvert.SerializeObject(model.ImagenUrls);


                // Devuelve directamente el modelo a la vista Index
                return View("Index", model);
            }

            // Si el modelo no es válido, vuelve a mostrar el formulario
            return View("Añadir");
        }

        // Método para obtener las URLs de las imágenes
        public async Task<List<string>> GetIdImages(long unitId, long timeFrom, long timeTo)
        {
            try
            {
                // Obtener el SID
                var sid = await GetSessionIdAsync();

                if (string.IsNullOrEmpty(sid))
                {
                    return new List<string>(); // Retornar una lista vacía si no se puede obtener el SID
                }

                // Consulta inicial para obtener el count
                var initialQueryUrl = $"{BaseUrl}?svc=messages/load_interval&params={{\"itemId\":{unitId},\"timeFrom\":{timeFrom},\"timeTo\":{timeTo},\"flags\":0,\"flagsMask\":65281,\"loadCount\":0}}&sid={sid}";

                var initialResponse = await client.GetStringAsync(initialQueryUrl);
                var initialResult = JsonConvert.DeserializeObject<dynamic>(initialResponse);

                if (initialResult?.count == null || initialResult.count == 0)
                {
                    return new List<string>(); // Si no se encuentran imágenes, retornar lista vacía
                }

                var totalCount = (int)initialResult.count;
                //var totalCount = 7;

                // Consulta con loadCount igual a totalCount
                var messagesUrl = $"{BaseUrl}?svc=messages/load_interval&params={{\"itemId\":{unitId},\"timeFrom\":{timeFrom},\"timeTo\":{timeTo},\"flags\":0,\"flagsMask\":65281,\"loadCount\":{totalCount}}}&sid={sid}";
                var messagesResponse = await client.GetStringAsync(messagesUrl);
                var messagesResult = JsonConvert.DeserializeObject<dynamic>(messagesResponse);

                if (messagesResult?.messages == null)
                {
                    return new List<string>(); // Si no se encuentran mensajes, retornar lista vacía
                }

                // Filtrar mensajes con imágenes
                var images = new List<string>();
                foreach (var message in messagesResult.messages)
                {
                    // Verifica si el mensaje contiene la propiedad 'p' y si dentro de ella existe 'files' con la clave 'image'
                    if (message.p != null && message.p.files != null && message.p.files.image != null)
                    {
                        // Obtén el 'fileId' de la imagen
                        var fileId = message.p.files.image.ToString();

                        // Obtén la hora de captura de la imagen (campo 't')
                        var timeCaptured = message.t;

                        // Aquí puedes convertir el valor 't' (si es necesario) a una fecha y hora legible
                        DateTime captureTime = DateTimeOffset.FromUnixTimeSeconds((long)timeCaptured).DateTime;

                        // Puedes almacenar la URL de la imagen junto con la hora
                        var imageUrl = await ObtenerImagen(unitId, fileId, sid);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            images.Add(imageUrl); // Agregar solo URLs válidas
                        }
                    }
                }

                return images; // Retorna las URLs de las imágenes
            }
            catch (Exception ex)
            {
                // En caso de error, retorna lista vacía
                return new List<string>();
            }
        }

        public async Task<string> ObtenerImagen(long itemId, string fileId, string sid)
        {
            try
            {
                var url = $"https://hst-api.wialon.com/wialon/ajax.html?svc=messages/get_message_file&params={{\"itemId\":{itemId},\"fileId\":\"{fileId}\"}}&sid={sid}";

                // Realiza la solicitud GET para obtener la imagen
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // Lee el contenido de la respuesta como un arreglo de bytes
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    var base64Image = Convert.ToBase64String(imageBytes);

                    // Retorna la imagen en formato base64 (puedes usarla en la web directamente)
                    return $"data:image/jpeg;base64,{base64Image}";
                }
                else
                {
                    return null;  // No se pudo obtener la imagen
                }
            }
            catch (Exception ex)
            {
                return null;  // Error al obtener la imagen
            }
        }

        // Método para obtener el SID
        private async Task<string> GetSessionIdAsync()
        {
            try
            {
                var url = $"{BaseUrl}?svc=token/login&params={{\"token\":\"{WialonToken}\"}}";
                var response = await client.GetStringAsync(url);
                var result = JsonConvert.DeserializeObject<dynamic>(response);

                return result?.eid?.ToString(); // Devolver el SID
            }
            catch
            {
                return null; // Si ocurre un error al obtener el SID, retornamos null
            }
        }

        // Clase auxiliar para deserializar la respuesta de Wialon
        private class WialonResponse
        {
            [JsonProperty("messages")]
            public List<Message> Messages { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> CreateVideo([FromBody] List<string> imageUrls)
        {
            try
            {
                // Crea un directorio temporal para guardar las imágenes
                string tempDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", "images");
                Directory.CreateDirectory(tempDir);

                // Descargar todas las imágenes a un directorio temporal
                List<string> imageFiles = new List<string>();
                foreach (var imageUrl in imageUrls)
                {
                    var fileName = Path.Combine(tempDir, Path.GetFileName(imageUrl));
                    var imageBytes = await client.GetByteArrayAsync(imageUrl);
                    System.IO.File.WriteAllBytes(fileName, imageBytes);
                    imageFiles.Add(fileName);
                }

                // Crear un archivo de texto con los nombres de las imágenes (para que FFmpeg las lea en orden)
                string fileListPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", "images.txt");
                using (StreamWriter sw = new StreamWriter(fileListPath))
                {
                    foreach (var file in imageFiles)
                    {
                        sw.WriteLine($"file '{file}'");
                    }
                }

                // Definir la ruta de salida para el video
                string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", "output_video.mp4");

                // Usar FFmpeg para crear el video con las imágenes
                var ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe"; // Asegúrate de que FFmpeg esté en esta ruta o ajusta el path
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-f concat -safe 0 -i \"{fileListPath}\" -c:v libx264 -pix_fmt yuv420p -r 30 \"{outputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    process.WaitForExit(); // Esperar a que FFmpeg termine de crear el video
                }

                // El video se generó correctamente, devolver la URL del archivo generado
                return Json(new { videoUrl = "/temp/output_video.mp4" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al crear el video: {ex.Message}");
            }
        }
        private class Message
        {
            public double Lat { get; set; }
            public double Lon { get; set; }
            public long Timestamp { get; set; }
        }


       

    }
}
