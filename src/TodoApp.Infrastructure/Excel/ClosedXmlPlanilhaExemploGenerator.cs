using ClosedXML.Excel;
using TodoApp.Application.Importacao;

namespace TodoApp.Infrastructure.Excel;

/// <summary>
/// Gera planilhas de exemplo com ClosedXML, no modelo esperado pela importação
/// (cabeçalho validado). Dois tipos: "completo" (10 tarefas válidas) e "com erros"
/// (linhas válidas e inválidas para demonstrar o relatório de resiliência).
/// </summary>
public sealed class ClosedXmlPlanilhaExemploGenerator : IPlanilhaExemploGenerator
{
    // Datas fixas no futuro distante para os exemplos continuarem válidos com o tempo.
    private static readonly DateTime Futura = new(2030, 12, 31);
    private static readonly DateTime Passada = new(2020, 1, 1);

    public byte[] Gerar(TipoPlanilhaExemplo tipo)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Tarefas");

        ws.Cell(1, 1).Value = "Título";
        ws.Cell(1, 2).Value = "Descrição";
        ws.Cell(1, 3).Value = "Data de Vencimento";
        ws.Cell(1, 4).Value = "Prioridade";

        if (tipo == TipoPlanilhaExemplo.Completo)
            PreencherCompleto(ws);
        else
            PreencherComErros(ws);

        ws.Columns().AdjustToContents();

        using var memoria = new MemoryStream();
        workbook.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void PreencherCompleto(IXLWorksheet ws)
    {
        (string Titulo, string Descricao, DateTime Data, string Prioridade)[] tarefas =
        {
            ("Preparar apresentação trimestral", "Slides e relatório de resultados", new DateTime(2030, 1, 15), "Alta"),
            ("Revisar contrato de prestação", "Cláusulas 3 e 7", new DateTime(2030, 1, 28), "Média"),
            ("Comprar materiais de escritório", "", new DateTime(2030, 2, 10), "Baixa"),
            ("Reunião de alinhamento com cliente", "Definição de escopo", new DateTime(2030, 2, 22), "Alta"),
            ("Atualizar política de privacidade", "Adequação à LGPD", new DateTime(2030, 3, 5), "Média"),
            ("Treinamento da equipe", "Onboarding dos novos membros", new DateTime(2030, 3, 19), "Baixa"),
            ("Fechamento contábil mensal", "", new DateTime(2030, 4, 2), "Alta"),
            ("Backup dos sistemas", "Rotina trimestral", new DateTime(2030, 4, 16), "Média"),
            ("Planejamento de marketing", "Campanha do segundo semestre", new DateTime(2030, 5, 8), "Baixa"),
            ("Auditoria interna", "Processos financeiros", new DateTime(2030, 6, 1), "Alta")
        };

        for (var i = 0; i < tarefas.Length; i++)
        {
            var linha = i + 2;
            ws.Cell(linha, 1).Value = tarefas[i].Titulo;
            ws.Cell(linha, 2).Value = tarefas[i].Descricao;
            ws.Cell(linha, 3).Value = tarefas[i].Data;
            ws.Cell(linha, 4).Value = tarefas[i].Prioridade;
        }
    }

    private static void PreencherComErros(IXLWorksheet ws)
    {
        // Válidas
        ws.Cell(2, 1).Value = "Preparar apresentação";
        ws.Cell(2, 2).Value = "Slides do fechamento do trimestre";
        ws.Cell(2, 3).Value = Futura;
        ws.Cell(2, 4).Value = "Alta";

        ws.Cell(3, 1).Value = "Revisar contrato";
        ws.Cell(3, 3).Value = Futura;
        ws.Cell(3, 4).Value = "Média";

        ws.Cell(4, 1).Value = "Comprar materiais";
        ws.Cell(4, 2).Value = "Itens de escritório";
        ws.Cell(4, 3).Value = Futura;
        ws.Cell(4, 4).Value = "baixa"; // caixa diferente — aceita

        // Inválidas (aparecem no relatório de erros)
        ws.Cell(5, 3).Value = Futura;          // título vazio
        ws.Cell(5, 4).Value = "Alta";

        ws.Cell(6, 1).Value = "Tarefa vencida";
        ws.Cell(6, 3).Value = Passada;          // data no passado
        ws.Cell(6, 4).Value = "Média";

        ws.Cell(7, 1).Value = "Prioridade errada";
        ws.Cell(7, 3).Value = Futura;
        ws.Cell(7, 4).Value = "Urgente";        // prioridade inválida

        ws.Cell(8, 1).Value = "Data inválida";
        ws.Cell(8, 3).Value = "data-invalida";  // formato inválido
        ws.Cell(8, 4).Value = "Baixa";
    }
}
