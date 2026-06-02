using TodoApp.Domain.Enums;

namespace TodoApp.Application.Tarefas;

/// <summary>
/// Critérios de filtro e paginação da listagem de tarefas.
/// A normalização (limites de página) é feita no service antes de chegar ao repositório.
/// </summary>
public sealed record FiltroTarefas
{
    public int Pagina { get; init; } = 1;
    public int TamanhoPagina { get; init; } = 20;
    public StatusTarefa? Status { get; init; }
    public Prioridade? Prioridade { get; init; }
    public string? Busca { get; init; }

    public int Offset => (Pagina - 1) * TamanhoPagina;
}
