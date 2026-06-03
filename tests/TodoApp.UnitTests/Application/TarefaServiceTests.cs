using FluentAssertions;
using FluentValidation;
using NSubstitute;
using TodoApp.Application.Common;
using TodoApp.Application.Tarefas;
using TodoApp.Application.Tarefas.Dtos;
using TodoApp.Application.Tarefas.Validators;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;
using TodoApp.UnitTests.TestDoubles;

namespace TodoApp.UnitTests.Application;

public class TarefaServiceTests
{
    private static readonly DateTime Agora = new(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DataFutura = new(2026, 12, 31);
    private static readonly DateTime DataPassada = new(2020, 1, 1);

    private readonly ITarefaRepository _repositorio = Substitute.For<ITarefaRepository>();
    private readonly RelogioFake _clock = new(Agora);

    private TarefaService CriarServico() => new(
        _repositorio,
        new CriarTarefaRequestValidator(_clock),
        new AtualizarTarefaRequestValidator(),
        _clock);

    private static Tarefa TarefaExemplo() =>
        Tarefa.Criar("Exemplo", "desc", DataFutura, Prioridade.Media, Agora);

    // --------------------------------------------------------------- Criar

    [Fact]
    public async Task Criar_ComDadosValidos_PersisteERetornaComIdEStatusPendente()
    {
        _repositorio.AdicionarAsync(Arg.Any<Tarefa>(), Arg.Any<CancellationToken>()).Returns(42L);
        var servico = CriarServico();

        var resposta = await servico.CriarAsync(
            new CriarTarefaRequest("Nova", "desc", DataFutura, Prioridade.Alta));

        resposta.Id.Should().Be(42);
        resposta.Status.Should().Be(StatusTarefa.Pendente);
        resposta.CriadoEm.Should().Be(Agora);
        await _repositorio.Received(1).AdicionarAsync(Arg.Any<Tarefa>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Criar_ComDataNoPassado_LancaValidationExceptionENaoPersiste()
    {
        var servico = CriarServico();

        var act = () => servico.CriarAsync(
            new CriarTarefaRequest("Nova", null, DataPassada, Prioridade.Alta));

        await act.Should().ThrowAsync<ValidationException>();
        await _repositorio.DidNotReceive().AdicionarAsync(Arg.Any<Tarefa>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Criar_ComDataDeHoje_Permite()
    {
        // Hoje é o limite de criação (não é "passado").
        _repositorio.AdicionarAsync(Arg.Any<Tarefa>(), Arg.Any<CancellationToken>()).Returns(10L);
        var servico = CriarServico();

        var resposta = await servico.CriarAsync(
            new CriarTarefaRequest("Para hoje", null, Agora, Prioridade.Media));

        resposta.Id.Should().Be(10);
    }

    // ----------------------------------------------------------- Atualizar

    [Fact]
    public async Task Atualizar_ComTarefaExistente_AtualizaERetornaTrue()
    {
        _repositorio.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(TarefaExemplo());
        _repositorio.AtualizarAsync(Arg.Any<Tarefa>(), Arg.Any<CancellationToken>()).Returns(true);
        var servico = CriarServico();

        var ok = await servico.AtualizarAsync(1,
            new AtualizarTarefaRequest("Novo", null, DataFutura, StatusTarefa.Concluida, Prioridade.Alta));

        ok.Should().BeTrue();
        await _repositorio.Received(1).AtualizarAsync(Arg.Any<Tarefa>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Atualizar_QuandoNaoEncontrada_RetornaFalseENaoChamaUpdate()
    {
        _repositorio.ObterPorIdAsync(99, Arg.Any<CancellationToken>()).Returns((Tarefa?)null);
        var servico = CriarServico();

        var ok = await servico.AtualizarAsync(99,
            new AtualizarTarefaRequest("Novo", null, DataFutura, StatusTarefa.Pendente, Prioridade.Alta));

        ok.Should().BeFalse();
        await _repositorio.DidNotReceive().AtualizarAsync(Arg.Any<Tarefa>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Atualizar_ComTituloVazio_LancaValidationException()
    {
        var servico = CriarServico();

        var act = () => servico.AtualizarAsync(1,
            new AtualizarTarefaRequest("", null, DataFutura, StatusTarefa.Pendente, Prioridade.Alta));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Atualizar_MovendoVencimentoParaPassado_Lanca()
    {
        // Tarefa com data futura; tentar mover para o passado deve falhar.
        _repositorio.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(TarefaExemplo());
        var servico = CriarServico();

        var act = () => servico.AtualizarAsync(1,
            new AtualizarTarefaRequest("Mudar pro passado", null, DataPassada, StatusTarefa.Pendente, Prioridade.Baixa));

        await act.Should().ThrowAsync<ValidationException>();
        await _repositorio.DidNotReceive().AtualizarAsync(Arg.Any<Tarefa>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Atualizar_MantendoDataJaVencida_Permite()
    {
        // Tarefa que venceu naturalmente; concluí-la sem mudar a data deve ser permitido.
        var vencida = Tarefa.Criar("Vencida", null, DataPassada, Prioridade.Media, Agora);
        _repositorio.ObterPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(vencida);
        _repositorio.AtualizarAsync(Arg.Any<Tarefa>(), Arg.Any<CancellationToken>()).Returns(true);
        var servico = CriarServico();

        var ok = await servico.AtualizarAsync(1,
            new AtualizarTarefaRequest("Vencida", null, DataPassada, StatusTarefa.Concluida, Prioridade.Media));

        ok.Should().BeTrue();
    }

    // ------------------------------------------------------------- Obter

    [Fact]
    public async Task ObterPorId_QuandoExiste_RetornaResposta()
    {
        _repositorio.ObterPorIdAsync(5, Arg.Any<CancellationToken>()).Returns(TarefaExemplo());
        var servico = CriarServico();

        (await servico.ObterPorIdAsync(5)).Should().NotBeNull();
    }

    [Fact]
    public async Task ObterPorId_QuandoNaoExiste_RetornaNull()
    {
        _repositorio.ObterPorIdAsync(5, Arg.Any<CancellationToken>()).Returns((Tarefa?)null);
        var servico = CriarServico();

        (await servico.ObterPorIdAsync(5)).Should().BeNull();
    }

    // ------------------------------------------------------------ Listar

    [Fact]
    public async Task Listar_NormalizaPaginacaoForaDosLimites()
    {
        _repositorio.ListarAsync(Arg.Any<FiltroTarefas>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Tarefa>(new List<Tarefa>(), 1, 100, 0));
        var servico = CriarServico();

        await servico.ListarAsync(new FiltroTarefas { Pagina = 0, TamanhoPagina = 999 });

        await _repositorio.Received(1).ListarAsync(
            Arg.Is<FiltroTarefas>(f => f.Pagina == 1 && f.TamanhoPagina == 100),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Listar_MapeiaItensParaResposta()
    {
        _repositorio.ListarAsync(Arg.Any<FiltroTarefas>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Tarefa>(new[] { TarefaExemplo() }, 1, 20, 1));
        var servico = CriarServico();

        var pagina = await servico.ListarAsync(new FiltroTarefas());

        pagina.Itens.Should().ContainSingle();
        pagina.TotalItens.Should().Be(1);
    }

    // ------------------------------------------------------------ Remover

    [Fact]
    public async Task Remover_DelegaParaORepositorio()
    {
        _repositorio.RemoverAsync(7, Arg.Any<CancellationToken>()).Returns(true);
        var servico = CriarServico();

        (await servico.RemoverAsync(7)).Should().BeTrue();
    }
}
