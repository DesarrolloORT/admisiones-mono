# 🔐 Integración con API de Inscripciones y Pagos - Service Token

## 📋 Resumen

Esta API (Admisiones) delega funcionalidades complejas de inscripción a una API interna separada (Inscripciones y Pagos).

Para garantizar seguridad y trazabilidad, implementamos un **patrón de doble token**:

| Token | Header | Representa | Generado por |
|-------|--------|------------|--------------|
| **Access Token** | `Authorization: Bearer {token}` | 👤 Usuario final | Al hacer login |
| **Service Token** | `X-Service-Token: {token}` | 🖥️ API llamadora (api-admisiones) | Por cada request a API 2 |

---

## 🏗️ Arquitectura

```
┌─────────────┐
│ Cliente Web │ Login exitoso
└──────┬──────┘
       │ Cookie: X-Access-Token (JWT del usuario)
       ▼
┌──────────────────────────────────────┐
│   API Admisiones (ESTA API)          │
│                                      │
│  ┌────────────────────────────────┐  │
│  │ AdmisionService                │  │
│  │  ↓ llama a                     │  │
│  │ InscripcionesApiClient         │  │
│  │  + ServiceAuthenticationHandler│  │
│  └────────────┬───────────────────┘  │
│               │                      │
│               │ Headers enviados:    │
│               │ 1. Authorization: Bearer {userToken}    │
│               │ 2. X-Service-Token: {serviceToken}     │
└───────────────┼──────────────────────┘
                │
                ▼
┌──────────────────────────────────────────────┐
│   API Inscripciones y Pagos (API EXTERNA)    │
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │ ServiceTokenValidationMiddleware      │  │
│  │  ✅ Valida X-Service-Token            │  │
│  │  ✅ Verifica servicio autorizado      │  │
│  │  ✅ Verifica Authorization existe     │  │
│  └────────────┬───────────────────────────┘  │
│               │                              │
│  ┌────────────▼───────────────────────────┐  │
│  │ JWT Authentication Middleware         │  │
│  │  ✅ Valida Bearer token del usuario   │  │
│  └────────────┬───────────────────────────┘  │
│               │                              │
│  ┌────────────▼───────────────────────────┐  │
│  │ InscripcionesController               │  │
│  │  - User.Identity.Name (del JWT)       │  │
│  │  - Headers["X-Service-Token"]         │  │
│  └────────────────────────────────────────┘  │
└──────────────────────────────────────────────┘
```

---

## 🔧 Componentes Implementados (en esta solución)

### 1️⃣ **IServiceTokenService** / **ServiceTokenService**
- **Ubicación:** `AppLogic/Services/ServiceTokenService.cs`
- **Responsabilidad:** Generar tokens JWT firmados que identifican a esta API
- **Método:** `GenerateServiceToken(string targetApi, params string[] scopes)`

**Ejemplo de token generado:**
```json
{
  "service_name": "api-admisiones",
  "target_api": "api-inscripciones-pagos",
  "scope": "inscripciones.write pagos.read",
  "jti": "a1b2c3d4-e5f6-7890",
  "exp": 1234567890,
  "iss": "api-admisiones",
  "aud": "api-inscripciones-pagos"
}
```

---

### 2️⃣ **ServiceAuthenticationHandler**
- **Ubicación:** `WebApiAdmisiones/HttpHandlers/ServiceAuthenticationHandler.cs`
- **Responsabilidad:** DelegatingHandler que inyecta automáticamente ambos tokens en cada request
- **Qué hace:**
  1. Lee el access token del usuario desde las cookies (usando `CookieAuthenticationHelper`)
  2. Genera un service token con `IServiceTokenService`
  3. Agrega ambos headers al `HttpRequestMessage`

**Headers enviados:**
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
X-Service-Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
X-Source-Service: api-admisiones
X-Correlation-Id: a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

---

