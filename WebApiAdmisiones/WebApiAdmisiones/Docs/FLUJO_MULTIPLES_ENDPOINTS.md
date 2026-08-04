# 🔄 Flujo Completo con Múltiples Endpoints

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     API ADMISIONES (esta solución)                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │ AdmisionService                                                │    │
│  │  - ObtenerInscripcionesPersonaAsync()       → GET              │    │
│  │  - CrearAdmisionConInscripcionAsync()       → POST             │    │
│  │  - ObtenerPagosPersonaAsync()               → GET              │    │
│  │  - RegistrarPagoInscripcionAsync()          → POST             │    │
│  │  - CrearInscripcionYPagoAsync()             → POST + POST      │    │
│  └─────────────────────┬──────────────────────────────────────────┘    │
│                        │ Usa                                            │
│  ┌─────────────────────▼──────────────────────────────────────────┐    │
│  │ InscripcionesApiClient (UN SOLO HttpClient)                    │    │
│  │                                                                 │    │
│  │  🔹 ObtenerInscripcionesAsync(codigoPersona)                   │    │
│  │     → await _httpClient.GetAsync("/api/inscripciones?...")     │    │
│  │                                                                 │    │
│  │  🔹 CrearInscripcionAsync(request)                             │    │
│  │     → await _httpClient.PostAsJsonAsync("/api/inscripciones")  │    │
│  │                                                                 │    │
│  │  🔹 ObtenerPagosAsync(codigoPersona)                           │    │
│  │     → await _httpClient.GetAsync("/api/pagos?...")             │    │
│  │                                                                 │    │
│  │  🔹 RegistrarPagoAsync(request)                                │    │
│  │     → await _httpClient.PostAsJsonAsync("/api/pagos")          │    │
│  │                                                                 │    │
│  └─────────────────────┬──────────────────────────────────────────┘    │
│                        │ Todos pasan por                                │
│  ┌─────────────────────▼──────────────────────────────────────────┐    │
│  │ ServiceAuthenticationHandler (DelegatingHandler)               │    │
│  │                                                                 │    │
│  │  INTERCEPTA TODOS LOS REQUESTS Y AGREGA:                       │    │
│  │  ✅ Authorization: Bearer {userToken}                          │    │
│  │  ✅ X-Service-Token: {serviceToken}                            │    │
│  │  ✅ X-Source-Service: api-admisiones                           │    │
│  │  ✅ X-Correlation-Id: {guid}                                   │    │
│  │                                                                 │    │
│  └─────────────────────┬──────────────────────────────────────────┘    │
│                        │                                                │
└────────────────────────┼────────────────────────────────────────────────┘
                         │
                         │ HTTP Requests con tokens
                         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│          API INSCRIPCIONES Y PAGOS (otra solución)                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  🔹 GET  /api/inscripciones?codigoPersona=123                          │
│  🔹 POST /api/inscripciones                                            │
│  🔹 GET  /api/pagos?codigoPersona=123                                  │
│  🔹 POST /api/pagos                                                    │
│                                                                         │
│  TODOS RECIBEN:                                                        │
│  ✅ Authorization: Bearer eyJhbGc... (valida usuario)                  │
│  ✅ X-Service-Token: eyJhbGc...      (valida servicio)                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 📊 Tabla de Métodos y Headers

| Método en AdmisionService | Endpoint API 2 | Verbo | Headers Inyectados |
|---------------------------|---------------|-------|-------------------|
| `ObtenerInscripcionesPersonaAsync()` | `/api/inscripciones` | GET | ✅ Authorization + X-Service-Token |
| `CrearAdmisionConInscripcionAsync()` | `/api/inscripciones` | POST | ✅ Authorization + X-Service-Token |
| `ObtenerPagosPersonaAsync()` | `/api/pagos` | GET | ✅ Authorization + X-Service-Token |
| `RegistrarPagoInscripcionAsync()` | `/api/pagos` | POST | ✅ Authorization + X-Service-Token |

**Conclusión:** El handler funciona como un **interceptor global** que afecta a TODAS las llamadas del HttpClient.

---

## 🔑 Códigos de Error por Operación

### **GET - Obtener Inscripciones**
```csharp
var result = await _client.ObtenerInscripcionesAsync(123456);

// Posibles resultados:
✅ Success: result.Data.Inscripciones (lista)
❌ INSCRIPCIONES_GET_01: Error de negocio (404, 403, etc.)
❌ API_TIMEOUT: >30 segundos
❌ API_NETWORK: Sin conexión
```

