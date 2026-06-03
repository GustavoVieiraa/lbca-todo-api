namespace TodoApp.Application.Importacao;

/// <summary>
/// Relatório retornado pela importação em massa: quantas linhas entraram, quantas
/// falharam e o detalhe de cada falha — atendendo ao requisito de resiliência
/// ("salvar as válidas e indicar claramente quais linhas falharam e por quê").
/// </summary>
public sealed record ImportacaoResultado(
    string NomeArquivo,
    int TotalLinhas,
    int Importadas,
    int Falhas,
    IReadOnlyList<ErroImportacao> Erros);

/// <summary>Detalhe de uma falha em uma linha específica da planilha.</summary>
public sealed record ErroImportacao(
    int Linha,
    string Coluna,
    string? Valor,
    string Erro);
