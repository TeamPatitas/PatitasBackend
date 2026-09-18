# PatitasBackend — PatitasAlRescate API

API REST para gestión de adopción de mascotas. Stack: `ASP.NET Core 10`, `Entity Framework Core + Npgsql`, `ASP.NET Identity` + `JWT Bearer`.

---

# Politicas

## Roles del proyecto

| Rol | Descripción |
|-----|-------------|
| `Dev` | Super-admin, acceso total. Hereda todas las políticas. |
| `ShelterOwner` | Dueño/voluntario de refugio. Gestiona mascotas de **su** refugio. |
| `User` | Adoptante general. Solo lectura de mascotas disponibles. |

### Jerarquía de políticas

* Requiere `Dev`.
* Requiere `ShelterOwner` o `Dev`.
* Requiere `User` o `ShelterOwner` o `Dev`.

`Dev` pasa cualquier endpoint que requiera `ShelterOwner` o `User`. `ShelterOwner` pasa endpoints de `User`.

---

# Endpoints

Base URL local: `http://localhost:5000`.

Autenticación: header `Authorization: Bearer <JWT>`.

## Autenticación

### POST `/auth/login`

**curl Request**:
```bash
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@patitas.com",
    "password": "tu_password"
  }'
```

**Response 200**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "roles": ["Dev"]
}
```

### POST `/auth/register`

- `gender`: Ver [Enums](#enums). 
- `Photo` opcional `IFormFile` → `users/{id}.webp` (`Max 10MB`, `jpeg/png/webp`).

**curl Request**:
```bash
curl -X POST http://localhost:5000/auth/register \
  -H "Authorization: Bearer $TOKEN" \
  -F "FirstName=Maria" \
  -F "LastName=Adoptante" \
  -F "Email=maria@test.com" \
  -F "Password=P@ssw0rd!" \
  -F "BirthDate=2005-01-01" \
  -F "Gender=1" \
  -F "Photo=@avatar.jpg"
```

**Response 200**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "roles": ["User"]
}
```

### POST `/auth/verify-email`

Verifica email con token enviado por correo. `VerifyEmailRequest` con `UserId` y `Token` (token `Base64Url`).

**curl Request**:
```bash
curl -X POST http://localhost:5000/auth/verify-email \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
    "token": "CfDJ8..."
  }'
```

**Response 200**:
```json
"Correo verificado correctamente"
```

### GET `/auth/send-verification-email`

Reenvía correo de verificación. Cooldown 1 minuto por IP (`429`).

* **Rol**: Requiere `User` o `ShelterOwner` o `Dev`.

**curl Request**:
```bash
curl http://localhost:5000/auth/send-verification-email \
  -H "Authorization: Bearer $TOKEN"
```

**Response 200**:
```json
"Correo de verificación enviado correctamente"
```
`429` `{"error":"Espera 1 minuto antes de volver a solicitar la verificación."}`

### Tokens JWT

* **Algoritmo**: `HmacSha256` con `JWT_SECRET_KEY`.
* **Claims**: `sub=user.Id`, `email`, `jti=guid`, `Role=primer rol`.
* **Duración**: **20 días** (`Expires = UtcNow + 20 días`).
* **Validación**: 
```
  ValidateIssuerSigningKey=true, 
  ValidateLifetime=true, 
  ClockSkew=Zero, 
  ValidateIssuer=false, 
  ValidateAudience=false.
```
* **Uso**: `Authorization: Bearer <token>` en cada request protegida.

## Usuario

### GET `/user`

Obtiene perfil actual. Dominio base `/user`.

* **Rol**: Requiere `User` o `ShelterOwner` o `Dev`.

**curl Request**:
```bash
curl http://localhost:5000/user \
  -H "Authorization: Bearer $TOKEN"
```

**Response 200**:
```json
{
  "id": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "firstName": "Maria",
  "lastName": "Adoptante",
  "email": "maria@test.com",
  "isEmailConfirmed": true,
  "gender": 1,
  "photoUrl": "https://r2.example.com/users/a1b2c3d4.webp",
  "birthDate": "2005-01-01",
  "role": "User",
  "shelterId": null
}
```

### PATCH `/user`

