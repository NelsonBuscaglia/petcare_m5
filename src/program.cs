using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PetCare.Features.Deberes.Data;
using PetCare.Features.Emails.Data;
using PetCare.Features.Emails.Domain;
using PetCare.Features.EstadoDelDia.Data;
using PetCare.Features.EstadoDelDia.Ui;
using PetCare.Features.Hogar.Data;
using PetCare.Features.Instancias.Data;
using PetCare.Shared.Data;
using PetCare.Shared.Hosting;

var builder = WebApplication.CreateBuilder(args);

var cadenaDeConexion = builder.Configuration.GetConnectionString("PetCare");
if (string.IsNullOrWhiteSpace(cadenaDeConexion))
{
    throw new InvalidOperationException("Falta ConnectionStrings:PetCare en la configuración.");
}

// Email:Smtp:* llega solo por variables de entorno (Email__Smtp__Host, …): nunca en appsettings.
var opcionesEmail = builder.Configuration.GetSection("Email").Get<OpcionesEmail>() ?? new OpcionesEmail();
var erroresEmail = ValidacionOpcionesEmail.Validar(opcionesEmail, builder.Environment.IsDevelopment());
if (erroresEmail.Count > 0)
{
    throw new InvalidOperationException($"Configuración de email inválida: {string.Join(" ", erroresEmail)}");
}

var opcionesRefresco = builder.Configuration.GetSection(OpcionesRefresco.Seccion).Get<OpcionesRefresco>() ?? new OpcionesRefresco();
var erroresRefresco = ValidacionOpcionesRefresco.Validar(opcionesRefresco);
if (erroresRefresco.Count > 0)
{
    throw new InvalidOperationException($"Configuración del estado del día inválida: {string.Join(" ", erroresRefresco)}");
}

builder.Services.AddRazorPages(o => o.RootDirectory = "/features");
// La base se crea y actualiza solo con `dotnet ef database update`: nunca al arrancar.
builder.Services.AddDbContext<PetCareDbContext>(o => o.UseSqlite(cadenaDeConexion));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<GeneracionLock>();
builder.Services.AddScoped<MascotasService>();
builder.Services.AddScoped<DeberesService>();
builder.Services.AddScoped<InstanciasService>();
builder.Services.AddScoped<RegistrosService>();
builder.Services.AddScoped<EstadoDelDiaService>();
builder.Services.AddSingleton(Options.Create(opcionesRefresco));
builder.Services.AddHostedService<GeneracionDiariaWorker>();
builder.Services.AddSingleton<IEnvioEmail>(opcionesEmail.Modo == OpcionesEmail.ModoSmtp
    ? new SmtpEnvioEmail(opcionesEmail.Smtp)
    : new ConsolaEnvioEmail());
builder.Services.AddScoped<EmailsVencimientoService>();
builder.Services.AddHostedService<VencimientosWorker>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.UseStaticFiles();
app.MapRazorPages();

app.Run();

public partial class Program;
