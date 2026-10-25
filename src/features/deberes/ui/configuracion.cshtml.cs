using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PetCare.Features.Deberes.Data;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Hogar.Data;
using PetCare.Features.Hogar.Domain;

namespace PetCare.Features.Deberes.Ui;

/// <summary>Modelo del parcial <c>_configuracion-deberes</c>: lista de activos y formulario de alta.</summary>
public sealed record ConfiguracionDeberesVista(
    MascotaVista Mascota,
    IReadOnlyList<DeberActivoVista> Deberes,
    IReadOnlyList<string> Errores,
    string? Tipo,
    string? Hora,
    string? Repeticion)
{
    public string Ruta => $"/mascotas/{Mascota.Id}/deberes";
}

public class ConfiguracionModel(MascotasService mascotas, DeberesService deberes) : PageModel
{
    public const string NombreParcial = "_configuracion-deberes";

    // Se bindean como texto para que valores como "medicación" o "25:00" lleguen a la validación.
    [BindProperty(Name = "tipo")]
    public string? Tipo { get; set; }

    [BindProperty(Name = "hora")]
    public string? Hora { get; set; }

    [BindProperty(Name = "repeticion")]
    public string? Repeticion { get; set; }

    public ConfiguracionDeberesVista? Vista { get; private set; }

    public async Task<IActionResult> OnGetAsync(int mascotaId, CancellationToken cancellationToken)
    {
        var mascota = await mascotas.ObtenerAsync(mascotaId, cancellationToken);
        if (mascota is null)
        {
            return NotFound();
        }

        Vista = await ArmarVistaAsync(mascota, [], null, null, null, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int mascotaId, CancellationToken cancellationToken)
    {
        var mascota = await mascotas.ObtenerAsync(mascotaId, cancellationToken);
        if (mascota is null)
        {
            return NotFound();
        }

        var resultado = await deberes.DarDeAltaAsync(mascotaId, Tipo, Hora, Repeticion, cancellationToken);
        if (!resultado.EsOk && resultado.Errores.Contains(AltaDeberError.MascotaNoEncontrada))
        {
            return NotFound();
        }

        var esHtmx = Request.Headers["HX-Request"] == "true";
        if (resultado.EsOk && !esHtmx)
        {
            // 303 (PRG): el navegador vuelve a pedir la página con GET.
            Response.Headers.Location = $"/mascotas/{mascotaId}/deberes";
            return StatusCode(StatusCodes.Status303SeeOther);
        }

        Vista = resultado.EsOk
            ? await ArmarVistaAsync(mascota, [], null, null, null, cancellationToken)
            : await ArmarVistaAsync(
                mascota,
                resultado.Errores.Select(AltaDeberErrores.Mensaje).ToList(),
                Tipo,
                Hora,
                Repeticion,
                cancellationToken);

        return esHtmx ? Partial(NombreParcial, Vista) : Page();
    }

    private async Task<ConfiguracionDeberesVista> ArmarVistaAsync(
        MascotaVista mascota,
        IReadOnlyList<string> errores,
        string? tipo,
        string? hora,
        string? repeticion,
        CancellationToken cancellationToken)
    {
        var activos = await deberes.ListarActivosAsync(mascota.Id, cancellationToken);
        return new ConfiguracionDeberesVista(mascota, activos, errores, tipo, hora, repeticion);
    }
}
