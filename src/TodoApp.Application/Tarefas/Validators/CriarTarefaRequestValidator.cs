using FluentValidation;
using TodoApp.Application.Common;
using TodoApp.Application.Tarefas.Dtos;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Tarefas.Validators;

/// <summary>
/// Regras de entrada para criação de tarefa. Reaproveitado na importação em massa,
/// garantindo consistência entre o endpoint de criação e o de upload.
/// </summary>
public sealed class CriarTarefaRequestValidator : AbstractValidator<CriarTarefaRequest>
{
    public CriarTarefaRequestValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(Tarefa.TituloTamanhoMaximo)
                .WithMessage($"O título deve ter no máximo {Tarefa.TituloTamanhoMaximo} caracteres.");

        // Must avalia o "agora" no momento da validação (não na construção do validator).
        RuleFor(x => x.DataVencimento)
            .Must(data => data > clock.UtcNow).WithMessage("A data de vencimento deve ser futura.");

        RuleFor(x => x.Prioridade)
            .IsInEnum().WithMessage("Prioridade inválida.");
    }
}