### 3️⃣ **InscripcionesApiClient**
- **Ubicación:** `AppLogic/ApiClients/InscripcionesApiClient.cs`
- **Responsabilidad:** Cliente tipado para llamar a la API de Inscripciones
- **Características:**
  - ✅ **SIN reintentos** (evita duplicados en inscripciones)
  - ✅ Timeout claro: 30 segundos
  - ✅ Manejo de errores diferenciado:
    - `INSCRIPCIONES_API_01`: Error de negocio (4xx/5xx)
    - `INSCRIPCIONES_API_TIMEOUT`: Timeout de red (>30s)
    - `INSCRIPCIONES_API_NETWORK`: Error de conexión
    - `INSCRIPCIONES_API_UNEXPECTED`: Error inesperado

**Ejemplo de uso:**
```csharp
public class AdmisionService
{
    private readonly InscripcionesApiClient _inscripcionesClient;

    public async Task<OperationResult<InscripcionResponse>> CrearAdmisionAsync(
        long codigoPersona, long codigoCarrera)
    {
        var request = new InscripcionRequest
        {
            CodigoPersona = codigoPersona,
            CodigoCarrera = codigoCarrera,
            Anio = DateTime.Now.Year
        };

        // El handler inyectará automáticamente los 2 tokens
        var result = await _inscripcionesClient.CrearInscripcionAsync(request);

        if (!result.Success)
        {
            // Manejar error claramente (NO se reintentó)
            _logger.LogError("Error al crear inscripción: {Error}", result.Message);
        }

        return result;
    }
}
```

---

### 4️⃣ **HttpClientExtensions**
- **Ubicación:** `WebApiAdmisiones/Extensions/HttpClientExtensions.cs`
- **Responsabilidad:** Registrar el HttpClient configurado
- **Configuración:**
  - Base URL desde `appsettings.json`
  - Timeout: 30 segundos
  - User-Agent: `WebApiAdmisiones/1.0`
  - **SIN políticas de reintento** (por seguridad en inscripciones)

**Registro en Program.cs:**
```csharp
builder.Services.AddInscripcionesApiClient(builder.Configuration);
```

---

## ⚙️ Configuración Requerida

### **appsettings.json**
```json
{
  "ApiClients": {
    "InscripcionesYPagos": {
      "BaseUrl": "https://api-inscripciones-pagos-desa.ort.edu.uy"
    }
  }
}
```

### **Variables de Entorno**
```sh
# Clave secreta para firmar service tokens (diferente de JWT_SECRET_KEY)
SERVICE_TOKEN_SECRET_KEY=tu_clave_super_secreta_minimo_256_bits_diferente_del_jwt
```

⚠️ **CRÍTICO:**
- Esta clave debe ser **compartida** con la API de Inscripciones y Pagos
- Debe ser **diferente** de `JWT_SECRET_KEY` (separación de concerns)
- Almacenar en Azure Key Vault (no en código)

---

## 🔒 Seguridad

### **¿Por qué dos tokens?**

1. **Access Token (Usuario):**
   - Identifica **quién** es el usuario final
   - Permite auditoría: "El usuario 123456 creó una inscripción"
   - Validado por el middleware JWT de ASP.NET Core

2. **Service Token (Servicio):**
   - Identifica **qué API** está llamando
   - Previene acceso directo desde clientes web
   - Permite whitelist de servicios autorizados
   - Validado por middleware personalizado

### **¿Qué previene?**

❌ **Cliente web → API Inscripciones**: BLOQUEADO (falta X-Service-Token)  
❌ **API no autorizada → API Inscripciones**: BLOQUEADO (service token inválido)  
✅ **API Admisiones → API Inscripciones**: PERMITIDO (ambos tokens válidos)

---

## 📊 Flujo de Datos

### **Request de Ejemplo:**
```http
POST https://api-inscripciones-pagos.ort.edu.uy/api/inscripciones
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTYi...
X-Service-Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzZXJ2aWNlX25hbWUi...
X-Source-Service: api-admisiones
X-Correlation-Id: a1b2c3d4-e5f6-7890-abcd-ef1234567890
Content-Type: application/json

{
  "codigoPersona": 123456,
  "codigoCarrera": 789,
  "anio": 2026
}
```