- Actualiza perfil.
- Atributo `Photo` opcional `users/{id}.webp` (si ya tiene foto la nueva lo reemplaza, `Max 10MB`). 
- Resto campos opcionales `FirstName`, `LastName`, `BirthDate`, `Gender` (Ver [Enums](#enums)).

* **Rol**: Requiere `User` o `ShelterOwner` o `Dev`.

**curl Request**:
```bash
curl -X PATCH http://localhost:5000/user \
  -H "Authorization: Bearer $TOKEN" \
  -F "FirstName=Maria" \
  -F "Photo=@avatar-new.jpg"
```

**Response 200**:
```json
{
  "id": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "firstName": "Maria",
  "lastName": "Adoptante",
  "email": "maria@test.com",
  "isEmailConfirmed": true,
  "gender": 1,
  "photoUrl": "https://r2.example.com/users/a1b2c3d4.webp",
  "birthDate": "2005-01-01",
  "role": "User",
  "shelterId": null
}
```

### DELETE `/user/{id}`

- Borra la cuenta (Referencia 🗣️🗣️). 
- Solo puede ejecutar el mismo usuario `id` o `Dev`. 
- Rol DEV puede borrar a cualquier usuario.
- Si tiene un refugio asignado (`ShelterId`) bloqueará y retornará error `400`
- Si se borra todos sus Adopciones pasarán de `REQUESTED` → `CANCELLED`.

* **Rol**: Requiere `User` o `ShelterOwner` o `Dev`.

**curl Request**:
```bash
curl -X DELETE http://localhost:5000/user/a1b2c3d4-e5f6-7890-1234-567890abcdef \
  -H "Authorization: Bearer $TOKEN"
```

**Response 200**:
```json
"Usuario borrado"
```
`403` si no es propio ni `Dev`, `400` si tiene refugio.

---

## Enums

```csharp
public enum Species
{
    OTHER, // 0
    DOG,   // 1
    CAT    // 2
}

public enum Gender
{
    MALE,   // 0
    FEMALE  // 1
}

public enum AdoptionStatus
{
    REQUESTED, // 0
    APPROVED,  // 1
    REJECTED,  // 2
    CANCELLED  // 3
}
```

---

## Pets

Grupo `/pet` — Tag `Mascotas`.

Ver [Enums](#enums) para `Species` y `Gender`. Imágenes R2 `CloudflareR2Service`: `pets/{petId}/{petId}-{n}.webp` `n=1..3`, `max 3`, `10MB` c/u, `jpeg/png/webp` → `webp Q75` `1024px`.

### POST `/pet`

Crea mascota. `[FromForm] CreatePetRequest` con `Photos` opcional.

* **Rol**: Requiere `ShelterOwner` o `Dev`.
* **Shelter**: infiere `ShelterId` del usuario autenticado vía JWT.

**curl Request**:
```bash
curl -X POST http://localhost:5000/pet/ \
  -H "Authorization: Bearer $TOKEN" \
  -F "Name=Firulais" \
  -F "Species=1" \
  -F "Breed=Labrador" \
  -F "Gender=0" \
  -F "Temperament=Juguetón" \
  -F "Story=Rescatado en..." \
  -F "Available=true" \
  -F "Photos=@firulais1.jpg" \
  -F "Photos=@firulais2.jpg"
```

**Response 201**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Firulais",
  "specie": 1,
  "breed": "Labrador",
  "gender": 0,
  "temperament": "Juguetón",
  "story": "Rescatado en...",
  "photos": [
    "https://r2.example.com/pets/550e8400/pets-550e8400-1.webp",
    "https://r2.example.com/pets/550e8400/pets-550e8400-2.webp"
  ],
  "available": true,
  "shelterId": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"
}
```

### GET `/pet`

Lista paginada resumida `PetSummaryResponse`.

* **Rol**: Requiere `User` o `ShelterOwner` o `Dev`.
* **Query**: `?page=1&pageSize=20` — `page≥1`, `pageSize 1-50`, orden `Name ASC`.
* **Filtrado por rol**: `User` → solo `available=true` global. `ShelterOwner` → solo su shelter (incluye `available=false`). `Dev` → todo.

**curl Request**:
```bash
curl "http://localhost:5000/pet?page=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"
```

**Response 200**:
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Firulais",
      "photos": ["https://r2.example.com/pets/550e8400/pets-550e8400-1.webp"],
      "available": true
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3
}
```

### GET `/pet/{petId}`

Obtiene una mascota detallada.

* **Rol**: Requiere `User` o `ShelterOwner` o `Dev`.
* **Visibilidad**: `User` ve `404` si `available=false`. `ShelterOwner` ve `false` solo si es su shelter; `Dev` ve todo.

**curl Request**:
```bash
curl http://localhost:5000/pet/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer $TOKEN"
```

**Response 200**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Firulais",
  "specie": 1,
  "breed": "Labrador",
  "gender": 0,
  "temperament": "Juguetón",
  "story": "Rescatado en...",
  "photos": ["https://r2.example.com/pets/550e8400/pets-550e8400-1.webp"],
  "available": true,
  "shelterId": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"
}
```

### PATCH `/pet/{petId}`

Actualización parcial (sin fotos).

* **Rol**: Requiere `ShelterOwner` o `Dev` (solo su shelter, `Dev` bypass).

**curl Request**:
```bash
curl -X PATCH http://localhost:5000/pet/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "available": false
  }'
