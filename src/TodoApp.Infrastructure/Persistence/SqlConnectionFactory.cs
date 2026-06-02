using System.Data;
using Microsoft.Data.SqlClient;

namespace TodoApp.Infrastructure.Persistence;

/// <summary>
/// Implementação para SQL Server. Cada chamada devolve uma conexão nova;
/// o pool do ADO.NET cuida do reaproveitamento físico.
/// </summary>
public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
        => _connectionString = connectionString
            ?? throw new ArgumentNullException(nameof(connectionString));

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
