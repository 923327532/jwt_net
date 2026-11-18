using System.ComponentModel.DataAnnotations;

namespace Taller.Entidades.DTOs;

public sealed record RegistroDto(
    [Required, EmailAddress, StringLength(254)] string Correo,
    [Required, StringLength(72, MinimumLength = 8)] string Clave);

public sealed record LoginDto(
    [Required, EmailAddress, StringLength(254)] string Correo,
    [Required, StringLength(72)] string Clave);

public sealed record TokenDto(string Token, DateTime Expiracion);
public sealed record PerfilDto(string Correo, string Rol);
