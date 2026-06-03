using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TodoApp.Api.Auth;
using TodoApp.Api.Configuration;
using TodoApp.Api.Contracts;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthSettings _credenciais;
    private readonly IJwtTokenService _tokenService;

    public AuthController(IOptions<AuthSettings> credenciais, IJwtTokenService tokenService)
    {
        _credenciais = credenciais.Value;
        _tokenService = tokenService;
    }

    /// <summary>Autentica o usuário e devolve um token JWT.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        if (!CredenciaisValidas(request))
            return Unauthorized();

        var (token, expiraEm) = _tokenService.GerarToken(request.Usuario);
        return Ok(new LoginResponse(token, expiraEm));
    }

    private bool CredenciaisValidas(LoginRequest request)
        => string.Equals(request.Usuario, _credenciais.Usuario, StringComparison.Ordinal)
        && string.Equals(request.Senha, _credenciais.Senha, StringComparison.Ordinal);
}
