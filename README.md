# PatitasBackend — PatitasAlRescate API

API REST para gestión de adopción de mascotas, proyecto "Patitas al Rescate". Stack: `ASP.NET Core 10`, `Entity Framework Core + Npgsql`, `ASP.NET Identity` + `JWT Bearer`.
- También puedes ver la [documentación](https://patitasalrescate.galaxym4.dev/docs).

## Errores comunes

| Código | Mensaje / Causa |
|--------|-----------------|
| `400` | `Ese correo ya está en uso` |
| `400` | `Valor de Género inválido. Use 0 para MASCULINO o 1 para FEMENINO.` |
| `400` | `Credenciales inválidas` |
| `401` | Sin `Authorization: Bearer` o token expirado (20 días) |
| `403` | `Se requiere rol ShelterOwner.` |
| `403` | `No puedes modificar/borrar mascotas de otro refugio.` |
| `400` | `No existe Shelter asociado a tu usuario.` |
| `400` | `Ya tienes un refugio registrado. Solo se permite uno por usuario.` |
| `400` | `Máximo 3 imágenes por mascota.` |
| `400` | `Archivo excede 10MB.` / `Archivo vacío.` |
| `400` | `Tipo de imagen no permitido: ... Use jpeg/png/webp.` |
| `400` | `PhotoIndex debe ser 1, 2 o 3.` |
| `400` | `El refugio ya está habilitado` / `El refugio ya está deshabilitado` |
| `400` | `No se puede borrar usuario con refugio asignado` |
| `400` | `Usuario no encontrado` / `Rol no existe: ...` / `El usuario no tiene ese rol` |
| `400` | `El correo ya ha sido verificado` |
| `429` | `Espera 1 minuto antes de volver a solicitar la verificación.` |
| `400` | `Name/Breed/Temperament/Story no puede estar vacío / máximo 100 caracteres` |
| `404` | Pet/Shelter/User no existe **o** oculto por `available=false`/`IsAvailable=false` para `User`/otro shelter |
| `204` | Borrado exitoso en cascada (sin body) |
