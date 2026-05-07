# Ejemplo de Integración con API Interna de Inscripciones y Pagos

Este ejemplo demuestra el **flujo completo** de integración entre la API de Admisiones y la API interna de Inscripciones y Pagos.

## 📋 Arquitectura del Ejemplo

```
┌─────────────────────────────────────────────────────────────────────┐
│  Cliente HTTP (Frontend/Postman)                                    │
│  GET /api/EjemploOfertas/persona/12345/ofertas?idProducto=10&...   │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│  EjemploOfertasController                                           │
│  - Validación de entrada                                            │
│  - Logging y auditoría                                              │
│  - Autorización JWT                                                 │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│  OfertasInscripcionService                                          │
│  - Consulta datos locales (BD Admisiones)                           │
│  - Valida que la persona existe                                     │
│  - Verifica código de vigencia                                      │
│  - Lógica de negocio adicional                                      │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│  InscripcionesyPagosApiClient                                       │
│  - Llamada HTTP a API interna                                       │
│  - Serialización/Deserialización                                    │
│  - Manejo de errores HTTP                                           │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│  ServiceAuthenticationHandler (DelegatingHandler)                   │
│  - Intercepta AUTOMÁTICAMENTE la petición HTTP                      │
│  - Inyecta Authorization: Bearer {token_usuario}                    │
│  - Inyecta X-Service-Token: {token_servicio}                        │
│  - Inyecta X-Correlation-Id, X-Source-Service                       │
└────────────────────────┬────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────────┐
│  API Interna: Inscripciones y Pagos                                 │
│  GET /OfertasParaInscripcionAdmisiones                              │
│  - Valida tokens (usuario + servicio)                               │
│  - Retorna ofertas disponibles                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 📁 Archivos del Ejemplo

### 1. **Controller**: `EjemploOfertasController.cs`
- **Ubicación**: `WebApiAdmisiones/Controllers/EjemploOfertasController.cs`
- **Responsabilidades**:
  - Validar parámetros de entrada
  - Autorización (usuario autenticado con JWT)
  - Logging de la petición
  - Delegar al servicio
  - Retornar respuesta HTTP apropiada

### 2. **Servicio**: `OfertasInscripcionService.cs`
- **Ubicación**: `AppLogic/Services/OfertasInscripcionService.cs`
- **Responsabilidades**:
  - Consultar datos de la persona en BD local (Unit of Work)
  - Validar que la persona existe
  - Verificar que tiene código de vigencia
  - Llamar al cliente de API interna
  - Aplicar lógica de negocio adicional
  - Enriquecer/transformar datos de respuesta

### 3. **Cliente API**: `InscripcionesyPagosApiClient.cs`
- **Ubicación**: `AppLogic/ApiClients/InscripcionesyPagosApiClient.cs`
- **Responsabilidades**:
  - Encapsular llamadas HTTP a la API interna
  - Manejo de errores HTTP
  - Serialización/Deserialización JSON
  - Timeout y manejo de excepciones

---

## 🚀 Endpoints del Ejemplo

### 1. Obtener ofertas para una persona específica

```http
GET /api/EjemploOfertas/persona/{codigoPersona}/ofertas?idProducto={id}&idComienzo={id}&idTurno={id}
Authorization: Bearer {jwt_token}
```

**Ejemplo:**
```http
GET /api/EjemploOfertas/persona/12345/ofertas?idProducto=10&idComienzo=2&idTurno=1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Respuesta exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "ofertas": [
      {
        "idOferta": 101,
        "idProducto": 10,
        "idTurno": 1,
        "nombreTurno": "Mañana",
        "idComienzo": 2,
        "nombreComienzo": "Semestre 1 - 2024",
        "cuposDisponibles": 25,
        "disponible": true
      },
      {
        "idOferta": 102,
        "idProducto": 10,
        "idTurno": 1,
        "nombreTurno": "Mañana",
        "idComienzo": 2,
        "nombreComienzo": "Semestre 1 - 2024",
        "cuposDisponibles": 15,
        "disponible": true
      }
    ],
    "totalCount": 2
  },
  "errorCode": null,
  "message": null,
  "httpCode": 200
}
```

**Respuestas de error:**

**404 - Persona no encontrada:**
```json
{
  "success": false,
  "data": null,
  "errorCode": "OFERTAS_PERSONA_01",
  "message": "La persona no existe en el sistema.",
  "httpCode": 404
}
```

**422 - Sin código de vigencia:**
```json
{
  "success": false,
  "data": null,
  "errorCode": "OFERTAS_PERSONA_02",
  "message": "La persona no tiene código de vigencia asignado. Debe completar su registro.",
  "httpCode": 422
}
```

---

### 2. Obtener ofertas del usuario autenticado

```http
GET /api/EjemploOfertas/mis-ofertas?idProducto={id}&idComienzo={id}&idTurno={id}
Authorization: Bearer {jwt_token}
```

**Ejemplo:**
```http
GET /api/EjemploOfertas/mis-ofertas?idProducto=10&idComienzo=2&idTurno=1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

Este endpoint obtiene automáticamente el `codigoPersona` del usuario autenticado desde el JWT.

---

