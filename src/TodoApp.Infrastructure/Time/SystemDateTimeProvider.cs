using TodoApp.Application.Common;

namespace TodoApp.Infrastructure.Time;

/// <summary>Relógio real do sistema (UTC). Implementação de produção do clock.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
