using ClosedXML.Excel;
using FluentAssertions;
using TodoApp.Application.Importacao;
using TodoApp.Infrastructure.Excel;

namespace TodoApp.UnitTests.Infrastructure;

public class ClosedXmlPlanilhaTarefaLeitorTests
{
    private readonly ClosedXmlPlanilhaTarefaLeitor _leitor = new();

    [Fact]
    public void Ler_ArquivoNaoXlsx_LancaPlanilhaInvalida()
    {
        using var conteudo = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        var act = () => _leitor.Ler(conteudo);

        act.Should().Throw<PlanilhaInvalidaException>();
    }

    [Fact]
    public void Ler_PlanilhaValida_RetornaLinhasComNumeroReal()
    {
        using var conteudo = ConstruirPlanilha();

        var linhas = _leitor.Ler(conteudo);

        linhas.Should().HaveCount(2);
        linhas[0].Numero.Should().Be(2);
        linhas[0].Titulo.Should().Be("Tarefa A");
        linhas[1].Numero.Should().Be(3);
    }

    [Fact]
    public void Ler_IgnoraLinhasTotalmenteVazias()
    {
        using var conteudo = ConstruirPlanilhaComLinhaVazia();

        var linhas = _leitor.Ler(conteudo);

        linhas.Should().ContainSingle();
    }

    [Fact]
    public void Ler_CabecalhoForaDoModelo_LancaPlanilhaInvalida()
    {
        using var conteudo = ConstruirPlanilhaCabecalhoErrado();

        var act = () => _leitor.Ler(conteudo);

        act.Should().Throw<PlanilhaInvalidaException>();
    }

    private static MemoryStream ConstruirPlanilhaCabecalhoErrado()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Tarefas");
        // Colunas trocadas / fora do modelo esperado.
        ws.Cell(1, 1).Value = "Nome";
        ws.Cell(1, 2).Value = "Obs";
        ws.Cell(1, 3).Value = "Prazo";
        ws.Cell(1, 4).Value = "Importancia";
        ws.Cell(2, 1).Value = "Tarefa A";
        return Salvar(wb);
    }

    private static MemoryStream ConstruirPlanilha()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Tarefas");
        PreencherCabecalho(ws);
        ws.Cell(2, 1).Value = "Tarefa A";
        ws.Cell(2, 3).Value = "2030-01-01";
        ws.Cell(2, 4).Value = "Alta";
        ws.Cell(3, 1).Value = "Tarefa B";
        ws.Cell(3, 3).Value = "2030-01-02";
        ws.Cell(3, 4).Value = "Baixa";
        return Salvar(wb);
    }

    private static MemoryStream ConstruirPlanilhaComLinhaVazia()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Tarefas");
        PreencherCabecalho(ws);
        ws.Cell(2, 1).Value = "Única";
        ws.Cell(2, 3).Value = "2030-01-01";
        ws.Cell(2, 4).Value = "Media";
        // linha 3 deixada totalmente vazia de propósito
        return Salvar(wb);
    }

    private static void PreencherCabecalho(IXLWorksheet ws)
    {
        ws.Cell(1, 1).Value = "Título";
        ws.Cell(1, 2).Value = "Descrição";
        ws.Cell(1, 3).Value = "Data de Vencimento";
        ws.Cell(1, 4).Value = "Prioridade";
    }

    private static MemoryStream Salvar(XLWorkbook wb)
    {
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }
}