### 3. Obtener datos básicos de una persona

```http
GET /api/EjemploOfertas/persona/{codigoPersona}/datos
Authorization: Bearer {jwt_token}
```

**Ejemplo:**
```http
GET /api/EjemploOfertas/persona/12345/datos
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Respuesta exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "codigoPersona": 12345,
    "primerNombre": "Juan",
    "primerApellido": "Pérez",
    "documento": "12345678",
    "email": "juan.perez@example.com",
    "codigoVigencia": "01",
    ...
  },
  "errorCode": null,
  "message": null,
  "httpCode": 200
}
```

---

## 🔐 Autenticación Automática

El `ServiceAuthenticationHandler` inyecta **automáticamente** estos headers en **TODAS** las peticiones a la API interna:

```http
GET /OfertasParaInscripcionAdmisiones?idProducto=10&idComienzo=2&idTurno=1 HTTP/1.1
Host: api-inscripciones-pagos-desa.ort.edu.uy
Authorization: Bearer eyJhbGc... (token del usuario autenticado)
X-Service-Token: eyJhbGc... (token de servicio a servicio)
X-Source-Service: api-admisiones
X-Correlation-Id: 550e8400-e29b-41d4-a716-446655440000
User-Agent: WebApiAdmisiones/1.0
```

**NO necesitas preocuparte por tokens o headers** - todo se maneja automáticamente.

---

## 🧪 Cómo probar con Postman

### Paso 1: Autenticarse
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "tu_usuario",
  "password": "tu_password"
}
```

Esto te devuelve un JWT que se guarda automáticamente en cookies.

### Paso 2: Consultar ofertas
```http
GET /api/EjemploOfertas/persona/12345/ofertas?idProducto=10&idComienzo=2&idTurno=1
```

Postman envía automáticamente las cookies con el JWT.

---

## 📊 Flujo de Validaciones

```
1. ¿Usuario autenticado? (JWT válido)
   │
   ├─ NO → 401 Unauthorized
   │
   └─ SÍ → Continuar
       │
       2. ¿Parámetros válidos? (codigoPersona > 0, etc.)
          │
          ├─ NO → 400 Bad Request
          │
          └─ SÍ → Continuar
              │
              3. ¿Persona existe en BD local?
                 │
                 ├─ NO → 404 Not Found
                 │
                 └─ SÍ → Continuar
                     │
                     4. ¿Tiene código de vigencia?
                        │
                        ├─ NO → 422 Unprocessable Entity
                        │
                        └─ SÍ → Llamar a API interna
                            │
                            5. ¿API interna responde OK?
                               │
                               ├─ NO → 503 Service Unavailable / 504 Gateway Timeout
                               │
                               └─ SÍ → 200 OK con ofertas
```

---

## 🎯 Puntos clave del diseño

✅ **Separación de responsabilidades**:
- Controller: HTTP, validación, autorización
- Service: Lógica de negocio, validaciones de dominio
- ApiClient: Comunicación HTTP con API externa

✅ **Validaciones en capas**:
- Controller: Validaciones básicas de entrada
- Service: Validaciones de negocio (persona existe, vigencia)
- API interna: Validaciones de autorización y disponibilidad

✅ **Logging completo**:
- Cada capa registra sus operaciones
- Trazabilidad end-to-end con `X-Correlation-Id`

✅ **Manejo robusto de errores**:
- Códigos HTTP específicos para cada tipo de error
- Mensajes de error claros y descriptivos
- Patrón `OperationResult<T>` consistente

✅ **Autenticación transparente**:
- No necesitas manejar tokens manualmente
- `ServiceAuthenticationHandler` lo hace automáticamente

---

## 🔧 Registro de dependencias

El servicio se registra en `DomainServicesExtensions.cs`:

```csharp
// Servicio de ejemplo: integración con API de Inscripciones y Pagos
services.AddScoped<IOfertasInscripcionService, OfertasInscripcionService>();
```

El cliente HTTP se registra en `Program.cs`:

```csharp
// Cliente HTTP para API de Inscripciones y Pagos
builder.Services.AddInscripcionesyPagosApiClient(builder.Configuration);
```

---

## 📝 Próximos pasos

Este ejemplo te sirve como **plantilla** para crear tus propios servicios que integren con la API interna:

1. **Copiar el patrón** de `OfertasInscripcionService`
2. **Agregar tus validaciones** de negocio específicas
3. **Llamar a otros métodos** de `InscripcionesyPagosApiClient`
4. **Enriquecer los datos** con información local si es necesario

---

## 🐛 Debugging

Para ver los logs completos del flujo:

1. Activa el nivel de log en `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "AppLogic.Services.OfertasInscripcionService": "Debug",
      "AppLogic.ApiClients.InscripcionesyPagosApiClient": "Debug",
      "WebApiAdmisiones.HttpHandlers.ServiceAuthenticationHandler": "Debug"
    }
  }
}
```

2. Busca en los logs las siguientes claves:
   - `CodigoPersona`: Identifica la persona consultada
   - `X-Correlation-Id`: Sigue el request end-to-end
   - `ErrorCode`: Identifica errores específicos

---

¿Preguntas? Contacta al equipo de desarrollo.
