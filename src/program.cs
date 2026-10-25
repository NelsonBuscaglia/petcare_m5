using Microsoft.EntityFrameworkCore;
using PetCare.Shared.Data;
using PetCare.Shared.Hosting;

var builder = WebApplication.CreateBuilder(args);

var cadenaDeConexion = builder.Configuration.GetConnectionString("PetCare");
if (string.IsNullOrWhiteSpace(cadenaDeConexion))
{
    throw new InvalidOperationException("Falta ConnectionStrings:PetCare en la configuración.");
}

builder.Services.AddRazorPages(o => o.RootDirectory = "/features");
// La base se crea y actualiza solo con `dotnet ef database update`: nunca al arrancar.
builder.Services.AddDbContext<PetCareDbContext>(o => o.UseSqlite(cadenaDeConexion));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<GeneracionLock>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.UseStaticFiles();
app.MapRazorPages();

app.Run();

public partial class Program;
