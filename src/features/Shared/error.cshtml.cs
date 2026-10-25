using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PetCare.Features.Shared;

/// <summary>
/// Página genérica que re-ejecuta <c>UseExceptionHandler</c>: sin detalles de la excepción.
/// Ignora antiforgery porque también se re-ejecuta para POST que fallaron.
/// </summary>
[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : PageModel
{
}
