namespace TodoApp.Api.Configuration;

public sealed class JwtSettings
{
    public const string Secao = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public int ExpiraEmMinutos { get; init; } = 60;
}
