using FluentValidation;
using TodoApp.Application.Tarefas.Dtos;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Tarefas.Validators;

/// <summary>
/// Regras de entrada para atualização. Não exige data futura (permite editar/concluir
/// tarefas vencidas) mas exige que a data esteja preenchida, e valida os enums.
/// </summary>
public sealed class AtualizarTarefaRequestValidator : AbstractValidator<AtualizarTarefaRequest>
{
    public AtualizarTarefaRequestValidator()
    {
        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(Tarefa.TituloTamanhoMaximo)
                .WithMessage($"O título deve ter no máximo {Tarefa.TituloTamanhoMaximo} caracteres.");

        RuleFor(x => x.DataVencimento)
            .NotEqual(default(DateTime)).WithMessage("A data de vencimento é obrigatória.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido.");

        RuleFor(x => x.Prioridade)
            .IsInEnum().WithMessage("Prioridade inválida.");
    }
}
