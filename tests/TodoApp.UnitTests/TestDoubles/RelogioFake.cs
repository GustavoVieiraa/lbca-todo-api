using TodoApp.Application.Common;

namespace TodoApp.UnitTests.TestDoubles;

/// <summary>Relógio com horário fixo para tornar os testes determinísticos.</summary>
public sealed class RelogioFake : IDateTimeProvider
{
    public RelogioFake(DateTime utcNow) => UtcNow = utcNow;

    public DateTime UtcNow { get; set; }
}
