using ClosedXML.Excel;
using TodoApp.Application.Importacao;

namespace TodoApp.Infrastructure.Excel;

/// <summary>
/// Leitor de planilhas .xlsx baseado em ClosedXML.
/// Lê a primeira aba assumindo a 1ª linha como cabeçalho e as colunas na ordem
/// da especificação: Título | Descrição | Data de Vencimento | Prioridade.
/// Não interpreta nem valida — devolve as células como texto.
/// </summary>
public sealed class ClosedXmlPlanilhaTarefaLeitor : IPlanilhaTarefaLeitor
{
    private const int ColTitulo = 1;
    private const int ColDescricao = 2;
    private const int ColDataVencimento = 3;
    private const int ColPrioridade = 4;

    public IReadOnlyList<LinhaPlanilha> Ler(Stream conteudo)
    {
        using var workbook = new XLWorkbook(conteudo);
        var planilha = workbook.Worksheets.First();

        var linhas = new List<LinhaPlanilha>();

        // RowsUsed() ignora linhas totalmente vazias; a 1ª usada é o cabeçalho.
        foreach (var linha in planilha.RowsUsed().Skip(1))
        {
            var titulo = LerTexto(linha.Cell(ColTitulo));
            var descricao = LerTexto(linha.Cell(ColDescricao));
            var dataVencimento = LerData(linha.Cell(ColDataVencimento));
            var prioridade = LerTexto(linha.Cell(ColPrioridade));

            // Pula linhas em que todas as colunas relevantes estão vazias.
            if (titulo is null && descricao is null && dataVencimento is null && prioridade is null)
                continue;

            linhas.Add(new LinhaPlanilha(
                linha.RowNumber(),
                titulo,
                descricao,
                dataVencimento,
                prioridade));
        }

        return linhas;
    }

    private static string? LerTexto(IXLCell cell)
    {
        if (cell.IsEmpty())
            return null;

        var texto = cell.GetString().Trim();
        return texto.Length == 0 ? null : texto;
    }

    private static string? LerData(IXLCell cell)
    {
        if (cell.IsEmpty())
            return null;

        // Célula formatada como data: normaliza para ISO, evitando ambiguidade de locale.
        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss");

        return LerTexto(cell);
    }
}
