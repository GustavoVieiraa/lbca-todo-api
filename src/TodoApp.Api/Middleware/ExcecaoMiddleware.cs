using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Application.Importacao;

namespace TodoApp.Api.Middleware;

/// <summary>
/// Tratamento centralizado de exceções. Converte falhas conhecidas em respostas
/// HTTP adequadas (ProblemDetails) e evita vazar detalhes internos em erros 500.
/// </summary>
public sealed class ExcecaoMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExcecaoMiddleware> _logger;

    public ExcecaoMiddleware(RequestDelegate next, ILogger<ExcecaoMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var erros = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var problema = new ValidationProblemDetails(erros)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Um ou mais erros de validação ocorreram."
            };

            await EscreverAsync(context, problema);
        }
        catch (PlanilhaInvalidaException ex)
        {
            await EscreverAsync(context, new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Planilha inválida.",
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            await EscreverAsync(context, new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Requisição inválida.",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado ao processar {Metodo} {Caminho}",
                context.Request.Method, context.Request.Path);

            await EscreverAsync(context, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno.",
                Detail = "Ocorreu um erro inesperado ao processar a requisição."
            });
        }
    }

    private static async Task EscreverAsync(HttpContext context, ProblemDetails problema)
    {
        context.Response.StatusCode = problema.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problema, problema.GetType());
    }
}
