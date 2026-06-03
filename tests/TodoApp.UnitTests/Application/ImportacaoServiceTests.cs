using FluentAssertions;
using NSubstitute;
using TodoApp.Application.Importacao;
using TodoApp.Application.Tarefas;
using TodoApp.Application.Tarefas.Dtos;
using TodoApp.Application.Tarefas.Validators;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;
using TodoApp.UnitTests.TestDoubles;

namespace TodoApp.UnitTests.Application;

public class ImportacaoServiceTests
{
    private static readonly DateTime Agora = new(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc);
    private const string DataFutura = "2026-12-31";
    private const string DataPassada = "2020-01-01";

    private readonly IPlanilhaTarefaLeitor _leitor = Substitute.For<IPlanilhaTarefaLeitor>();
    private readonly ITarefaRepository _repositorio = Substitute.For<ITarefaRepository>();
    private readonly RelogioFake _clock = new(Agora);

    private ImportacaoService CriarServico()
    {
        _repositorio
            .AdicionarEmLoteAsync(Arg.Any<IReadOnlyCollection<Tarefa>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<IReadOnlyCollection<Tarefa>>().Count);

        return new ImportacaoService(
            _leitor, _repositorio, new CriarTarefaRequestValidator(_clock), _clock);
    }

    private void ConfigurarLinhas(params LinhaPlanilha[] linhas)
        => _leitor.Ler(Arg.Any<Stream>()).Returns(linhas);

    [Fact]
    public async Task Importar_ComTodasLinhasValidas_ImportaTodasSemErros()
    {
        ConfigurarLinhas(
            new LinhaPlanilha(2, "Estudar", "desc", DataFutura, "Alta"),
            new LinhaPlanilha(3, "Treinar", null, DataFutura, "Baixa"));
        var servico = CriarServico();

        var resultado = await servico.ImportarAsync(Stream.Null, "tarefas.xlsx");

        resultado.TotalLinhas.Should().Be(2);
        resultado.Importadas.Should().Be(2);
        resultado.Falhas.Should().Be(0);
        resultado.Erros.Should().BeEmpty();
        resultado.NomeArquivo.Should().Be("tarefas.xlsx");
    }

    [Fact]
    public async Task Importar_ComLinhasMistas_SalvaValidasEReportaInvalidas()
    {
        ConfigurarLinhas(
            new LinhaPlanilha(2, "Válida", "ok", DataFutura, "Alta"),       // ok
            new LinhaPlanilha(3, "", null, DataFutura, "Alta"),             // título vazio
            new LinhaPlanilha(4, "Passada", null, DataPassada, "Media"),    // data no passado
            new LinhaPlanilha(5, "Prio ruim", null, DataFutura, "Urgente"), // prioridade inválida
            new LinhaPlanilha(6, "Data ruim", null, "xx/xx/xx", "Baixa"));  // data em formato inválido
        var servico = CriarServico();

        var resultado = await servico.ImportarAsync(Stream.Null, "mistas.xlsx");

        resultado.TotalLinhas.Should().Be(5);
        resultado.Importadas.Should().Be(1);
        resultado.Falhas.Should().Be(4);
        resultado.Erros.Should().HaveCount(4);
        resultado.Erros.Select(e => e.Linha).Should().BeEquivalentTo(new[] { 3, 4, 5, 6 });
    }

    [Fact]
    public async Task Importar_ReportaColunaEValorDaLinhaInvalida()
    {
        ConfigurarLinhas(new LinhaPlanilha(7, "X", null, DataFutura, "Urgente"));
        var servico = CriarServico();

        var resultado = await servico.ImportarAsync(Stream.Null, "f.xlsx");

        var erro = resultado.Erros.Should().ContainSingle().Subject;
        erro.Linha.Should().Be(7);
        erro.Coluna.Should().Be("Prioridade");
        erro.Valor.Should().Be("Urgente");
        erro.Erro.Should().Contain("Prioridade");
    }

    [Fact]
    public async Task Importar_PersisteSomenteAsTarefasValidas()
    {
        ConfigurarLinhas(
            new LinhaPlanilha(2, "Válida", null, DataFutura, "Alta"),
            new LinhaPlanilha(3, "", null, DataFutura, "Alta"));
        var servico = CriarServico();

        await servico.ImportarAsync(Stream.Null, "f.xlsx");

        await _repositorio.Received(1).AdicionarEmLoteAsync(
            Arg.Is<IReadOnlyCollection<Tarefa>>(t =>
                t.Count == 1 &&
                t.First().Titulo == "Válida" &&
                t.First().Status == StatusTarefa.Pendente &&
                t.First().CriadoEm == Agora),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Importar_ComTodasInvalidas_NaoChamaOBulk()
    {
        ConfigurarLinhas(
            new LinhaPlanilha(2, "", null, DataFutura, "Alta"),
            new LinhaPlanilha(3, "X", null, DataPassada, "Alta"));
        var servico = CriarServico();

        var resultado = await servico.ImportarAsync(Stream.Null, "f.xlsx");

        resultado.Importadas.Should().Be(0);
        await _repositorio.DidNotReceive().AdicionarEmLoteAsync(
            Arg.Any<IReadOnlyCollection<Tarefa>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Importar_PlanilhaVazia_RetornaRelatorioZerado()
    {
        ConfigurarLinhas();
        var servico = CriarServico();

        var resultado = await servico.ImportarAsync(Stream.Null, "vazia.xlsx");

        resultado.TotalLinhas.Should().Be(0);
        resultado.Importadas.Should().Be(0);
        resultado.Falhas.Should().Be(0);
        resultado.Erros.Should().BeEmpty();
    }
}