```

**Response 200**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Firulais",
  "specie": 1,
  "breed": "Labrador",
  "gender": 0,
  "temperament": "Juguetón",
  "story": "Rescatado en...",
  "photos": ["https://r2.example.com/pets/550e8400/pets-550e8400-1.webp"],
  "available": false,
  "shelterId": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"
}
```

### PATCH `/pet/{petId}/photo/{photoIndex}`

Reemplaza foto `n=1..3` en `pets/{id}/{id}-{n}.webp` (overwrite `webp Q75`).

* **Rol**: Requiere `ShelterOwner` o `Dev`.

**curl Request**:
```bash
curl -X PATCH http://localhost:5000/pet/550e8400-e29b-41d4-a716-446655440000/photo/1 \
  -H "Authorization: Bearer $TOKEN" \
  -F "photo=@new1.jpg"
```

**Response 200**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Firulais",
  "specie": 1,
  "breed": "Labrador",
  "gender": 0,
  "temperament": "Juguetón",
  "story": "Rescatado en...",
  "photos": [
    "https://r2.example.com/pets/550e8400/pets-550e8400-1.webp",
    "https://r2.example.com/pets/550e8400/pets-550e8400-2.webp"
  ],
  "available": true,
  "shelterId": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"
}
```

### DELETE `/pet/{petId}`

Borrado físico en cascada.

* **Rol**: Requiere `ShelterOwner` o `Dev` (solo su shelter, `Dev` bypass).
* **Efecto**: hard delete — borra favoritos y adopciones asociados. Irreversible.

**curl Request**:
```bash
curl -X DELETE http://localhost:5000/pet/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer $TOKEN"
```

**Response**: `204 NoContent` (sin body).

---

## Refugios

Grupo `/shelter` — Tag `Refugios`. Foto perfil `shelters/{id}.webp` (`10MB`, `webp Q75`, `jpeg/png/webp`, opcional, `null` permitido, patch reemplaza).

### POST `/shelter`

Crea refugio y asigna `ShelterId` al creador. Límite 1 por usuario (`User` y `Dev`).

* **Rol**: Requiere `User` o `ShelterOwner` o `Dev`.

**curl Request**:
```bash
curl -X POST http://localhost:5000/shelter/ \
  -H "Authorization: Bearer $TOKEN" \
  -F "Name=Refugio Patitas" \
  -F "Address=Calle 123" \
  -F "Latitude=-34.6" \
  -F "Longitude=-58.4" \
  -F "Photo=@refugio.jpg"
```

**Response 201**:
```json
{
  "id": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11",
  "name": "Refugio Patitas",
  "address": "Calle 123",
  "isAvailable": false,
  "latitude": -34.6,
  "longitude": -58.4,
  "photoUrl": "https://r2.example.com/shelters/a0eebc99.webp"
}
```

### PATCH `/shelter/{id}`

Actualiza datos y/o foto (`Photo` opcional `shelters/{id}.webp` si existe reemplaza).

* **Rol**: Requiere `User` o `ShelterOwner` o `Dev` (owner o `Dev`).

**curl Request**:
```bash
curl -X PATCH http://localhost:5000/shelter/a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11 \
  -H "Authorization: Bearer $TOKEN" \
  -F "Name=Refugio Actualizado" \
  -F "Photo=@new-refugio.jpg"
```

**Response 200**:
```json
{
  "id": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11",
  "name": "Refugio Actualizado",
  "address": "Calle 123",
  "isAvailable": true,
  "latitude": -34.6,
  "longitude": -58.4,
  "photoUrl": "https://r2.example.com/shelters/a0eebc99.webp"
}
```

### PATCH `/shelter/enable/{id}`

Habilita refugio (`IsAvailable=true`).

* **Rol**: Requiere `Dev`.

**curl Request**:
```bash
curl -X PATCH http://localhost:5000/shelter/enable/a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11 \
  -H "Authorization: Bearer $TOKEN"
