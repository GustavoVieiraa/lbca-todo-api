using ClosedXML.Excel;
using TodoApp.Application.Importacao;

namespace TodoApp.Infrastructure.Excel;

/// <summary>
/// Gera a planilha de exemplo com ClosedXML, no mesmo modelo esperado pela importação
/// (cabeçalho validado). Traz linhas válidas e inválidas para demonstrar o relatório.
/// </summary>
public sealed class ClosedXmlPlanilhaExemploGenerator : IPlanilhaExemploGenerator
{
    public byte[] Gerar()
    {
        // Datas fixas no futuro distante para o exemplo continuar válido ao longo do tempo.
        var futura = new DateTime(2030, 12, 31);
        var passada = new DateTime(2020, 1, 1);

        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Tarefas");

        ws.Cell(1, 1).Value = "Título";
        ws.Cell(1, 2).Value = "Descrição";
        ws.Cell(1, 3).Value = "Data de Vencimento";
        ws.Cell(1, 4).Value = "Prioridade";

        // Válidas
        ws.Cell(2, 1).Value = "Preparar apresentação";
        ws.Cell(2, 2).Value = "Slides do fechamento do trimestre";
        ws.Cell(2, 3).Value = futura;
        ws.Cell(2, 4).Value = "Alta";

        ws.Cell(3, 1).Value = "Revisar contrato";
        ws.Cell(3, 3).Value = futura;
        ws.Cell(3, 4).Value = "Média";

        ws.Cell(4, 1).Value = "Comprar materiais";
        ws.Cell(4, 2).Value = "Itens de escritório";
        ws.Cell(4, 3).Value = futura;
        ws.Cell(4, 4).Value = "baixa"; // caixa diferente — aceita

        // Inválidas (aparecem no relatório de erros)
        ws.Cell(5, 3).Value = futura;          // título vazio
        ws.Cell(5, 4).Value = "Alta";

        ws.Cell(6, 1).Value = "Tarefa vencida";
        ws.Cell(6, 3).Value = passada;          // data no passado
        ws.Cell(6, 4).Value = "Média";

        ws.Cell(7, 1).Value = "Prioridade errada";
        ws.Cell(7, 3).Value = futura;
        ws.Cell(7, 4).Value = "Urgente";        // prioridade inválida

        ws.Cell(8, 1).Value = "Data inválida";
        ws.Cell(8, 3).Value = "data-invalida";  // formato inválido
        ws.Cell(8, 4).Value = "Baixa";

        ws.Columns().AdjustToContents();

        using var memoria = new MemoryStream();
        workbook.SaveAs(memoria);
        return memoria.ToArray();
    }
}
