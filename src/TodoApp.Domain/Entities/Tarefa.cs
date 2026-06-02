using TodoApp.Domain.Enums;

namespace TodoApp.Domain.Entities;

/// <summary>
/// Tarefa do To-Do. Concentra os dados e as transições de estado válidas.
/// A entidade não valida regras de entrada (isso é responsabilidade da camada
/// Application, via FluentValidation) — assim a importação em massa consegue
/// reportar linhas inválidas sem usar exceções como fluxo de controle.
/// </summary>
public class Tarefa
{
    /// <summary>Tamanho máximo do título exigido pela especificação.</summary>
    public const int TituloTamanhoMaximo = 100;

    public long Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public DateTime DataVencimento { get; private set; }
    public StatusTarefa Status { get; private set; }
    public Prioridade Prioridade { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? AtualizadoEm { get; private set; }

    // Exigido pelo Dapper para materializar a entidade a partir do banco.
    private Tarefa() { }

    private Tarefa(
        string titulo,
        string? descricao,
        DateTime dataVencimento,
        Prioridade prioridade,
        StatusTarefa status,
        DateTime criadoEm)
    {
        Titulo = titulo;
        Descricao = descricao;
        DataVencimento = dataVencimento;
        Prioridade = prioridade;
        Status = status;
        CriadoEm = criadoEm;
    }

    /// <summary>
    /// Cria uma nova tarefa. Toda tarefa nasce como <see cref="StatusTarefa.Pendente"/>
    /// (inclusive as vindas da importação, que não traz a coluna Status).
    /// </summary>
    public static Tarefa Criar(
        string titulo,
        string? descricao,
        DateTime dataVencimento,
        Prioridade prioridade,
        DateTime criadoEm)
        => new(titulo, descricao, dataVencimento, prioridade, StatusTarefa.Pendente, criadoEm);

    /// <summary>Atualiza os dados editáveis da tarefa (usado no PUT).</summary>
    public void Atualizar(
        string titulo,
        string? descricao,
        DateTime dataVencimento,
        StatusTarefa status,
        Prioridade prioridade,
        DateTime atualizadoEm)
    {
        Titulo = titulo;
        Descricao = descricao;
        DataVencimento = dataVencimento;
        Status = status;
        Prioridade = prioridade;
        AtualizadoEm = atualizadoEm;
    }
}
