namespace PetCare.Shared.Resultados;

/// <summary>
/// Resultado de una operación que puede fallar por reglas de negocio: un valor, o la lista de
/// errores tipados (nunca vacía) que impidieron obtenerlo. Es una clase con constructor privado
/// para que no exista un valor por defecto inválido.
/// </summary>
public sealed record Resultado<T, TError>
{
    private readonly T? valor;

    private Resultado(bool esOk, T? valor, IReadOnlyList<TError> errores)
    {
        EsOk = esOk;
        this.valor = valor;
        Errores = errores;
    }

    public bool EsOk { get; }

    public T Valor => EsOk
        ? valor!
        : throw new InvalidOperationException("El resultado es una falla: no tiene valor.");

    public IReadOnlyList<TError> Errores { get; }

    public static Resultado<T, TError> Ok(T valor) => new(true, valor, []);

    public static Resultado<T, TError> Falla(IReadOnlyList<TError> errores)
    {
        ArgumentNullException.ThrowIfNull(errores);
        if (errores.Count == 0)
        {
            throw new ArgumentException("Una falla necesita al menos un error.", nameof(errores));
        }

        return new(false, default, errores);
    }
}
