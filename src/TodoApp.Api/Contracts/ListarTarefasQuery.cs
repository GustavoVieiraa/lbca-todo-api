using TodoApp.Application.Tarefas;
using TodoApp.Domain.Enums;

namespace TodoApp.Api.Contracts;

/// <summary>Parâmetros de query da listagem paginada (bind a partir da query string).</summary>
public sealed class ListarTarefasQuery
{
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
    public StatusTarefa? Status { get; set; }
    public Prioridade? Prioridade { get; set; }
    public string? Busca { get; set; }

    public FiltroTarefas ParaFiltro() => new()
    {
        Pagina = Pagina,
        TamanhoPagina = TamanhoPagina,
        Status = Status,
        Prioridade = Prioridade,
        Busca = Busca
    };
}