### **Response de Éxito:**
```json
{
  "success": true,
  "data": {
    "codigoInscripcion": 999888777,
    "estado": "ACTIVA",
    "fechaCreacion": "2026-04-08T10:30:00Z"
  }
}
```

### **Response de Error:**
```json
{
  "success": false,
  "errorCode": "INSCRIPCIONES_API_01",
  "message": "La API de Inscripciones rechazó la solicitud: 409 - Inscripción duplicada",
  "httpCode": 409
}
```

---

## 🚫 ¿Por qué NO hay reintentos?

```csharp
// ❌ MAL: Con reintentos automáticos
await _httpClient.PostAsJsonAsync("/api/inscripciones", request);
// Si falla por timeout, reintenta 3 veces
// → Riesgo de crear 3 inscripciones duplicadas

// ✅ BIEN: Sin reintentos
await _httpClient.PostAsJsonAsync("/api/inscripciones", request);
// Si falla, devuelve error claro al usuario
// → El usuario decide si reintenta manualmente
```

**Principio:** Las **inscripciones son operaciones críticas e idempotentes**. Es mejor fallar de forma clara que crear duplicados.

---

## 📝 Validación en API 2 (Referencia)

⚠️ **NOTA:** Este código NO va en esta solución. Es para el equipo de la API de Inscripciones.

Ver archivo: `WebApiAdmisiones/Middleware/ServiceTokenValidationMiddleware.cs` (solo documentación)

**Configuración en API 2:**
```csharp
// Program.cs de la API de Inscripciones
app.UseMiddleware<ServiceTokenValidationMiddleware>(); // ANTES de auth
app.UseAuthentication();
app.UseAuthorization();
```

---

## 🧪 Testing

### **Escenario 1: Inscripción exitosa**
```csharp
[Fact]
public async Task CrearInscripcion_Success_ReturnsOk()
{
    // Arrange
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(new InscripcionResponse
            {
                CodigoInscripcion = 123,
                Estado = "ACTIVA"
            })
        });

    var httpClient = new HttpClient(mockHandler.Object)
    {
        BaseAddress = new Uri("https://api-test.ort.edu.uy")
    };

    var client = new InscripcionesApiClient(httpClient, Mock.Of<ILogger<InscripcionesApiClient>>());

    // Act
    var result = await client.CrearInscripcionAsync(new InscripcionRequest
    {
        CodigoPersona = 123456,
        CodigoCarrera = 789
    });

    // Assert
    Assert.True(result.Success);
    Assert.Equal(123, result.Data.CodigoInscripcion);
}
```

### **Escenario 2: Timeout**
```csharp
[Fact]
public async Task CrearInscripcion_Timeout_ReturnsError()
{
    // Arrange
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ThrowsAsync(new TaskCanceledException("Timeout", new TimeoutException()));

    // Act
    var result = await client.CrearInscripcionAsync(request);

    // Assert
    Assert.False(result.Success);
    Assert.Equal("INSCRIPCIONES_API_TIMEOUT", result.ErrorCode);
    Assert.Equal(504, result.HttpCode);
}
```

---

## 📚 Referencias

- **Patrón:** Service-to-Service Authentication with JWT
- **Estándar:** OAuth 2.0 Client Credentials Flow (simplificado)
- **Seguridad:** Defense in Depth (doble validación)

---

## ✅ Checklist de Implementación

- [x] Crear `IServiceTokenService` y `ServiceTokenService`
- [x] Crear `ServiceAuthenticationHandler`
- [x] Crear `InscripcionesApiClient`
- [x] Configurar `HttpClientExtensions` (sin reintentos)
- [x] Agregar configuración en `appsettings.json`
- [ ] Configurar `SERVICE_TOKEN_SECRET_KEY` en Azure App Service
- [ ] Registrar en `Program.cs`: `builder.Services.AddInscripcionesApiClient(builder.Configuration);`
- [ ] Testing unitario del cliente
- [ ] Testing de integración con API 2
- [ ] Documentar para equipo de API de Inscripciones
