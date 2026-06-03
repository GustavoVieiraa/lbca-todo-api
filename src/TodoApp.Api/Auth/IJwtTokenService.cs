namespace TodoApp.Api.Auth;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiraEm) GerarToken(string usuario);
}
