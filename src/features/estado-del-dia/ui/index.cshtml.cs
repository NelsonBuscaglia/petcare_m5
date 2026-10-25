using Microsoft.AspNetCore.Mvc.RazorPages;
using PetCare.Features.EstadoDelDia.Data;

namespace PetCare.Features.EstadoDelDia.Ui;

// Dentro del namespace: si no, "EstadoDelDia" se resolvería al namespace PetCare.Features.EstadoDelDia.
using PetCare.Features.EstadoDelDia.Domain;

/// <summary>Pantalla principal: estado del día. Se actualiza al abrirla o recargarla.</summary>
public class IndexModel(EstadoDelDiaService estadoDelDia) : PageModel
{
    public EstadoDelDia Estado { get; private set; } = new([]);

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Estado = await estadoDelDia.ObtenerAsync(cancellationToken);
}
