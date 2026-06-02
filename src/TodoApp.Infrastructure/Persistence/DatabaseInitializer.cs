using Dapper;
using Microsoft.Data.SqlClient;
using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Persistence;

/// <summary>
/// Aplica os scripts SQL (embutidos como recurso) no startup, de forma idempotente.
/// Faz retry com backoff porque, em ambiente de container, o SQL Server pode levar
/// alguns segundos para aceitar conexões.
/// </summary>
public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private const int MaxTentativas = 15;
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(2);

    private readonly string _connectionString;

    public DatabaseInitializer(string connectionString)
        => _connectionString = connectionString;

    public async Task InicializarAsync(CancellationToken cancellationToken = default)
    {
        await ComRetryAsync(async () =>
        {
            // 1) Cria o banco conectando ao 'master'.
            var masterCs = new SqlConnectionStringBuilder(_connectionString)
            {
                InitialCatalog = "master"
            }.ConnectionString;
            await ExecutarScriptAsync(masterCs, "0000_create_database.sql", cancellationToken);

            // 2) Cria a tabela no banco alvo.
            await ExecutarScriptAsync(_connectionString, "0001_create_tarefas.sql", cancellationToken);
        }, cancellationToken);
    }

    private static async Task ExecutarScriptAsync(
        string connectionString,
        string nomeScript,
        CancellationToken ct)
    {
        var sql = LerScriptEmbutido(nomeScript);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }

    private static string LerScriptEmbutido(string nomeScript)
    {
        var assembly = typeof(DatabaseInitializer).Assembly;
        var recurso = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(nomeScript, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Script embutido não encontrado: {nomeScript}");

        using var stream = assembly.GetManifestResourceStream(recurso)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async Task ComRetryAsync(Func<Task> acao, CancellationToken ct)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                await acao();
                return;
            }
            catch (SqlException) when (tentativa < MaxTentativas)
            {
                await Task.Delay(Intervalo, ct);
            }
        }
    }
}
