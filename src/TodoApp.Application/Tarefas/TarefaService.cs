using FluentValidation;
using TodoApp.Application.Common;
using TodoApp.Application.Tarefas.Dtos;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Tarefas;

public sealed class TarefaService : ITarefaService
{
    private const int TamanhoPaginaMaximo = 100;

    private readonly ITarefaRepository _repositorio;
    private readonly IValidator<CriarTarefaRequest> _criarValidator;
    private readonly IValidator<AtualizarTarefaRequest> _atualizarValidator;
    private readonly IDateTimeProvider _clock;

    public TarefaService(
        ITarefaRepository repositorio,
        IValidator<CriarTarefaRequest> criarValidator,
        IValidator<AtualizarTarefaRequest> atualizarValidator,
        IDateTimeProvider clock)
    {
        _repositorio = repositorio;
        _criarValidator = criarValidator;
        _atualizarValidator = atualizarValidator;
        _clock = clock;
    }

    public async Task<TarefaResponse> CriarAsync(
        CriarTarefaRequest request,
        CancellationToken cancellationToken = default)
    {
        await _criarValidator.ValidateAndThrowAsync(request, cancellationToken);

        var tarefa = Tarefa.Criar(
            request.Titulo, request.Descricao, request.DataVencimento, request.Prioridade, _clock.UtcNow);

        var id = await _repositorio.AdicionarAsync(tarefa, cancellationToken);

        // O Id é gerado pelo banco; os demais valores já são conhecidos da entidade.
        return new TarefaResponse(
            id, tarefa.Titulo, tarefa.Descricao, tarefa.DataVencimento,
            tarefa.Status, tarefa.Prioridade, tarefa.CriadoEm, tarefa.AtualizadoEm);
    }

    public async Task<TarefaResponse?> ObterPorIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var tarefa = await _repositorio.ObterPorIdAsync(id, cancellationToken);
        return tarefa?.ToResponse();
    }

    public async Task<PagedResult<TarefaResponse>> ListarAsync(
        FiltroTarefas filtro,
        CancellationToken cancellationToken = default)
    {
        var normalizado = Normalizar(filtro);
        var pagina = await _repositorio.ListarAsync(normalizado, cancellationToken);

        var itens = pagina.Itens.Select(t => t.ToResponse()).ToList();
        return new PagedResult<TarefaResponse>(
            itens, pagina.Pagina, pagina.TamanhoPagina, pagina.TotalItens);
    }

    public async Task<bool> AtualizarAsync(
        long id,
        AtualizarTarefaRequest request,
        CancellationToken cancellationToken = default)
    {
        await _atualizarValidator.ValidateAndThrowAsync(request, cancellationToken);

        var tarefa = await _repositorio.ObterPorIdAsync(id, cancellationToken);
        if (tarefa is null)
            return false;

        tarefa.Atualizar(
            request.Titulo, request.Descricao, request.DataVencimento,
            request.Status, request.Prioridade, _clock.UtcNow);

        return await _repositorio.AtualizarAsync(tarefa, cancellationToken);
    }

    public Task<bool> RemoverAsync(long id, CancellationToken cancellationToken = default)
        => _repositorio.RemoverAsync(id, cancellationToken);

    private static FiltroTarefas Normalizar(FiltroTarefas filtro) => filtro with
    {
        Pagina = Math.Max(1, filtro.Pagina),
        TamanhoPagina = Math.Clamp(filtro.TamanhoPagina, 1, TamanhoPaginaMaximo)
    };
}
