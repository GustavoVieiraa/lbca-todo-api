using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Health;

/// <summary>Verifica a saúde da aplicação testando a conectividade com o SQL Server.</summary>
public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqlServerHealthCheck(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));

            return HealthCheckResult.Healthy("Banco de dados acessível.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Banco de dados inacessível.", ex);
        }
    }
}
