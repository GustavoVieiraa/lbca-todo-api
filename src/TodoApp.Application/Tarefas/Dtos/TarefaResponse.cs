using TodoApp.Domain.Enums;

namespace TodoApp.Application.Tarefas.Dtos;

/// <summary>Representação de saída de uma tarefa (contrato da API).</summary>
public sealed record TarefaResponse(
    long Id,
    string Titulo,
    string? Descricao,
    DateTime DataVencimento,
    StatusTarefa Status,
    Prioridade Prioridade,
    DateTime CriadoEm,
    DateTime? AtualizadoEm);
