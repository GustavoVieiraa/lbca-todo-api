using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TodoApp.Application.Common;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("health")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheck;
    private readonly IHostEnvironment _environment;
    private readonly IDateTimeProvider _clock;

    public HealthController(
        HealthCheckService healthCheck,
        IHostEnvironment environment,
        IDateTimeProvider clock)
    {
        _healthCheck = healthCheck;
        _environment = environment;
        _clock = clock;
    }

    /// <summary>
    /// Verifica a saúde da API e a conectividade com o banco. Retorna 200 (Healthy)
    /// ou 503 (Unhealthy), com detalhamento por verificação.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var relatorio = await _healthCheck.CheckHealthAsync(cancellationToken);

        var resposta = new
        {
            status = relatorio.Status.ToString(),
            duracaoMs = Math.Round(relatorio.TotalDuration.TotalMilliseconds, 1),
            timestamp = _clock.UtcNow,
            ambiente = _environment.EnvironmentName,
            versao = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            checks = relatorio.Entries.Select(e => new
            {
                nome = e.Key,
                status = e.Value.Status.ToString(),
                descricao = e.Value.Description,
                duracaoMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1)
            })
        };

        return relatorio.Status == HealthStatus.Healthy
            ? Ok(resposta)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, resposta);
    }
}
