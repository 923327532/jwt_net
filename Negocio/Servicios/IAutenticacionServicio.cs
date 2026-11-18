using Taller.Entidades.DTOs;

namespace Taller.Negocio.Servicios;

public interface IAutenticacionServicio
{
    Task<PerfilDto> RegistrarAsync(RegistroDto registro, CancellationToken cancellationToken);
    Task<TokenDto?> LoginAsync(LoginDto login, CancellationToken cancellationToken);
}
