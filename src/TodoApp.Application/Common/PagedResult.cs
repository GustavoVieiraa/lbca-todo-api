namespace TodoApp.Application.Common;

/// <summary>
/// Resultado paginado genérico retornado pelas listagens.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Itens,
    int Pagina,
    int TamanhoPagina,
    long TotalItens)
{
    public int TotalPaginas =>
        TamanhoPagina <= 0 ? 0 : (int)Math.Ceiling((double)TotalItens / TamanhoPagina);
}
