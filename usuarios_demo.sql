-- Cuentas de demostración. Ejecutar exclusivamente en una base de pruebas.
-- Los hashes se obtuvieron con POST /api/auth/registro.
-- La cuenta jefe se promovió mediante UPDATE antes de exportar su fila.
INSERT INTO usuarios (correo, clave_hash, rol) VALUES
    ('jefe@taller.example', '$2a$11$4R.Uyc9WIjYm5hlReXDVJ.13Os9sxVcoKOywPxrOwUW86XbY3WUPu', 'JefeTaller'),
    ('recepcion@taller.example', '$2a$11$yFKOi/eaMzp96lOUAkpT4.Jg1pLRx4Wpxv5S8FUSw3D0n4Kpvq4uW', 'Recepcionista')
ON CONFLICT (correo) DO NOTHING;
