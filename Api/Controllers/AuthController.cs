using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Taller.Entidades.DTOs;
using Taller.Negocio.ReglasNegocio;
using Taller.Negocio.Servicios;

namespace Taller.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public sealed class AuthController(IAutenticacionServicio autenticacion) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("registro")]
    public async Task<ActionResult<PerfilDto>> Registro(RegistroDto registro, CancellationToken cancellationToken)
    {
        if (Encoding.UTF8.GetByteCount(registro.Clave) > 72 || registro.Clave.Contains('\0'))
        {
            ModelState.AddModelError(nameof(registro.Clave), "La clave admite hasta 72 bytes UTF-8 y no admite caracteres nulos.");
            return ValidationProblem(ModelState);
        }

        try
        {
            return StatusCode(StatusCodes.Status201Created,
                await autenticacion.RegistrarAsync(registro, cancellationToken));
        }
        catch (ReglaNegocioException error)
        {
            return Conflict(new { mensaje = error.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<TokenDto>> Login(LoginDto login, CancellationToken cancellationToken)
    {
        var token = await autenticacion.LoginAsync(login, cancellationToken);
        if (token is null)
            return Unauthorized();

        return Ok(token);
    }

    [HttpGet("perfil")]
    public ActionResult<PerfilDto> Perfil()
    {
        var correo = User.FindFirstValue(JwtRegisteredClaimNames.Email);
        var rol = User.FindFirstValue(JwtConfiguracion.RolClaim);
        if (correo is null || rol is null)
            return Unauthorized();

        return Ok(new PerfilDto(correo, rol));
    }
}