### **POST - Crear Inscripción**
```csharp
var result = await _client.CrearInscripcionAsync(request);

// Posibles resultados:
✅ Success: result.Data.CodigoInscripcion
❌ INSCRIPCIONES_POST_01: Error de negocio (409 duplicado, 400 datos inválidos)
❌ API_TIMEOUT: >30 segundos
❌ API_NETWORK: Sin conexión
```

### **GET - Obtener Pagos**
```csharp
var result = await _client.ObtenerPagosAsync(123456);

// Posibles resultados:
✅ Success: result.Data.Pagos (lista)
❌ PAGOS_GET_01: Error de negocio
❌ API_TIMEOUT: >30 segundos
❌ API_NETWORK: Sin conexión
```

### **POST - Registrar Pago**
```csharp
var result = await _client.RegistrarPagoAsync(request);

// Posibles resultados:
✅ Success: result.Data.CodigoPago
❌ PAGOS_POST_01: Error de negocio (400 monto inválido, 404 inscripción no existe)
❌ API_TIMEOUT: >30 segundos
❌ API_NETWORK: Sin conexión
```

---

## 💡 Ejemplo Real: Flujo Completo

```csharp
public async Task<OperationResult<string>> ProcesarAdmision(
    long codigoPersona,
    long codigoCarrera,
    decimal montoPago)
{
    // 1️⃣ GET - Verificar inscripciones existentes
    var inscripcionesExistentes = await _client.ObtenerInscripcionesAsync(codigoPersona);
    // Headers: Authorization + X-Service-Token ✅

    if (!inscripcionesExistentes.Success)
        return Error($"No se pudieron consultar inscripciones: {inscripcionesExistentes.ErrorCode}");

    if (inscripcionesExistentes.Data.Inscripciones.Any(i => i.CodigoCarrera == codigoCarrera))
        return Error("Ya está inscrito en esta carrera");

    // 2️⃣ POST - Crear nueva inscripción
    var nuevaInscripcion = await _client.CrearInscripcionAsync(new InscripcionRequest
    {
        CodigoPersona = codigoPersona,
        CodigoCarrera = codigoCarrera,
        Anio = DateTime.Now.Year
    });
    // Headers: Authorization + X-Service-Token ✅

    if (!nuevaInscripcion.Success)
        return Error($"No se pudo crear inscripción: {nuevaInscripcion.ErrorCode}");

    // 3️⃣ POST - Registrar pago
    var pago = await _client.RegistrarPagoAsync(new PagoRequest
    {
        CodigoInscripcion = nuevaInscripcion.Data.CodigoInscripcion,
        Monto = montoPago,
        MetodoPago = "TARJETA"
    });
    // Headers: Authorization + X-Service-Token ✅

    if (!pago.Success)
        return Error($"Inscripción OK, pero pago falló: {pago.ErrorCode}");

    // 4️⃣ GET - Verificar estado final
    var pagosFinales = await _client.ObtenerPagosAsync(codigoPersona);
    // Headers: Authorization + X-Service-Token ✅

    return Ok($"Admisión completa: Inscripción {nuevaInscripcion.Data.CodigoInscripcion}, Pago {pago.Data.CodigoPago}");
}
```

**4 llamadas diferentes → Todas con los mismos headers → Sin código duplicado**

---

## ✨ Ventaja Clave

```csharp
// ❌ SIN DelegatingHandler: Repetir en cada método
public async Task GetA()
{
    var token = GenerateServiceToken();
    var userToken = GetUserToken();
    _httpClient.DefaultRequestHeaders.Add("X-Service-Token", token);
    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
    await _httpClient.GetAsync("/api/a");
}

public async Task PostB()
{
    var token = GenerateServiceToken(); // Duplicado
    var userToken = GetUserToken();     // Duplicado
    _httpClient.DefaultRequestHeaders.Add("X-Service-Token", token);
    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
    await _httpClient.PostAsync("/api/b", content);
}

// ✅ CON DelegatingHandler: Automático en todos
public async Task GetA() => await _httpClient.GetAsync("/api/a");
public async Task PostB() => await _httpClient.PostAsync("/api/b", content);
```

**Resultado:** Menos código, más consistencia, más mantenible.
