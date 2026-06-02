using System.Data;

namespace TodoApp.Infrastructure.Persistence;

/// <summary>
/// Fábrica de conexões com o banco. Abstrai a criação para facilitar testes
/// e manter os repositórios desacoplados da connection string.
/// </summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
