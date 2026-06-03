using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Common;
using TodoApp.Application.Importacao;
using TodoApp.Application.Tarefas;
using TodoApp.Infrastructure.Excel;
using TodoApp.Infrastructure.Persistence;
using TodoApp.Infrastructure.Time;

namespace TodoApp.Infrastructure;

/// <summary>
/// Registro dos serviços da camada de infraestrutura (composição na API).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
        services.AddScoped<ITarefaRepository, TarefaRepository>();
        services.AddSingleton<IPlanilhaTarefaLeitor, ClosedXmlPlanilhaTarefaLeitor>();
        services.AddSingleton<IDatabaseInitializer>(_ => new DatabaseInitializer(connectionString));
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
