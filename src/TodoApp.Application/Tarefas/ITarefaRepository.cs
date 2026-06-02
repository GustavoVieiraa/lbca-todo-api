using TodoApp.Application.Common;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Tarefas;

/// <summary>
/// Porta de persistência de tarefas. A implementação (Dapper/SQL Server) vive na Infrastructure.
/// </summary>
public interface ITarefaRepository
{
    /// <summary>Insere uma tarefa e retorna o Id gerado.</summary>
    Task<long> AdicionarAsync(Tarefa tarefa, CancellationToken cancellationToken = default);

    /// <summary>Insere várias tarefas de uma vez (bulk) e retorna a quantidade inserida.</summary>
    Task<int> AdicionarEmLoteAsync(IReadOnlyCollection<Tarefa> tarefas, CancellationToken cancellationToken = default);

    Task<Tarefa?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);

    Task<PagedResult<Tarefa>> ListarAsync(FiltroTarefas filtro, CancellationToken cancellationToken = default);

    /// <summary>Atualiza a tarefa; retorna false se o Id não existir.</summary>
    Task<bool> AtualizarAsync(Tarefa tarefa, CancellationToken cancellationToken = default);

    /// <summary>Remove a tarefa; retorna false se o Id não existir.</summary>
    Task<bool> RemoverAsync(long id, CancellationToken cancellationToken = default);
}
