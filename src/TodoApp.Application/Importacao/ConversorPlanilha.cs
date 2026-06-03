using System.Globalization;
using System.Text;
using TodoApp.Domain.Enums;

namespace TodoApp.Application.Importacao;

/// <summary>
/// Conversões tolerantes dos valores brutos da planilha para os tipos do domínio.
/// Lida com os formatos de data mais comuns e com a prioridade escrita em texto
/// (insensível a acento e a maiúsculas/minúsculas).
/// </summary>
public static class ConversorPlanilha
{
    private static readonly string[] FormatosData =
    {
        "yyyy-MM-dd HH:mm:ss", // formato normalizado emitido pelo leitor para células de data
        "yyyy-MM-dd",
        "dd/MM/yyyy",
        "dd/MM/yyyy HH:mm",
        "dd/MM/yyyy HH:mm:ss"
    };

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static bool TryParseData(string? valor, out DateTime data)
    {
        data = default;
        if (string.IsNullOrWhiteSpace(valor))
            return false;

        valor = valor.Trim();

        return DateTime.TryParseExact(valor, FormatosData, CultureInfo.InvariantCulture, DateTimeStyles.None, out data)
            || DateTime.TryParse(valor, PtBr, DateTimeStyles.None, out data);
    }

    public static bool TryParsePrioridade(string? valor, out Prioridade prioridade)
    {
        prioridade = default;
        if (string.IsNullOrWhiteSpace(valor))
            return false;

        var normalizado = RemoverAcentos(valor.Trim().ToLowerInvariant());

        switch (normalizado)
        {
            case "baixa": prioridade = Prioridade.Baixa; return true;
            case "media": prioridade = Prioridade.Media; return true;
            case "alta": prioridade = Prioridade.Alta; return true;
            default: return false;
        }
    }

    private static string RemoverAcentos(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);

        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
