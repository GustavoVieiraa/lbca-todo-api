namespace TodoApp.Api.Configuration;

/// <summary>
/// Credenciais do usuário "semeado" para emissão de token. Em produção isso viria
/// de uma tabela de usuários com senha hasheada — aqui é simplificado para o teste.
/// </summary>
public sealed class AuthSettings
{
    public const string Secao = "Auth";

    public string Usuario { get; init; } = string.Empty;
    public string Senha { get; init; } = string.Empty;
}
