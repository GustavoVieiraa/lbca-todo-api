namespace TodoApp.Api.Configuration;

public sealed class ImportacaoSettings
{
    public const string Secao = "Importacao";

    public long TamanhoMaximoBytes { get; init; } = 10 * 1024 * 1024;
    public string ExtensaoPermitida { get; init; } = ".xlsx";
}
