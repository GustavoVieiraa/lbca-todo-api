namespace TodoApp.Application.Importacao;

/// <summary>
/// Indica que o arquivo enviado não pôde ser lido como uma planilha .xlsx válida
/// (arquivo corrompido, formato incorreto, etc.). A API traduz isso em 400 — diferente
/// de linhas com dados inválidos, que entram no relatório sem interromper a importação.
/// </summary>
public sealed class PlanilhaInvalidaException : Exception
{
    public PlanilhaInvalidaException(string mensagem, Exception? innerException = null)
        : base(mensagem, innerException)
    {
    }
}
