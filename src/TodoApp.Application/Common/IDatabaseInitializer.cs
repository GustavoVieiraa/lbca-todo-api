namespace TodoApp.Application.Common;

/// <summary>
/// Aplica os scripts de criação do banco no startup (idempotente),
/// de modo que o ambiente (ex.: container) suba pronto para uso.
/// </summary>
public interface IDatabaseInitializer
{
    Task InicializarAsync(CancellationToken cancellationToken = default);
}
