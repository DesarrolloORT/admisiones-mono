# 🚀 Escalabilidad de la Arquitectura Service Token

## ✅ Respuesta Corta

**SÍ, la arquitectura funciona para múltiples endpoints.**

El `ServiceAuthenticationHandler` se aplica **a nivel de HttpClient**, por lo que **TODOS los métodos** (GET, POST, PUT, DELETE) del cliente inyectan automáticamente los dos tokens.

---

## 🏗️ Arquitectura Escalable

### **1 HttpClient = 1 API Destino**

```csharp
// Un solo HttpClient configurado
services.AddHttpClient<InscripcionesApiClient>(...)
    .AddHttpMessageHandler<ServiceAuthenticationHandler>(); // Se aplica a TODOS los requests

// Múltiples métodos en el mismo cliente
public class InscripcionesApiClient
{
    // ✅ GET → Tokens inyectados automáticamente
    public async Task<...> ObtenerInscripcionesAsync(long codigoPersona)
    {
        return await _httpClient.GetAsync($"/api/inscripciones?codigoPersona={codigoPersona}");
    }

    // ✅ GET → Tokens inyectados automáticamente
    public async Task<...> ObtenerPagosAsync(long codigoPersona)
    {
        return await _httpClient.GetAsync($"/api/pagos?codigoPersona={codigoPersona}");
    }

    // ✅ POST → Tokens inyectados automáticamente
    public async Task<...> CrearInscripcionAsync(InscripcionRequest request)
    {
        return await _httpClient.PostAsJsonAsync("/api/inscripciones", request);
    }

    // ✅ POST → Tokens inyectados automáticamente
    public async Task<...> RegistrarPagoAsync(PagoRequest request)
    {
        return await _httpClient.PostAsJsonAsync("/api/pagos", request);
    }

    // ✅ PUT, DELETE, PATCH... todos inyectan tokens
}
```

---

## 📊 Headers Enviados en TODOS los Métodos

**Request de ejemplo (GET):**
```http
GET https://api-inscripciones-pagos.ort.edu.uy/api/inscripciones?codigoPersona=123456
Authorization: Bearer eyJhbGc...  (token del usuario)
X-Service-Token: eyJhbGc...       (token del servicio)
X-Source-Service: api-admisiones
X-Correlation-Id: a1b2c3d4-...
```

**Request de ejemplo (POST):**
```http
POST https://api-inscripciones-pagos.ort.edu.uy/api/pagos
Authorization: Bearer eyJhbGc...  (token del usuario)
X-Service-Token: eyJhbGc...       (token del servicio)
X-Source-Service: api-admisiones
X-Correlation-Id: a1b2c3d4-...
Content-Type: application/json

{
  "codigoInscripcion": 999,
  "monto": 5000.00,
  "metodoPago": "TARJETA"
}
```

---

## 🔧 Métodos Implementados en el Cliente

| Método | Verbo HTTP | Endpoint | Descripción |
|--------|-----------|----------|-------------|
| `ObtenerInscripcionesAsync(codigoPersona)` | GET | `/api/inscripciones` | Lista inscripciones de una persona |
| `CrearInscripcionAsync(request)` | POST | `/api/inscripciones` | Crea nueva inscripción |
| `ObtenerPagosAsync(codigoPersona)` | GET | `/api/pagos` | Lista pagos de una persona |
| `RegistrarPagoAsync(request)` | POST | `/api/pagos` | Registra un nuevo pago |

**Todos usan el mismo handler → Todos inyectan los mismos tokens.**

---

## 💡 Ejemplo de Uso en un Servicio

```csharp
public class AdmisionService
{
    private readonly InscripcionesApiClient _client;

    // Flujo completo: GET → POST → POST
    public async Task<...> ProcesarAdmisionCompleta(long codigoPersona, long codigoCarrera)
    {
        // 1. GET - Obtener inscripciones existentes
        var inscripcionesExistentes = await _client.ObtenerInscripcionesAsync(codigoPersona);
        if (!inscripcionesExistentes.Success)
            return Error("No se pudieron obtener inscripciones");

        // Validar que no esté inscrito
        if (inscripcionesExistentes.Data.Inscripciones.Any(i => i.CodigoCarrera == codigoCarrera))
            return Error("Ya está inscrito en esta carrera");

        // 2. POST - Crear nueva inscripción
        var nuevaInscripcion = await _client.CrearInscripcionAsync(new InscripcionRequest
        {
            CodigoPersona = codigoPersona,
            CodigoCarrera = codigoCarrera,
            Anio = DateTime.Now.Year
        });

        if (!nuevaInscripcion.Success)
            return Error("No se pudo crear la inscripción");

        // 3. POST - Registrar pago
        var pago = await _client.RegistrarPagoAsync(new PagoRequest
        {
            CodigoInscripcion = nuevaInscripcion.Data.CodigoInscripcion,
            Monto = 5000.00m,
            MetodoPago = "TARJETA"
        });

        if (!pago.Success)
            return Error("Inscripción creada pero falló el pago");

        return Ok("Admisión procesada exitosamente");
    }
}
```

**Todos los requests (GET, POST) envían:**
- ✅ Authorization (usuario)
- ✅ X-Service-Token (servicio)

---

## 🔒 Códigos de Error Diferenciados

Cada método tiene su propio código de error, facilitando el diagnóstico:

### **Inscripciones:**
| Código | Método | Descripción |
|--------|--------|-------------|
| `INSCRIPCIONES_GET_01` | ObtenerInscripcionesAsync | Error al consultar |
| `INSCRIPCIONES_POST_01` | CrearInscripcionAsync | Error al crear (ej: duplicado) |

