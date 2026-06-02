namespace TodoApp.Application.Importacao;

/// <summary>
/// Lê uma planilha (.xlsx) e devolve as linhas em formato bruto (strings),
/// SEM interpretar ou validar — toda a interpretação (parse de data, mapeamento
/// de prioridade, regras de negócio) acontece na camada Application. Isso mantém
/// o leitor "burro" e testável, e centraliza a resiliência da importação.
/// </summary>
public interface IPlanilhaTarefaLeitor
{
    IReadOnlyList<LinhaPlanilha> Ler(Stream conteudo);
}

/// <summary>
/// Uma linha bruta da planilha. <see cref="Numero"/> é o número real da linha no
/// Excel (1-based), usado no relatório de erros para o usuário localizar o problema.
/// </summary>
public sealed record LinhaPlanilha(
    int Numero,
    string? Titulo,
    string? Descricao,
    string? DataVencimento,
    string? Prioridade);
