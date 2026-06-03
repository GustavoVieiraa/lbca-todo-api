using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Importacao;
using TodoApp.Application.Tarefas;
using TodoApp.Application.Tarefas.Validators;

namespace TodoApp.Application;

/// <summary>Registro dos serviços e validadores da camada de aplicação.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CriarTarefaRequestValidator>();

        services.AddScoped<ITarefaService, TarefaService>();
        services.AddScoped<IImportacaoService, ImportacaoService>();

        return services;
    }
}
