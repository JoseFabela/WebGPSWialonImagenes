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
//using Xabe.FFmpeg;
using System.IO;  // Asegúrate de tener esta línea para usar System.IO.File
//using OpenCvSharp;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.IO; // Asegúrate de tener este espacio de nombres

using System;
using System;
using System.Collections.Generic;
using System.IO;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
//using Accord.Video.FFMPEG;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using FFMpegCore;
using FFMpegCore.Pipes;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Drawing;
using FFMpegCore;
using FFMpegCore.Pipes;

namespace WebGPSWialon.Controllers
{
    public class MapController : Controller
    {
        private readonly string WialonToken = "0752d7c746ac950fcee4b37b52a5088b8A9341CE09D28EBEB9054832AD5AA6F470B8CBD2"; // Reemplaza con tu token de Wialon
        private readonly string BaseUrl = "https://hst-api.wialon.us/wialon/ajax.html";
        private static readonly HttpClient client = new HttpClient();
        // Acción para la vista Añadir
        private readonly IWebHostEnvironment _env;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public MapController(IWebHostEnvironment _webHostEnvironment)
        {
            _webHostEnvironment = _webHostEnvironment;
        }

        public async Task<IActionResult> Añadir()
        {
            // Llama al método que obtiene las unidades desde la API de Wialon
            var unidades = await ObtenerUnidadesAsync();

            // Convierte las unidades a una lista de SelectListItem
            var listaUnidades = unidades.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(), // ID de la unidad
                Text = u.Name // Nombre de la unidad
            }).ToList();

            // Almacena las unidades en el ViewBag para que estén disponibles en la vista
            ViewBag.Units = listaUnidades;

            // Devuelve la vista con un modelo inicializado (si es necesario)
            return View(new ImagenesViewModel());
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
      
        public IActionResult Video(string videoPath)
        {
            ViewBag.VideoPath = videoPath;
            return View();
        }



        private async Task<List<Unidad>> ObtenerUnidadesAsync()
        {
            var sid = await GetSessionIdAsync(); // Método para obtener el SID de Wialon

            if (sid == null)
            {
                return new List<Unidad>();
            }

            // Llama a la API para obtener las unidades
            var url = $"{BaseUrl}?svc=core/search_items&params={{\"spec\":{{\"itemsType\":\"avl_unit\",\"propName\":\"sys_name\",\"propValueMask\":\"*\",\"sortType\":\"sys_name\"}},\"force\":1,\"flags\":1025,\"from\":0,\"to\":0}}&sid={sid}";
            var response = await client.GetStringAsync(url); // _client es tu HttpClient

            // Deserializa la respuesta
            var data = JsonConvert.DeserializeObject<dynamic>(response);
            var unidades = new List<Unidad>();

            foreach (var item in data.items)
            {
                unidades.Add(new Unidad
                {
                    Id = (int)item.id,
                    Name = (string)item.nm
                });
            }

            return unidades;
        }

        [HttpPost]
        public ActionResult SeleccionarUnidad(string unitId)
        {
            // Aquí puedes manejar la lógica de selección de unidad
            return RedirectToAction("MostrarMapa", new { unitId });
        }

        public ActionResult MostrarMapa(string unitId)
        {
            ViewBag.UnitId = unitId;
            return View(); // Cargar una vista que muestra el mapa
        }
        // Modelo para representar las unidades

        //public async Task<IActionResult> Mapa(int SelectedUnitId, long timeFrom, long timeTo)
        //{
        //    if (SelectedUnitId <= 0)
        //    {
        //        return RedirectToAction("Añadir"); // Redirige si no hay unidad seleccionada
        //    }

        //    // Obtén las ubicaciones de la unidad seleccionada
        //    var ubicaciones = await ObtenerUbicacionesDeUnidadAsync(SelectedUnitId, timeFrom, timeTo);

        //    // Ordena por tiempo ascendente (si no lo está ya)
        //    ubicaciones = ubicaciones.OrderBy(u => u.Tiempo).ToList();

        //    // Pasar ubicaciones a la vista
        //    ViewBag.Ubicaciones = ubicaciones;

        //    return View(); // Renderiza la vista Mapa
        //}
        public async Task<IActionResult> Mapa(int SelectedUnitId, long timeFrom, long timeTo)
        {
            if (SelectedUnitId <= 0)
            {
                return RedirectToAction("Añadir"); // Redirige si no hay unidad seleccionada
            }

            // Obtén las ubicaciones de la unidad seleccionada
            var ubicaciones = await ObtenerUbicacionesDeUnidadAsync(SelectedUnitId, timeFrom, timeTo);

            // Ordena las ubicaciones por la propiedad Hora (que ahora es un DateTime)
            ubicaciones = ubicaciones.OrderBy(u => u.Hora).ToList(); // Ordenamos por hora de forma ascendente

            // Pasar ubicaciones a la vista
            ViewBag.Ubicaciones = ubicaciones;

            return View(); // Renderiza la vista Mapa
        }


