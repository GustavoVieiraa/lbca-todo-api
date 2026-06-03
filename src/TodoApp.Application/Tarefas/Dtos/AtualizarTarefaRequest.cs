using TodoApp.Domain.Enums;

namespace TodoApp.Application.Tarefas.Dtos;

/// <summary>
/// Dados de entrada para atualizar uma tarefa (PUT). Diferente da criação,
/// permite definir o Status e NÃO exige data futura — assim é possível editar
/// ou concluir uma tarefa cujo vencimento já passou.
/// </summary>
public sealed record AtualizarTarefaRequest(
    string Titulo,
    string? Descricao,
    DateTime DataVencimento,
    StatusTarefa Status,
    Prioridade Prioridade);