### **Pagos:**
| Código | Método | Descripción |
|--------|--------|-------------|
| `PAGOS_GET_01` | ObtenerPagosAsync | Error al consultar |
| `PAGOS_POST_01` | RegistrarPagoAsync | Error al crear (ej: monto inválido) |

### **Errores Comunes (todos los métodos):**
| Código | Descripción |
|--------|-------------|
| `API_TIMEOUT` | Timeout >30s |
| `API_NETWORK` | Error de conexión (DNS, SSL, etc.) |
| `API_UNEXPECTED` | Error inesperado |

---

## 🎯 Ventajas de Esta Arquitectura

### **1. DRY (Don't Repeat Yourself)**
```csharp
// ❌ MAL: Repetir lógica de tokens en cada método
public async Task GetA()
{
    var token = GenerateToken(); // Duplicado
    request.Headers.Add("X-Service-Token", token);
    await _httpClient.GetAsync(...);
}

public async Task PostB()
{
    var token = GenerateToken(); // Duplicado
    request.Headers.Add("X-Service-Token", token);
    await _httpClient.PostAsync(...);
}

// ✅ BIEN: DelegatingHandler inyecta automáticamente
public async Task GetA() => await _httpClient.GetAsync(...);
public async Task PostB() => await _httpClient.PostAsync(...);
```

### **2. Consistencia**
Todos los métodos usan exactamente la misma lógica de autenticación.

### **3. Mantenibilidad**
Si cambias la lógica de tokens, solo editas `ServiceAuthenticationHandler`.

### **4. Testabilidad**
Puedes mockear `HttpClient` sin preocuparte por tokens.

---

## 📈 Agregar Nuevos Endpoints

**¿Necesitas más métodos? Solo agrégalos al cliente:**

```csharp
public class InscripcionesApiClient
{
    // ... métodos existentes ...

    // ✅ Nuevo método PUT - Tokens inyectados automáticamente
    public async Task<OperationResult<InscripcionResponse>> ActualizarInscripcionAsync(
        long codigoInscripcion,
        InscripcionRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/inscripciones/{codigoInscripcion}",
                request
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<InscripcionResponse>();
                return OperationResult<InscripcionResponse>.Ok(result!, nameof(ActualizarInscripcionAsync));
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return OperationResult<InscripcionResponse>.IsFailed(
                "INSCRIPCIONES_PUT_01",
                nameof(ActualizarInscripcionAsync),
                $"Error al actualizar: {response.StatusCode} - {errorContent}",
                (int)response.StatusCode,
                default!
            );
        }
        catch (Exception ex)
        {
            return HandleException<InscripcionResponse>(ex, nameof(ActualizarInscripcionAsync));
        }
    }

    // ✅ Nuevo método DELETE - Tokens inyectados automáticamente
    public async Task<OperationResult<bool>> CancelarInscripcionAsync(long codigoInscripcion)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/inscripciones/{codigoInscripcion}");

            if (response.IsSuccessStatusCode)
            {
                return OperationResult<bool>.Ok(true, nameof(CancelarInscripcionAsync));
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return OperationResult<bool>.IsFailed(
                "INSCRIPCIONES_DELETE_01",
                nameof(CancelarInscripcionAsync),
                $"Error al cancelar: {response.StatusCode} - {errorContent}",
                (int)response.StatusCode,
                default!
            );
        }
        catch (Exception ex)
        {
            return HandleException<bool>(ex, nameof(CancelarInscripcionAsync));
        }
    }
}
```

**No necesitas:**
- ❌ Modificar el handler
- ❌ Registrar nuevo HttpClient
- ❌ Agregar más configuración

**Solo agregas el método y listo.** 🚀

---

## 🌐 Múltiples APIs Destino

**¿Y si necesitas llamar a otra API diferente (ej: api-academico)?**

```csharp
// HttpClientExtensions.cs

// API 1: Inscripciones y Pagos
services.AddHttpClient<InscripcionesApiClient>(...)
    .AddHttpMessageHandler(sp => new ServiceAuthenticationHandler(
        sp.GetRequiredService<IServiceTokenService>(),
        sp.GetRequiredService<IHttpContextAccessor>(),
        "api-inscripciones-pagos" // Target API
    ));

// API 2: Académico (nuevo)
services.AddHttpClient<AcademicoApiClient>(client =>
{
    client.BaseAddress = new Uri(configuration["ApiClients:Academico:BaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler(sp => new ServiceAuthenticationHandler(
    sp.GetRequiredService<IServiceTokenService>(),
    sp.GetRequiredService<IHttpContextAccessor>(),
    "api-academico" // Target API diferente
));
```

**Cada cliente tiene su propio handler con su target API.**

---

## ✅ Resumen

| Pregunta | Respuesta |
|----------|-----------|
| **¿Funciona para GET?** | ✅ Sí |
| **¿Funciona para POST?** | ✅ Sí |
| **¿Funciona para PUT/DELETE?** | ✅ Sí |
| **¿Puedo tener múltiples métodos?** | ✅ Sí, ilimitados |
| **¿Tengo que configurar cada método?** | ❌ No, el handler es global |
| **¿Puedo llamar a múltiples APIs?** | ✅ Sí, un HttpClient por API |
| **¿Se inyectan los tokens automáticamente?** | ✅ Sí, en todos los métodos |

---

## 🎯 Conclusión

La arquitectura es **completamente escalable** porque:

1. **El handler trabaja a nivel de HttpClient** (no de método individual)
2. **Cada request que salga de ese HttpClient** recibe los tokens automáticamente
3. **No importa el verbo HTTP** (GET, POST, PUT, DELETE, PATCH, etc.)
4. **No importa cuántos métodos agregues** al cliente

**Patrón:** 1 API Destino = 1 HttpClient = 1 ServiceAuthenticationHandler = ∞ Métodos con tokens
