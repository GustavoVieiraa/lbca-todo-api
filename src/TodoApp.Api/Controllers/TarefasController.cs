using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TodoApp.Api.Configuration;
using TodoApp.Api.Contracts;
using TodoApp.Application.Common;
using TodoApp.Application.Importacao;
using TodoApp.Application.Tarefas;
using TodoApp.Application.Tarefas.Dtos;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/tarefas")]
[Authorize]
[Produces("application/json")]
public sealed class TarefasController : ControllerBase
{
    private readonly ITarefaService _tarefas;
    private readonly IImportacaoService _importacao;
    private readonly ImportacaoSettings _importSettings;

    public TarefasController(
        ITarefaService tarefas,
        IImportacaoService importacao,
        IOptions<ImportacaoSettings> importSettings)
    {
        _tarefas = tarefas;
        _importacao = importacao;
        _importSettings = importSettings.Value;
    }

    /// <summary>Lista tarefas com paginação e filtros opcionais (status, prioridade, busca por título).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TarefaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TarefaResponse>>> Listar(
        [FromQuery] ListarTarefasQuery query, CancellationToken cancellationToken)
        => Ok(await _tarefas.ListarAsync(query.ParaFiltro(), cancellationToken));

    /// <summary>Obtém uma tarefa pelo id.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(TarefaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TarefaResponse>> ObterPorId(long id, CancellationToken cancellationToken)
    {
        var tarefa = await _tarefas.ObterPorIdAsync(id, cancellationToken);
        return tarefa is null ? NotFound() : Ok(tarefa);
    }

    /// <summary>Cria uma nova tarefa.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TarefaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TarefaResponse>> Criar(
        CriarTarefaRequest request, CancellationToken cancellationToken)
    {
        var criada = await _tarefas.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = criada.Id }, criada);
    }

    /// <summary>Atualiza uma tarefa existente.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(
        long id, AtualizarTarefaRequest request, CancellationToken cancellationToken)
    {
        var atualizada = await _tarefas.AtualizarAsync(id, request, cancellationToken);
        return atualizada ? NoContent() : NotFound();
    }

    /// <summary>Remove uma tarefa.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(long id, CancellationToken cancellationToken)
    {
        var removida = await _tarefas.RemoverAsync(id, cancellationToken);
        return removida ? NoContent() : NotFound();
    }

    /// <summary>
    /// Importa tarefas em massa a partir de um arquivo Excel (.xlsx). Linhas inválidas
    /// não interrompem o processo: as válidas são salvas e o relatório detalha as falhas.
    /// </summary>
    [HttpPost("importar")]
    [ProducesResponseType(typeof(ImportacaoResultado), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportacaoResultado>> Importar(
        IFormFile arquivo, CancellationToken cancellationToken)
    {
        if (arquivo is null || arquivo.Length == 0)
            return Problem("O arquivo é obrigatório.", statusCode: StatusCodes.Status400BadRequest);

        var extensao = Path.GetExtension(arquivo.FileName);
        if (!string.Equals(extensao, _importSettings.ExtensaoPermitida, StringComparison.OrdinalIgnoreCase))
            return Problem($"Apenas arquivos {_importSettings.ExtensaoPermitida} são aceitos.",
                statusCode: StatusCodes.Status400BadRequest);

        if (arquivo.Length > _importSettings.TamanhoMaximoBytes)
            return Problem("Arquivo excede o tamanho máximo permitido.",
                statusCode: StatusCodes.Status400BadRequest);

        await using var stream = arquivo.OpenReadStream();
        var resultado = await _importacao.ImportarAsync(stream, arquivo.FileName, cancellationToken);
        return Ok(resultado);
    }
}
