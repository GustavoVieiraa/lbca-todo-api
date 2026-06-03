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

        // Não pode ser anterior a hoje (hoje é permitido — é o limite de criação).
        // Comparação por data (ignora a hora); avaliada no momento da validação.
        RuleFor(x => x.DataVencimento)
            .Must(data => data.Date >= clock.UtcNow.Date)
            .WithMessage("A data de vencimento não pode ser anterior a hoje.");

        RuleFor(x => x.Prioridade)
            .IsInEnum().WithMessage("Prioridade inválida.");
    }
}
