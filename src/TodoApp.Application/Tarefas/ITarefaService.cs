using TodoApp.Application.Common;
using TodoApp.Application.Tarefas.Dtos;

namespace TodoApp.Application.Tarefas;

/// <summary>Casos de uso de CRUD de tarefas.</summary>
public interface ITarefaService
{
    Task<TarefaResponse> CriarAsync(CriarTarefaRequest request, CancellationToken cancellationToken = default);

    Task<TarefaResponse?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);

    Task<PagedResult<TarefaResponse>> ListarAsync(FiltroTarefas filtro, CancellationToken cancellationToken = default);

    /// <summary>Retorna false se a tarefa não existir (a API traduz em 404).</summary>
    Task<bool> AtualizarAsync(long id, AtualizarTarefaRequest request, CancellationToken cancellationToken = default);

    /// <summary>Retorna false se a tarefa não existir (a API traduz em 404).</summary>
    Task<bool> RemoverAsync(long id, CancellationToken cancellationToken = default);
}
