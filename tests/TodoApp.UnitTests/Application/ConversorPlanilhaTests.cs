using FluentAssertions;
using TodoApp.Application.Importacao;
using TodoApp.Domain.Enums;

namespace TodoApp.UnitTests.Application;

public class ConversorPlanilhaTests
{
    // ----------------------------------------------------------- TryParseData

    [Theory]
    [InlineData("2026-12-31", 2026, 12, 31)]
    [InlineData("2026-12-31 23:59:00", 2026, 12, 31)]
    [InlineData("31/12/2026", 2026, 12, 31)]
    [InlineData("31/12/2026 08:30", 2026, 12, 31)]
    [InlineData("  31/12/2026  ", 2026, 12, 31)]
    public void TryParseData_ComFormatosValidos_RetornaData(string valor, int ano, int mes, int dia)
    {
        var ok = ConversorPlanilha.TryParseData(valor, out var data);

        ok.Should().BeTrue();
        data.Year.Should().Be(ano);
        data.Month.Should().Be(mes);
        data.Day.Should().Be(dia);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("não é data")]
    [InlineData("32/13/2026")]
    public void TryParseData_ComValoresInvalidos_RetornaFalse(string? valor)
        => ConversorPlanilha.TryParseData(valor, out _).Should().BeFalse();

    // ------------------------------------------------------ TryParsePrioridade

    [Theory]
    [InlineData("Baixa", Prioridade.Baixa)]
    [InlineData("baixa", Prioridade.Baixa)]
    [InlineData("Média", Prioridade.Media)]
    [InlineData("media", Prioridade.Media)]
    [InlineData("MEDIA", Prioridade.Media)]
    [InlineData("Alta", Prioridade.Alta)]
    [InlineData("  alta  ", Prioridade.Alta)]
    public void TryParsePrioridade_ComTextoValido_RetornaEnum(string valor, Prioridade esperada)
    {
        var ok = ConversorPlanilha.TryParsePrioridade(valor, out var prioridade);

        ok.Should().BeTrue();
        prioridade.Should().Be(esperada);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Urgente")]
    [InlineData("123")]
    public void TryParsePrioridade_ComTextoInvalido_RetornaFalse(string? valor)
        => ConversorPlanilha.TryParsePrioridade(valor, out _).Should().BeFalse();
}
