namespace TodoApp.Domain.Enums;

/// <summary>
/// Nível de prioridade de uma tarefa.
/// Os valores numéricos são persistidos no banco (TINYINT), por isso são fixos.
/// </summary>
public enum Prioridade
{
    Baixa = 0,
    Media = 1,
    Alta = 2
}
