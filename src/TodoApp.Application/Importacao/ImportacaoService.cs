using FluentValidation;
using TodoApp.Application.Common;
using TodoApp.Application.Tarefas;
using TodoApp.Application.Tarefas.Dtos;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Importacao;

/// <summary>
/// Importação em massa resiliente: cada linha é convertida e validada de forma
/// isolada. Linhas inválidas viram itens do relatório (nunca exceção/500); apenas
/// as válidas são persistidas em lote (bulk). Reutiliza o validador de criação
/// para manter as regras consistentes com o endpoint POST.
/// </summary>
public sealed class ImportacaoService : IImportacaoService
{
    private readonly IPlanilhaTarefaLeitor _leitor;
    private readonly ITarefaRepository _repositorio;
    private readonly IValidator<CriarTarefaRequest> _validator;
    private readonly IDateTimeProvider _clock;

    public ImportacaoService(
        IPlanilhaTarefaLeitor leitor,
        ITarefaRepository repositorio,
        IValidator<CriarTarefaRequest> validator,
        IDateTimeProvider clock)
    {
        _leitor = leitor;
        _repositorio = repositorio;
        _validator = validator;
        _clock = clock;
    }

    public async Task<ImportacaoResultado> ImportarAsync(
        Stream conteudo,
        string nomeArquivo,
        CancellationToken cancellationToken = default)
    {
        var linhas = _leitor.Ler(conteudo);
        var validas = new List<Tarefa>(linhas.Count);
        var erros = new List<ErroImportacao>();
        var agora = _clock.UtcNow;

        foreach (var linha in linhas)
        {
            var errosLinha = ConverterEValidar(linha, agora, out var tarefa);

            if (errosLinha.Count == 0 && tarefa is not null)
                validas.Add(tarefa);
            else
                erros.AddRange(errosLinha);
        }

        var importadas = validas.Count > 0
            ? await _repositorio.AdicionarEmLoteAsync(validas, cancellationToken)
            : 0;

        return new ImportacaoResultado(
            nomeArquivo,
            TotalLinhas: linhas.Count,
            Importadas: importadas,
            Falhas: linhas.Count - importadas,
            Erros: erros);
    }

    /// <summary>
    /// Converte os campos de texto e valida a linha. Retorna a lista de erros
    /// (vazia se ok) e, no caso válido, a <see cref="Tarefa"/> pronta via <paramref name="tarefa"/>.
    /// </summary>
    private List<ErroImportacao> ConverterEValidar(LinhaPlanilha linha, DateTime agora, out Tarefa? tarefa)
    {
        tarefa = null;
        var erros = new List<ErroImportacao>();

        // 1) Conversões de tipo (parse). Erros de formato são reportados aqui.
        if (!ConversorPlanilha.TryParsePrioridade(linha.Prioridade, out var prioridade))
            erros.Add(new ErroImportacao(linha.Numero, "Prioridade", linha.Prioridade,
                "Prioridade inválida (use Baixa, Média ou Alta)."));

        if (!ConversorPlanilha.TryParseData(linha.DataVencimento, out var dataVencimento))
            erros.Add(new ErroImportacao(linha.Numero, "DataVencimento", linha.DataVencimento,
                "Data de vencimento em formato inválido."));

        if (erros.Count > 0)
            return erros;

        // 2) Regras de negócio (mesmo validador do POST): título, data futura, etc.
        var request = new CriarTarefaRequest(linha.Titulo ?? string.Empty, linha.Descricao, dataVencimento, prioridade);
        var validacao = _validator.Validate(request);

        if (!validacao.IsValid)
        {
            foreach (var falha in validacao.Errors)
                erros.Add(new ErroImportacao(
                    linha.Numero, falha.PropertyName, ValorBruto(linha, falha.PropertyName), falha.ErrorMessage));

            return erros;
        }

        tarefa = Tarefa.Criar(request.Titulo, request.Descricao, request.DataVencimento, request.Prioridade, agora);
        return erros;
    }

    private static string? ValorBruto(LinhaPlanilha linha, string propriedade) => propriedade switch
    {
        nameof(CriarTarefaRequest.Titulo) => linha.Titulo,
        nameof(CriarTarefaRequest.DataVencimento) => linha.DataVencimento,
        nameof(CriarTarefaRequest.Prioridade) => linha.Prioridade,
        _ => null
    };
}
