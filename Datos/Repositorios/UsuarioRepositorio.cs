using Microsoft.EntityFrameworkCore;
using Npgsql;
using Taller.Entidades;

namespace Taller.Datos.Repositorios;

public sealed class UsuarioRepositorio(TallerDbContext db) : IUsuarioRepositorio
{
    public Task<Usuario?> BuscarPorCorreoAsync(string correo, CancellationToken cancellationToken) =>
        db.Usuarios.AsNoTracking().SingleOrDefaultAsync(usuario => usuario.Correo == correo, cancellationToken);

    public void Agregar(Usuario usuario) => db.Usuarios.Add(usuario);

    public async Task<bool> GuardarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "usuarios_correo_key" })
        {
            foreach (var entry in error.Entries)
                entry.State = EntityState.Detached;

            return false;
        }
    }
}
