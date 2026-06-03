using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace TodoApp.IntegrationTests;

/// <summary>
/// Sobe um SQL Server real em container (Testcontainers) e aponta a API para ele.
/// O schema é criado pelo próprio DatabaseInitializer no startup da aplicação.
///
/// A connection string é injetada via variável de ambiente (ConnectionStrings__TodoDb):
/// como o AddEnvironmentVariables tem precedência sobre o appsettings.json, isso garante
/// que a aplicação use o container de teste, e não a connection string padrão.
/// Encrypt=false evita o handshake TLS contra o certificado autoassinado do container.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();

        var connectionString = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "TodoDb",
            TrustServerCertificate = true,
            Encrypt = false
        }.ConnectionString;

        Environment.SetEnvironmentVariable("ConnectionStrings__TodoDb", connectionString);
    }

    Task IAsyncLifetime.DisposeAsync() => _sqlServer.DisposeAsync().AsTask();
}
