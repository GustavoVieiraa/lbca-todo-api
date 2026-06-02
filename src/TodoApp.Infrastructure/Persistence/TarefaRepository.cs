using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using TodoApp.Application.Common;
using TodoApp.Application.Tarefas;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Persistence;

/// <summary>
/// Persistência de <see cref="Tarefa"/> com Dapper (queries parametrizadas) e
/// <see cref="SqlBulkCopy"/> para a carga em massa.
/// </summary>
public sealed class TarefaRepository : ITarefaRepository
{
    private const string Colunas =
        "Id, Titulo, Descricao, DataVencimento, Status, Prioridade, CriadoEm, AtualizadoEm";

    private readonly IDbConnectionFactory _connectionFactory;

    public TarefaRepository(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    public async Task<long> AdicionarAsync(Tarefa tarefa, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Tarefas (Titulo, Descricao, DataVencimento, Status, Prioridade, CriadoEm)
            OUTPUT INSERTED.Id
            VALUES (@Titulo, @Descricao, @DataVencimento, @Status, @Prioridade, @CriadoEm);
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            sql,
            new
            {
                tarefa.Titulo,
                tarefa.Descricao,
                tarefa.DataVencimento,
                Status = (byte)tarefa.Status,
                Prioridade = (byte)tarefa.Prioridade,
                tarefa.CriadoEm
            },
            cancellationToken: cancellationToken));
    }

    public async Task<int> AdicionarEmLoteAsync(
        IReadOnlyCollection<Tarefa> tarefas,
        CancellationToken cancellationToken = default)
    {
        if (tarefas.Count == 0)
            return 0;

        using var table = MontarDataTable(tarefas);

        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var bulk = new SqlBulkCopy(connection)
        {
            DestinationTableName = "dbo.Tarefas",
            BatchSize = 5_000,
            BulkCopyTimeout = 120
        };

        // Mapeamento explícito coluna→coluna (Id é IDENTITY e AtualizadoEm fica nulo).
        foreach (DataColumn coluna in table.Columns)
            bulk.ColumnMappings.Add(coluna.ColumnName, coluna.ColumnName);

        await bulk.WriteToServerAsync(table, cancellationToken);
        return tarefas.Count;
    }

    public async Task<Tarefa?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {Colunas} FROM dbo.Tarefas WHERE Id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Tarefa>(new CommandDefinition(
            sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<PagedResult<Tarefa>> ListarAsync(
        FiltroTarefas filtro,
        CancellationToken cancellationToken = default)
    {
        var where = MontarWhere(filtro, out var parametros);

        var sql = $"""
            SELECT COUNT(1) FROM dbo.Tarefas {where};

            SELECT {Colunas}
            FROM dbo.Tarefas
            {where}
            ORDER BY DataVencimento, Id
            OFFSET @Offset ROWS FETCH NEXT @TamanhoPagina ROWS ONLY;
            """;

        parametros.Add("Offset", filtro.Offset);
        parametros.Add("TamanhoPagina", filtro.TamanhoPagina);

        using var connection = _connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            sql, parametros, cancellationToken: cancellationToken));

        var total = await multi.ReadSingleAsync<long>();
        var itens = (await multi.ReadAsync<Tarefa>()).ToList();

        return new PagedResult<Tarefa>(itens, filtro.Pagina, filtro.TamanhoPagina, total);
    }

    public async Task<bool> AtualizarAsync(Tarefa tarefa, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Tarefas
               SET Titulo = @Titulo,
                   Descricao = @Descricao,
                   DataVencimento = @DataVencimento,
                   Status = @Status,
                   Prioridade = @Prioridade,
                   AtualizadoEm = @AtualizadoEm
             WHERE Id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var linhas = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                tarefa.Id,
                tarefa.Titulo,
                tarefa.Descricao,
                tarefa.DataVencimento,
                Status = (byte)tarefa.Status,
                Prioridade = (byte)tarefa.Prioridade,
                tarefa.AtualizadoEm
            },
            cancellationToken: cancellationToken));

        return linhas > 0;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Tarefas WHERE Id = @Id;";

        using var connection = _connectionFactory.CreateConnection();
        var linhas = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id }, cancellationToken: cancellationToken));

        return linhas > 0;
    }

    private static string MontarWhere(FiltroTarefas filtro, out DynamicParameters parametros)
    {
        parametros = new DynamicParameters();
        var condicoes = new List<string>();

        if (filtro.Status is not null)
        {
            condicoes.Add("Status = @Status");
            parametros.Add("Status", (byte)filtro.Status.Value);
        }

        if (filtro.Prioridade is not null)
        {
            condicoes.Add("Prioridade = @Prioridade");
            parametros.Add("Prioridade", (byte)filtro.Prioridade.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            condicoes.Add("Titulo LIKE @Busca");
            parametros.Add("Busca", $"%{filtro.Busca.Trim()}%");
        }

        return condicoes.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", condicoes);
    }

    private static DataTable MontarDataTable(IReadOnlyCollection<Tarefa> tarefas)
    {
        var table = new DataTable();
        table.Columns.Add("Titulo", typeof(string));
        table.Columns.Add("Descricao", typeof(string));
        table.Columns.Add("DataVencimento", typeof(DateTime));
        table.Columns.Add("Status", typeof(byte));
        table.Columns.Add("Prioridade", typeof(byte));
        table.Columns.Add("CriadoEm", typeof(DateTime));

        foreach (var t in tarefas)
        {
            table.Rows.Add(
                t.Titulo,
                (object?)t.Descricao ?? DBNull.Value,
                t.DataVencimento,
                (byte)t.Status,
                (byte)t.Prioridade,
                t.CriadoEm);
        }

        return table;
    }
}