        //private async Task<List<Ubicacion>> ObtenerUbicacionesDeUnidadAsync(int unitId, long timeFrom, long timeTo)
        //{
        //    // Obtiene el SID usando tu método GetSessionIdAsync
        //    var sid = await GetSessionIdAsync();

        //    if (string.IsNullOrEmpty(sid))
        //    {
        //        return new List<Ubicacion>(); // Retorna una lista vacía si no hay SID
        //    }

        //    // URL del endpoint con los parámetros necesarios
        //    var url = $"{BaseUrl}?svc=messages/load_interval&params={{\"itemId\":{unitId},\"timeFrom\":{timeFrom},\"timeTo\":{timeTo},\"flags\":1,\"flagsMask\":65281,\"loadCount\":717}}&sid={sid}";

        //    try
        //    {
        //        // Realiza la solicitud HTTP
        //        var response = await client.GetStringAsync(url);

        //        // Deserializa la respuesta de la API
        //        var data = JsonConvert.DeserializeObject<dynamic>(response);

        //        // Lista para almacenar las ubicaciones obtenidas
        //        var ubicaciones = new List<Ubicacion>();

        //        // Itera sobre los mensajes recibidos y extrae las ubicaciones
        //        if (data?.messages != null)
        //        {
        //            foreach (var message in data.messages)
        //            {
        //                ubicaciones.Add(new Ubicacion
        //                {
        //                    Latitud = (double)message.pos.y, // Latitud
        //                    Longitud = (double)message.pos.x, // Longitud
        //                    Tiempo = (long)message.t         // Timestamp
        //                });
        //            }
        //        }

        //        return ubicaciones; // Devuelve la lista de ubicaciones
        //    }
        //    catch (Exception ex)
        //    {
        //        // Manejo de errores (opcional: registra el error)
        //        Console.WriteLine($"Error al obtener ubicaciones: {ex.Message}");
        //        return new List<Ubicacion>(); // Devuelve una lista vacía en caso de error
        //    }
        //}


        public class Ubicacion
        {
            public double Latitud { get; set; }
            public double Longitud { get; set; }
            public DateTime Hora { get; set; } // Ahora es un DateTime
        }

        private async Task<List<Ubicacion>> ObtenerUbicacionesDeUnidadAsync(int unitId, long timeFrom, long timeTo)
        {
            // Obtiene el SID usando tu método GetSessionIdAsync
            var sid = await GetSessionIdAsync();

            if (string.IsNullOrEmpty(sid))
            {
                return new List<Ubicacion>(); // Retorna una lista vacía si no hay SID
            }

            // URL del endpoint con los parámetros necesarios
            var url = $"{BaseUrl}?svc=messages/load_interval&params={{\"itemId\":{unitId},\"timeFrom\":{timeFrom},\"timeTo\":{timeTo},\"flags\":1,\"flagsMask\":65281,\"loadCount\":717}}&sid={sid}";

            try
            {
                // Realiza la solicitud HTTP
                var response = await client.GetStringAsync(url);

                // Deserializa la respuesta de la API
                var data = JsonConvert.DeserializeObject<dynamic>(response);

                // Lista para almacenar las ubicaciones obtenidas
                var ubicaciones = new List<Ubicacion>();

                // Itera sobre los mensajes recibidos y extrae las ubicaciones
                if (data?.messages != null)
                {
                    foreach (var message in data.messages)
                    {
                        // Convierte el timestamp UNIX a DateTime
                        var hora = DateTimeOffset.FromUnixTimeSeconds((long)message.t).DateTime;

                        ubicaciones.Add(new Ubicacion
                        {
                            Latitud = (double)message.pos.y, // Latitud
                            Longitud = (double)message.pos.x, // Longitud
                            Hora = hora  // Asigna la hora convertida
                        });
                    }
                }

                return ubicaciones; // Devuelve la lista de ubicaciones
            }
            catch (Exception ex)
            {
                // Manejo de errores (opcional: registra el error)
                Console.WriteLine($"Error al obtener ubicaciones: {ex.Message}");
                return new List<Ubicacion>(); // Devuelve una lista vacía en caso de error
            }
        }




