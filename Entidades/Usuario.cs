namespace Taller.Entidades;

public sealed class Usuario
{
    public int Id { get; set; }
    public required string Correo { get; set; }
    public required string ClaveHash { get; set; }
    public required string Rol { get; set; }
}
