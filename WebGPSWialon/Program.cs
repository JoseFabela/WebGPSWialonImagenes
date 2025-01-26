using WebGPSWialon.Models;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor.
builder.Services.AddControllersWithViews();

// Configurar sesiones.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de expiración de la sesión.
    options.Cookie.HttpOnly = true;                // Hacer que la cookie solo sea accesible mediante HTTP.
    options.Cookie.IsEssential = true;             // Marcar la cookie como esencial para el funcionamiento de la aplicación.
});

// Registrar el servicio de limpieza para eliminar imágenes.
builder.Services.AddSingleton<CleanupService>();

var app = builder.Build();

// Obtener una instancia del servicio de limpieza
var cleanupService = app.Services.GetRequiredService<CleanupService>();

// Registrar el evento para eliminar imágenes cuando la aplicación se cierre
var applicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
applicationLifetime.ApplicationStopping.Register(() =>
{
    cleanupService.DeleteImages(); // Eliminar imágenes al detener la aplicación
});

// Configurar el middleware del pipeline de la aplicación.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Habilitar sesiones.
app.UseSession();

app.UseRouting();

app.UseAuthorization();

// Configurar las rutas.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ejecutar la aplicación.
app.Run();
