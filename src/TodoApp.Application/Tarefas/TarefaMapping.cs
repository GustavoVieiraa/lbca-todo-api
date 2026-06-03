using TodoApp.Application.Tarefas.Dtos;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Tarefas;

/// <summary>
/// Mapeamento explícito entre entidade e DTO de saída. Manual de propósito:
/// é trivial, sem dependência extra (AutoMapper/Mapster) e sem "mágica" em runtime.
/// </summary>
public static class TarefaMapping
{
    public static TarefaResponse ToResponse(this Tarefa tarefa) => new(
        tarefa.Id,
        tarefa.Titulo,
        tarefa.Descricao,
        tarefa.DataVencimento,
        tarefa.Status,
        tarefa.Prioridade,
        tarefa.CriadoEm,
        tarefa.AtualizadoEm);
}
