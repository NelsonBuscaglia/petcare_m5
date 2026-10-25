using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PetCare.Features.EstadoDelDia.Data;
using PetCare.Features.Instancias.Data;
using PetCare.Features.Instancias.Domain;

namespace PetCare.Features.EstadoDelDia.Ui;

// Dentro del namespace: si no, "EstadoDelDia" se resolvería al namespace PetCare.Features.EstadoDelDia.
using PetCare.Features.EstadoDelDia.Domain;

/// <summary>
/// Pantalla principal: estado del día, registro y eliminación de cumplimientos y refresco del parcial. Los
/// handlers solo traducen errores tipados a mensaje o status (respuestas HTMX del spec: los
/// rechazos de negocio son 200 con el parcial).
/// </summary>
public class IndexModel(EstadoDelDiaService estadoDelDia, RegistrosService registros) : PageModel
{
    public const string NombreParcial = "_estado-del-dia";

    // Fijo hasta que el refresco automático sea configurable (OpcionesRefresco).
    private const int RefrescoSegundos = 10;

    public EstadoDelDiaVista Vista { get; private set; } = new(new EstadoDelDia([]), null, null, RefrescoSegundos);

    private bool EsHtmx => Request.Headers["HX-Request"] == "true";

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Vista = await ArmarVistaAsync(null, null, cancellationToken);

    /// <summary>Solo el parcial, sin layout: lo usan "Cancelar" y el refresco (FR-01).</summary>
    public async Task<IActionResult> OnGetEstadoAsync(CancellationToken cancellationToken)
    {
        Vista = await ArmarVistaAsync(null, null, cancellationToken);
        return Partial(NombreParcial, Vista);
    }

    public async Task<IActionResult> OnGetFormularioCumplimientoAsync(int? instanciaId, CancellationToken cancellationToken)
    {
        if (instanciaId is not { } id)
        {
            return BadRequest();
        }

        var hora = await registros.PrepararFormularioAsync(id, cancellationToken);
        if (!hora.EsOk && hora.Errores.Contains(CumplimientoError.InstanciaNoEncontrada))
        {
            return NotFound();
        }

        Vista = hora.EsOk
            ? await ArmarVistaAsync(
                null,
                new FormularioCumplimientoVista(id, hora.Valor.ToString("HH:mm", CultureInfo.InvariantCulture), null),
                cancellationToken)
            : await ArmarVistaAsync(CumplimientoErrores.Mensaje(hora.Errores[0]), null, cancellationToken);
        return Responder();
    }

    public async Task<IActionResult> OnPostCumplirAsync(int? instanciaId, string? horaReal, CancellationToken cancellationToken)
    {
        if (instanciaId is not { } id)
        {
            return BadRequest();
        }

        var resultado = await registros.RegistrarAsync(id, horaReal, cancellationToken);
        if (!resultado.EsOk && resultado.Errores.Contains(CumplimientoError.InstanciaNoEncontrada))
        {
            return NotFound();
        }

        if (resultado.EsOk && !EsHtmx)
        {
            return VolverAlInicio();
        }

        if (resultado.EsOk)
        {
            Vista = await ArmarVistaAsync(null, null, cancellationToken);
        }
        else
        {
            var error = resultado.Errores[0];
            var mensaje = CumplimientoErrores.Mensaje(error);
            // Un error de la hora deja el formulario abierto con lo ingresado; los demás lo cierran.
            Vista = CumplimientoErrores.EsDeHora(error)
                ? await ArmarVistaAsync(null, new FormularioCumplimientoVista(id, horaReal ?? "", mensaje), cancellationToken)
                : await ArmarVistaAsync(mensaje, null, cancellationToken);
        }

        return Responder();
    }

    public async Task<IActionResult> OnPostEliminarRegistroAsync(int? registroId, CancellationToken cancellationToken)
    {
        if (registroId is not { } id)
        {
            return BadRequest();
        }

        var resultado = await registros.EliminarAsync(id, cancellationToken);
        if (resultado.EsOk && !EsHtmx)
        {
            return VolverAlInicio();
        }

        // Los rechazos (ya eliminado, de otro día) son 200 con el mensaje: HTMX no reemplaza con 4xx.
        var mensaje = resultado.EsOk ? null : CumplimientoErrores.Mensaje(resultado.Errores[0]);
        Vista = await ArmarVistaAsync(mensaje, null, cancellationToken);
        return Responder();
    }

    /// <summary>303 (PRG): el navegador vuelve a pedir la página con GET.</summary>
    private StatusCodeResult VolverAlInicio()
    {
        Response.Headers.Location = "/";
        return StatusCode(StatusCodes.Status303SeeOther);
    }

    private IActionResult Responder() => EsHtmx ? Partial(NombreParcial, Vista) : Page();

    private async Task<EstadoDelDiaVista> ArmarVistaAsync(
        string? mensaje, FormularioCumplimientoVista? formulario, CancellationToken cancellationToken) =>
        new(await estadoDelDia.ObtenerAsync(cancellationToken), mensaje, formulario, RefrescoSegundos);
}
