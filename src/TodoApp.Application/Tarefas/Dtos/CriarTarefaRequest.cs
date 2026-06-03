using TodoApp.Domain.Enums;

namespace TodoApp.Application.Tarefas.Dtos;

/// <summary>
/// Dados de entrada para criar uma tarefa. Também é o formato para o qual cada
/// linha da planilha é convertida na importação — daí a reutilização da validação.
/// O Status não entra: toda tarefa nova nasce Pendente.
/// </summary>
public sealed record CriarTarefaRequest(
    string Titulo,
    string? Descricao,
    DateTime DataVencimento,
    Prioridade Prioridade);
