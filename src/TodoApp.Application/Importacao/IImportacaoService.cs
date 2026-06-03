namespace TodoApp.Application.Importacao;

/// <summary>Caso de uso de importação em massa de tarefas a partir de uma planilha.</summary>
public interface IImportacaoService
{
    Task<ImportacaoResultado> ImportarAsync(
        Stream conteudo,
        string nomeArquivo,
        CancellationToken cancellationToken = default);
}
