namespace TodoApp.Domain.Enums;

/// <summary>
/// Situação de uma tarefa dentro do fluxo de trabalho.
/// Os valores numéricos são persistidos no banco (TINYINT), por isso são fixos.
/// </summary>
public enum StatusTarefa
{
    Pendente = 0,
    EmAndamento = 1,
    Concluida = 2
}
