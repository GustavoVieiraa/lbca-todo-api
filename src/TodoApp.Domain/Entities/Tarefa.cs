using TodoApp.Domain.Enums;

namespace TodoApp.Domain.Entities;

/// <summary>
/// Tarefa do To-Do. Concentra os dados e as transições de estado válidas.
///
/// As <b>invariantes</b> (o que precisa ser verdade para uma Tarefa existir) são
/// garantidas por guard clauses na criação/atualização — é a última linha de defesa
/// contra estados inválidos. As <b>regras de entrada relativas ao contexto</b>
/// (ex.: "a data deve ser futura", que só vale na criação) ficam na camada Application
/// via FluentValidation, que valida ANTES de chamar a entidade — assim a importação
/// em massa reporta linhas inválidas sem usar exceções como fluxo de controle.
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
    // Não passa pelos guards de propósito: dados vindos do banco são confiáveis
    // (e ainda protegidos pelas CHECK constraints da tabela).
    private Tarefa() { }

    private Tarefa(
        string titulo,
        string? descricao,
        DateTime dataVencimento,
        Prioridade prioridade,
        StatusTarefa status,
        DateTime criadoEm)
    {
        GarantirDadosValidos(titulo, dataVencimento, prioridade, status);

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
        GarantirDadosValidos(titulo, dataVencimento, prioridade, status);

        Titulo = titulo;
        Descricao = descricao;
        DataVencimento = dataVencimento;
        Status = status;
        Prioridade = prioridade;
        AtualizadoEm = atualizadoEm;
    }

    /// <summary>
    /// Invariantes mínimas e atemporais de uma Tarefa. Falha rápido (fail-fast) com
    /// exceção, pois chegar aqui com dado inválido significa que a validação da borda
    /// foi burlada — é erro de programação, não entrada de usuário.
    /// </summary>
    private static void GarantirDadosValidos(
        string titulo,
        DateTime dataVencimento,
        Prioridade prioridade,
        StatusTarefa status)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("O título é obrigatório.", nameof(titulo));

        if (titulo.Length > TituloTamanhoMaximo)
            throw new ArgumentException(
                $"O título deve ter no máximo {TituloTamanhoMaximo} caracteres.", nameof(titulo));

        if (dataVencimento == default)
            throw new ArgumentException("A data de vencimento é obrigatória.", nameof(dataVencimento));

        if (!Enum.IsDefined(prioridade))
            throw new ArgumentOutOfRangeException(
                nameof(prioridade), prioridade, "Prioridade inválida.");

        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(
                nameof(status), status, "Status inválido.");
    }
}