```

**Response 200**:
```json
{
  "id": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11",
  "name": "Refugio Patitas",
  "address": "Calle 123",
  "isAvailable": true,
  "latitude": -34.6,
  "longitude": -58.4,
  "photoUrl": "https://r2.example.com/shelters/a0eebc99.webp"
}
```
`400 El refugio ya está habilitado` si ya `true`.

### PATCH `/shelter/disable/{id}`

Deshabilita refugio (`IsAvailable=false`).

* **Rol**: Requiere `Dev`.

**curl Request**:
```bash
curl -X PATCH http://localhost:5000/shelter/disable/a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11 \
  -H "Authorization: Bearer $TOKEN"
```

**Response 200**:
```json
{
  "id": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11",
  "name": "Refugio Patitas",
  "address": "Calle 123",
  "isAvailable": false,
  "latitude": -34.6,
  "longitude": -58.4,
  "photoUrl": "https://r2.example.com/shelters/a0eebc99.webp"
}
```
`400 El refugio ya está deshabilitado` si ya `false`.

### GET `/shelter`

Lista paginada.

* **Rol**: Requiere `User` o `ShelterOwner` o `Dev`.
* **Query**: `?page=1&pageSize=20` — `page≥1`, `pageSize 1-50`, orden `Name ASC`.
* **Filtrado**: `User/ShelterOwner` → solo `IsAvailable=true`. `Dev` → todo.

**curl Request**:
```bash
curl "http://localhost:5000/shelter?page=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"
```

**Response 200**:
```json
{
  "items": [
    {
      "id": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11",
      "name": "Refugio Patitas",
      "address": "Calle 123",
      "isAvailable": true,
      "latitude": -34.6,
      "longitude": -58.4,
      "photoUrl": "https://r2.example.com/shelters/a0eebc99.webp"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 5,
  "totalPages": 1
}
```

### GET `/shelter/{id}`

Obtiene refugio por id.

* **Rol**: Requiere `User` o `ShelterOwner` o `Dev`.
* **Visibilidad**: `IsAvailable=false` solo `Dev` o dueño (`user.ShelterId == id`), resto `404`.

**curl Request**:
```bash
curl http://localhost:5000/shelter/a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11 \
  -H "Authorization: Bearer $TOKEN"
```

**Response 200**:
```json
{
  "id": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11",
  "name": "Refugio Patitas",
  "address": "Calle 123",
  "isAvailable": true,
  "latitude": -34.6,
  "longitude": -58.4,
  "photoUrl": "https://r2.example.com/shelters/a0eebc99.webp"
}
```

### DELETE `/shelter/{id}`

Borra refugio en cascada (pets, events, adopciones, favoritos). Irreversible.

* **Rol**: Requiere `ShelterOwner` o `Dev` (owner o `Dev`), si no `403`.

**curl Request**:
```bash
curl -X DELETE http://localhost:5000/shelter/a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11 \
  -H "Authorization: Bearer $TOKEN"
```

**Response 200**:
```json
"Refugio Borrado"
```
`404` si no existe, `403` si no es dueño ni `Dev`.

---

## Health

### GET `/health`

Verifica servicios externos. Solo `Dev`.

* **Rol**: Requiere `Dev`.

**curl Request**:
```bash
curl http://localhost:5000/health \
  -H "Authorization: Bearer $TOKEN"
```

**Response 200** (todo `UP`):
```json
{
  "status": "UP",
  "services": {
    "database": { "status": "UP", "latencyMs": 12.3 },
    "storage": { "status": "UP", "latencyMs": 45.6 },
    "email": { "status": "UP", "latencyMs": 22.1 },
    "api": { "status": "UP", "latencyMs": 0 }
  }
}
```
**Response 503** si algún `DOWN`, mantiene `services` con `error`.

---

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
| `400` | `Usuario no encontrado` |
| `400` | `El correo ya ha sido verificado` |
| `429` | `Espera 1 minuto antes de volver a solicitar la verificación.` |
| `400` | `Name/Breed/Temperament/Story no puede estar vacío / máximo 100 caracteres` |
| `404` | Pet/Shelter/User no existe **o** oculto por `available=false`/`IsAvailable=false` para `User`/otro shelter |
| `204` | Borrado exitoso en cascada (sin body) |
