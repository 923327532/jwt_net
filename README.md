# Laboratorio 14 · API de taller mecánico con JWT

Continuación del [Laboratorio 13, commit 17802b4](https://github.com/923327532/.netwithoracle/commit/17802b4885e22c2ba09a6b778f3dc85261856044). Conserva los cuatro proyectos, PostgreSQL, las rutas, los DTO y el `MappingProfile` del taller.

## Ejecutar con .NET 10 y PostgreSQL

1. En una base local vacía, ejecuta [database.sql](database.sql). En una base existente de S13, comienza por el siguiente paso.
2. Ejecuta [usuarios.sql](usuarios.sql) para crear la tabla. En una base de demostración, ejecuta también [usuarios_demo.sql](usuarios_demo.sql) para insertar un usuario de cada rol.
3. Configura la conexión y una clave JWT aleatoria de al menos 32 caracteres con Secret Manager en Development o con las variables `ConnectionStrings__DefaultConnection` y `Jwt__Clave`. `Jwt:Emisor`, `Jwt:Audiencia` y `Jwt:Minutos` están en [appsettings.json](Api/appsettings.json) y se pueden sobrescribir por ambiente.

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<cadena PostgreSQL local>" --project Api
   $bytes = New-Object byte[] 32
   $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
   $rng.GetBytes($bytes)
   $rng.Dispose()
   dotnet user-secrets set "Jwt:Clave" ([Convert]::ToBase64String($bytes)) --project Api
   dotnet run --project Api --launch-profile http
   ```

4. Abre Swagger en `http://localhost:5221/swagger`. Inicia sesión en `POST /api/auth/login`. En **Authorize**, pega el valor de `token` y ejecuta `GET /api/auth/perfil`.
5. Para reproducir las pruebas, ejecuta el script con la API y las cuentas de demostración disponibles.

   ```powershell
   ./scripts/Probar-Lab14.ps1 -ClavePruebas 'TallerDemo-S14!2026'
   ```

| Cuenta local de prueba | Rol |
|---|---|
| `recepcion@taller.example` | Recepcionista |
| `jefe@taller.example` | JefeTaller |

La contraseña de ambas cuentas de demostración es `TallerDemo-S14!2026`. Los hashes del script provienen de registrar las cuentas por la API y promover al jefe con `UPDATE usuarios SET rol = 'JefeTaller' WHERE correo = 'jefe@taller.example';`.

## Entrega

- [Informe con matriz de permisos y sustentación](Informe-Lab14.md)
- [Pruebas HTTP y respuestas](evidencias/pruebas.json)
- [Claims de los dos tokens](evidencias/claims.json)
- [Captura del JWT decodificado](evidencias/01_claims_jwt.png)
- [Capturas de la ejecución 1](evidencias/02_pruebas_http.png), [2](evidencias/03_pruebas_http.png) y [3](evidencias/04_pruebas_http.png)
- [Captura de Authorize en Swagger](evidencias/05_swagger_authorize.png)
