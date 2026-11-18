namespace Taller.Negocio.Servicios;

public sealed class JwtConfiguracion
{
    public const string RolClaim = "rol";
    public string Clave { get; init; } = "";
    public string Emisor { get; init; } = "";
    public string Audiencia { get; init; } = "";
    public int Minutos { get; init; }
}
