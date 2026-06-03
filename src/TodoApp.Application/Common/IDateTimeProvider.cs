namespace TodoApp.Application.Common;

/// <summary>
/// Abstração do relógio do sistema. Permite que regras dependentes de tempo
/// (ex.: "a data deve ser futura") e os carimbos CriadoEm/AtualizadoEm sejam
/// determinísticos e testáveis (basta injetar um relógio fixo nos testes).
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
