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

`gender`: Ver [Enums](#enums).

**curl Request**:
```bash
curl -X POST http://localhost:5000/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Maria",
    "lastName": "Adoptante",
    "email": "maria@test.com",
    "password": "P@ssw0rd!",
    "birthDate": "2005-01-01",
    "gender": 1
  }'
```

**Response 200**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "roles": ["User"]
}
```

### Tokens JWT

* **Algoritmo**: `HmacSha256` con `JWT_SECRET_KEY`.
* **Claims**: `sub=user.Id`, `email`, `jti=guid`, `Role=primer rol`.
* **Duración**: **20 días** (`Expires = UtcNow + 20 días`).
* **Validación**: `ValidateIssuerSigningKey=true`, `ValidateLifetime=true`, `ClockSkew=Zero`, `ValidateIssuer=false`, `ValidateAudience=false`.
* **Uso**: `Authorization: Bearer <token>` en cada request protegida.

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
    REJECTED   // 2
}
```

---

## Pets

Grupo `/pet` — Tag `Mascotas`.

Ver [Enums](#enums) para `Species` y `Gender`.

### POST `/pet`

Crea mascota.

* **Rol**: Requiere `ShelterOwner` o `Dev`.
* **Shelter**: infiere `ShelterId` del usuario autenticado vía JWT.

**curl Request**:
```bash
curl -X POST http://localhost:5000/pet/ \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Firulais",
    "species": 1,
    "breed": "Labrador",
    "gender": 0,
    "temperament": "Juguetón",
    "story": "Rescatado en...",
    "photos": [
      "https://cdn.example.com/firulais1.jpg",
      "https://cdn.example.com/firulais2.jpg"
    ],
    "available": true
  }'
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
  "photos": ["https://cdn.example.com/firulais1.jpg","https://cdn.example.com/firulais2.jpg"],
  "available": true,
  "shelterId": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"
}
```

### GET `/pet`

Lista paginada.

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
      "specie": 1,
      "breed": "Labrador",
      "gender": 0,
      "temperament": "Juguetón",
      "story": "Rescatado en...",
      "photos": ["https://cdn.example.com/firulais1.jpg"],
      "available": true,
      "shelterId": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3
}
```

### GET `/pet/{petId}`

Obtiene una mascota.

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
  "photos": ["https://cdn.example.com/firulais1.jpg"],
  "available": true,
  "shelterId": "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"
}
```

### PATCH `/pet/{petId}`

Actualización parcial.

* **Rol**: Requiere `ShelterOwner` o `Dev` (solo su shelter, `Dev` bypass).

**curl Request**:
```bash
curl -X PATCH http://localhost:5000/pet/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "available": false,
    "photos": ["https://cdn.example.com/new.jpg"]
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
  "photos": ["https://cdn.example.com/new.jpg"],
  "available": false,
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

## Errores comunes

| Código | Mensaje / Causa |
|--------|-----------------|
| `400` | `Email already in use` |
| `400` | `Invalid gender value. Use 0 for MALE or 1 for FEMALE.` |
| `400` | `Invalid credentials` |
| `401` | Sin `Authorization: Bearer` o token expirado (20 días) |
| `403` | `Se requiere rol ShelterOwner.` |
| `403` | `No puedes modificar/borrar mascotas de otro refugio.` |
| `400` | `No Shelter associated with your user` |
| `400` | `Máximo 5 imágenes por mascota.` |
| `400` | `URL de foto inválida` / `URL debe ser http/https` |
| `400` | `Name/Breed/Temperament/Story no puede estar vacío / máximo 100 caracteres` |
| `404` | Pet no existe **o** oculto por `available=false` para `User`/otro shelter |
| `204` | Borrado exitoso en cascada (sin body) |
