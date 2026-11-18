using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Taller.Datos.Repositorios;
using Taller.Entidades;
using Taller.Entidades.DTOs;
using Taller.Negocio.ReglasNegocio;

namespace Taller.Negocio.Servicios;

public sealed class AutenticacionServicio(IUsuarioRepositorio usuarios, JwtConfiguracion jwt)
    : IAutenticacionServicio
{
    private static readonly string HashParaCorreoInexistente =
        BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), workFactor: 11);

    public async Task<PerfilDto> RegistrarAsync(RegistroDto registro, CancellationToken cancellationToken)
    {
        var correo = registro.Correo.Trim().ToLowerInvariant();
        if (await usuarios.BuscarPorCorreoAsync(correo, cancellationToken) is not null)
            throw new ReglaNegocioException("El correo ya está registrado.");

        var usuario = new Usuario
        {
            Correo = correo,
            ClaveHash = BCrypt.Net.BCrypt.HashPassword(registro.Clave, workFactor: 11),
            Rol = RolesTaller.Recepcionista
        };
        usuarios.Agregar(usuario);
        if (!await usuarios.GuardarAsync(cancellationToken))
            throw new ReglaNegocioException("El correo ya está registrado.");

        return new PerfilDto(usuario.Correo, usuario.Rol);
    }

    public async Task<TokenDto?> LoginAsync(LoginDto login, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.BuscarPorCorreoAsync(login.Correo.Trim().ToLowerInvariant(), cancellationToken);
        if (Encoding.UTF8.GetByteCount(login.Clave) > 72 || login.Clave.Contains('\0'))
            return null;

        var coincide = BCrypt.Net.BCrypt.Verify(login.Clave, usuario?.ClaveHash ?? HashParaCorreoInexistente);
        if (!coincide || usuario is null)
            return null;

        var ahora = DateTime.UtcNow;
        var expiracion = DateTimeOffset.FromUnixTimeSeconds(
            new DateTimeOffset(ahora.AddMinutes(jwt.Minutos)).ToUnixTimeSeconds()).UtcDateTime;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
            new Claim(JwtConfiguracion.RolClaim, usuario.Rol)
        };
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(jwt.Emisor, jwt.Audiencia, claims, ahora, expiracion, credenciales);
        return new TokenDto(new JwtSecurityTokenHandler().WriteToken(token), expiracion);
    }
}
