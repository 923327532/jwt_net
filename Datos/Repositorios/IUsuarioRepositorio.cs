using Taller.Entidades;

namespace Taller.Datos.Repositorios;

public interface IUsuarioRepositorio
{
    Task<Usuario?> BuscarPorCorreoAsync(string correo, CancellationToken cancellationToken);
    void Agregar(Usuario usuario);
    Task<bool> GuardarAsync(CancellationToken cancellationToken);
}
