param(
    [string]$BaseUrl = 'http://127.0.0.1:5221',
    [Parameter(Mandatory)][string]$ClavePruebas,
    [string]$CarpetaEvidencias = (Join-Path $PSScriptRoot '../evidencias')
)

$ErrorActionPreference = 'Stop'
$script:Casos = [System.Collections.Generic.List[object]]::new()

function Probar {
    param([string]$Metodo, [string]$Ruta, [int]$Esperado, [string]$Actor, $Cuerpo, [string]$Token)
    $opciones = @{ Uri = "$BaseUrl$Ruta"; Method = $Metodo; UseBasicParsing = $true }
    if ($null -ne $Cuerpo) {
        $opciones.ContentType = 'application/json; charset=utf-8'
        $opciones.Body = $Cuerpo | ConvertTo-Json -Compress
    }
    if ($Token) { $opciones.Headers = @{ Authorization = "Bearer $Token" } }
    try {
        $respuesta = Invoke-WebRequest @opciones
        $codigo = [int]$respuesta.StatusCode
        $json = $respuesta.Content
    }
    catch {
        if (-not $_.Exception.Response) { throw }
        $codigo = [int]$_.Exception.Response.StatusCode
        $json = $_.ErrorDetails.Message
    }
    $contenido = if ($json) { $json | ConvertFrom-Json }
    $evidencia = if ($contenido.token) {
        [ordered]@{ token = '[omitido]'; expiracion = $contenido.expiracion }
    } else { $contenido }
    $script:Casos.Add([ordered]@{
        actor = $Actor; metodo = $Metodo; ruta = $Ruta; esperado = $Esperado
        obtenido = $codigo; respuesta = $evidencia
    })
    Write-Host "$codigo $Actor $Metodo $Ruta"
    if ($codigo -ne $Esperado) { throw "Se esperaba HTTP $Esperado." }
    return $contenido
}

function Leer-Claims([string]$Token) {
    $payload = $Token.Split('.')[1].Replace('-', '+').Replace('_', '/')
    $payload = $payload.PadRight($payload.Length + (4 - $payload.Length % 4) % 4, '=')
    [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload)) | ConvertFrom-Json
}

New-Item -ItemType Directory -Force $CarpetaEvidencias | Out-Null
try {
    $tag = [Guid]::NewGuid().ToString('N').Substring(0, 7)
    $cuenta = @{ correo = "prueba-$tag@taller.example"; clave = $ClavePruebas }
    $registro = Probar POST /api/auth/registro 201 'Sin token' ($cuenta + @{ rol = 'JefeTaller' })
    if ($registro.rol -ne 'Recepcionista') { throw 'El servidor debe asignar el rol Recepcionista.' }
    $null = Probar POST /api/auth/registro 409 'Sin token' @{ correo = $cuenta.correo.ToUpperInvariant(); clave = $ClavePruebas }
    $null = Probar POST /api/auth/registro 400 'Sin token' @{ correo = 'invalido'; clave = $ClavePruebas }
    $null = Probar POST /api/auth/login 401 'Correo inexistente' @{ correo = "ausente-$tag@taller.example"; clave = $ClavePruebas }
    $null = Probar POST /api/auth/login 401 'Clave incorrecta' @{ correo = $cuenta.correo; clave = 'ClaveIncorrecta' }
    $recep = (Probar POST /api/auth/login 200 'Recepcionista' @{ correo = 'recepcion@taller.example'; clave = $ClavePruebas }).token
    $jefe = (Probar POST /api/auth/login 200 'JefeTaller' @{ correo = 'jefe@taller.example'; clave = $ClavePruebas }).token
    $claims = [ordered]@{ Recepcionista = Leer-Claims $recep; JefeTaller = Leer-Claims $jefe }
    $claims | ConvertTo-Json -Depth 8 | Set-Content "$CarpetaEvidencias/claims.json" -Encoding utf8

    $null = Probar GET /api/auth/perfil 401 'Sin token'
    $perfil = Probar GET /api/auth/perfil 200 'Recepcionista' $null $recep
    $perfilJefe = Probar GET /api/auth/perfil 200 'JefeTaller' $null $jefe
    if ($perfil.rol -ne 'Recepcionista' -or $perfilJefe.rol -ne 'JefeTaller') { throw 'Roles inesperados.' }

    $null = Probar GET /api/vehiculos 401 'Sin token'
    $vehiculo = @{ placa = "L$($tag.ToUpperInvariant())"; marca = 'Toyota'; modelo = 'Yaris'; anio = 2024; clienteNombre = 'Cliente de prueba' }
    $null = Probar POST /api/vehiculos 403 'JefeTaller' $vehiculo $jefe
    $v = Probar POST /api/vehiculos 201 'Recepcionista' $vehiculo $recep
    $vid = $v.id
    $null = Probar POST /api/vehiculos 409 'Recepcionista' $vehiculo $recep
    $null = Probar POST /api/vehiculos 400 'Recepcionista' @{} $recep
    $null = Probar GET /api/vehiculos 200 'Recepcionista' $null $recep
    $null = Probar GET "/api/vehiculos/$vid" 200 'JefeTaller' $null $jefe
    $null = Probar PUT "/api/vehiculos/$vid" 204 'Recepcionista' ($vehiculo + @{ id = $vid; color = 'Azul' }) $recep

    $orden = @{ vehiculoId = $vid; descripcion = 'Revision de frenos'; estado = 'Pendiente'; costoEstimado = 150 }
    $o = Probar POST /api/ordenes 201 'Recepcionista' $orden $recep
    $oid = $o.id
    $null = Probar GET "/api/vehiculos/$vid/ordenes" 200 'Recepcionista' $null $recep
    $null = Probar GET "/api/ordenes?vehiculoId=$vid" 200 'JefeTaller' $null $jefe
    $null = Probar GET "/api/ordenes/$oid" 200 'Recepcionista' $null $recep
    $orden.estado = 'EnProceso'
    $orden.id = $oid
    $null = Probar PUT "/api/ordenes/$oid" 403 'Recepcionista' $orden $recep
    $null = Probar PUT "/api/ordenes/$oid" 204 'JefeTaller' $orden $jefe
    $null = Probar DELETE "/api/vehiculos/$vid" 409 'JefeTaller' $null $jefe
    $null = Probar DELETE "/api/ordenes/$oid" 204 'JefeTaller' $null $jefe
    $null = Probar GET "/api/ordenes/$oid" 404 'JefeTaller' $null $jefe
    $null = Probar DELETE "/api/vehiculos/$vid" 403 'Recepcionista' $null $recep
    $null = Probar DELETE "/api/vehiculos/$vid" 204 'JefeTaller' $null $jefe
    $null = Probar GET "/api/vehiculos/$vid" 404 'Recepcionista' $null $recep

    $openapi = Invoke-RestMethod "$BaseUrl/swagger/v1/swagger.json"
    if ($openapi.components.securitySchemes.Bearer.scheme -ne 'bearer' -or
        $null -eq $openapi.paths.'/api/auth/perfil'.get.security[0].Bearer -or
        $null -ne $openapi.paths.'/api/auth/login'.post.security) { throw 'La configuración de Swagger es incorrecta.' }
    if ((Invoke-WebRequest "$BaseUrl/swagger/index.html" -UseBasicParsing).StatusCode -ne 200) { throw 'Swagger UI no responde.' }
}
finally {
    ConvertTo-Json -InputObject @($script:Casos.ToArray()) -Depth 16 | Set-Content "$CarpetaEvidencias/pruebas.json" -Encoding utf8
}
