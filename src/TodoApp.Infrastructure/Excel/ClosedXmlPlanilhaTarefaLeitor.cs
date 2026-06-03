using System.Globalization;
using System.Text;
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

    // Modelo esperado do arquivo (ordem das colunas).
    private static readonly string[] ColunasEsperadas =
        { "Título", "Descrição", "Data de Vencimento", "Prioridade" };

    public IReadOnlyList<LinhaPlanilha> Ler(Stream conteudo)
    {
        try
        {
            return LerInterno(conteudo);
        }
        catch (Exception ex) when (ex is not PlanilhaInvalidaException)
        {
            // Arquivo corrompido / não é um .xlsx válido: vira 400, não 500.
            throw new PlanilhaInvalidaException(
                "Não foi possível ler a planilha. Verifique se o arquivo é um .xlsx válido.", ex);
        }
    }

    private static IReadOnlyList<LinhaPlanilha> LerInterno(Stream conteudo)
    {
        using var workbook = new XLWorkbook(conteudo);
        var planilha = workbook.Worksheets.First();

        var linhasUsadas = planilha.RowsUsed().ToList();
        if (linhasUsadas.Count == 0)
            throw new PlanilhaInvalidaException("A planilha está vazia ou não possui cabeçalho.");

        ValidarCabecalho(linhasUsadas[0]);

        var linhas = new List<LinhaPlanilha>();

        // A 1ª linha usada é o cabeçalho (já validado); as demais são dados.
        foreach (var linha in linhasUsadas.Skip(1))
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

    /// <summary>
    /// Garante que o cabeçalho corresponde ao modelo esperado (na ordem). Comparação
    /// tolerante a acento, caixa e espaços nas pontas. Caso contrário, 400 (modelo inválido).
    /// </summary>
    private static void ValidarCabecalho(IXLRow cabecalho)
    {
        for (var i = 0; i < ColunasEsperadas.Length; i++)
        {
            var atual = Normalizar(cabecalho.Cell(i + 1).GetString());
            var esperado = Normalizar(ColunasEsperadas[i]);

            if (atual != esperado)
            {
                throw new PlanilhaInvalidaException(
                    $"Cabeçalho inválido na coluna {i + 1} (esperado: '{ColunasEsperadas[i]}'). " +
                    $"Use o modelo com as colunas: {string.Join(", ", ColunasEsperadas)}.");
            }
        }
    }

    private static string Normalizar(string texto)
    {
        var decomposto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);

        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