        [HttpPost]
        public IActionResult CerrarSesion()
        {
            // Ruta absoluta a la carpeta "wwwroot/imagenes"
            string rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");

            try
            {
                if (Directory.Exists(rutaCarpeta))
                {
                    // Obtener todos los archivos en la carpeta
                    var archivos = Directory.GetFiles(rutaCarpeta);

                    // Eliminar cada archivo
                    foreach (var archivo in archivos)
                    {
                        System.IO.File.Delete(archivo);
                    }

                    // También puedes eliminar subdirectorios si existen
                    var subdirectorios = Directory.GetDirectories(rutaCarpeta);
                    foreach (var subdirectorio in subdirectorios)
                    {
                        Directory.Delete(subdirectorio, true);
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejar errores si algo falla
                Console.WriteLine($"Error al borrar archivos: {ex.Message}");
                // Puedes redirigir a una página de error o mostrar un mensaje al usuario
                return RedirectToAction("Error", new { mensaje = ex.Message });
            }

            // Lógica para cerrar sesión (opcional)
            HttpContext.Session.Clear(); // Si usas sesiones

            return RedirectToAction("Index", "Home"); // Redirigir a la página principal u otra
        }


        [HttpPost]
        public async Task<ActionResult> ObtenerImagenes(ImagenesViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Lógica para obtener las URLs de las imágenes con los parámetros proporcionados
                model.ImagenUrls = await GetIdImages(model.UnitId, model.TimeFrom, model.TimeTo);

                // Guardar las imágenes en el servidor
                await SaveImagesToServer(model.ImagenUrls);
                // Crear el video a partir de las imágenes
                //string videoPath = await CrearVideo();  // Método para crear el video

                // Almacenar la URL del video en TempData
                //TempData["VideoUrls"] = JsonConvert.SerializeObject(new List<string> { videoPath });


                // Almacena las URLs de las imágenes en TempData (esto se pasará a la acción Index)
                TempData["ImagenUrls"] = JsonConvert.SerializeObject(model.ImagenUrls);

                // Devuelve directamente el modelo a la vista Index
                return View("Index", model);
            }

            // Si el modelo no es válido, vuelve a mostrar el formulario
            return View("Añadir");
        }


        public async Task SaveImagesToServer(List<string> imagenUrls)
        {
         string imagesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");

            // Crear la carpeta si no existe
            if (!Directory.Exists(imagesDirectory))
            {
                Directory.CreateDirectory(imagesDirectory);
            }

            int index = 1;
            foreach (var base64Url in imagenUrls)
            {
                try
                {
                    // Verificar si la URL está en formato Base64
                    if (base64Url.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                    {
                        // Eliminar el prefijo "data:image/png;base64," o similar
                        var base64Data = base64Url.Substring(base64Url.IndexOf(',') + 1);

                        // Convertir Base64 a bytes
                        byte[] imageBytes = Convert.FromBase64String(base64Data);

                        // Guardar la imagen en el directorio correspondiente
                        string filePath = Path.Combine(imagesDirectory, $"{index}.jpeg");
                        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                        // Verificar la firma del archivo PNG
                        if (VerifyPngSignature(imageBytes))
                        {
                            Console.WriteLine($"Imagen válida guardada: {filePath}");
                        }
                        else
                        {
                            Console.WriteLine($"Imagen no válida encontrada: {filePath}");
                        }

                        index++;
                    }
                    else
                    {
                        Console.WriteLine($"La URL no está en formato Base64: {base64Url}");
                    }
                }
                catch (Exception ex)
                {
                    // Manejo de errores (si la URL no es válida o la descarga falla)
                    Console.WriteLine($"Error procesando la imagen Base64: {ex.Message}");
                }
            }
        }

        private bool VerifyPngSignature(byte[] imageBytes)
        {
            // La firma PNG debe comenzar con los bytes: 89 50 4E 47 0D 0A 1A 0A
            byte[] pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            for (int i = 0; i < pngSignature.Length; i++)
            {
                if (imageBytes[i] != pngSignature[i])
                {
                    return false;
                }
            }
            return true;
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

        private class Message
        {
            public double Lat { get; set; }
            public double Lon { get; set; }
            public long Timestamp { get; set; }
        }



        [HttpPost]
        public async Task<IActionResult> CreateVideoFromImages()
        {
            string imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");

            // Verificar si hay imágenes en la carpeta
            var imageFiles = Directory.GetFiles(imagesFolder, "*.jpeg");
            if (imageFiles.Length == 0)
            {
                return BadRequest("No se encontraron imágenes en la carpeta.");
            }

            // Verificar los archivos encontrados (esto es solo para depuración)
            foreach (var file in imageFiles)
            {
                Console.WriteLine($"Archivo encontrado: {file}");
            }

            string outputVideo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes", "output.mp4");

            // Comando para ffmpeg con analyzeduration y probesize
            string ffmpegCommand = $"-analyzeduration 2147483647 -probesize 2147483647 -y -framerate 0.5 -i \"{imagesFolder}\\%d.jpeg\" -c:v libx264 -pix_fmt yuv420p \"{outputVideo}\"";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg", // Ruta a ffmpeg.exe si no está en PATH
                Arguments = ffmpegCommand,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = new Process { StartInfo = processStartInfo };
                process.Start();

                // Leer la salida estándar en tiempo real
                process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                process.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Esperar a que el proceso termine
                await process.WaitForExitAsync();

                if (System.IO.File.Exists(outputVideo))
                {
                    byte[] videoBytes = await System.IO.File.ReadAllBytesAsync(outputVideo);
                    System.IO.File.Delete(outputVideo);

                    ViewBag.VideoBase64 = Convert.ToBase64String(videoBytes);
                    return View("Video");
                }
                else
                {
                    return BadRequest("No se pudo generar el video. Verifica los errores de FFmpeg.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al generar el video: {ex.Message}");
            }
        }





    }
}
   




    public class Unidad
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class Ubicacion
    {
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public long Tiempo { get; set; }
    }

   
