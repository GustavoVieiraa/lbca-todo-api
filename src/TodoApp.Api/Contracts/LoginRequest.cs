namespace TodoApp.Api.Contracts;

public sealed record LoginRequest(string Usuario, string Senha);

public sealed record LoginResponse(string Token, DateTime ExpiraEm);
