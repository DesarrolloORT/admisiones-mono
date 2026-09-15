# 🔐 Rate Limiting - Protección contra Fuerza Bruta

## Descripción General

Sistema de **rate limiting distribuido** implementado con **Redis** para proteger el endpoint de Login contra ataques de fuerza bruta, siguiendo las recomendaciones de **OWASP Anti-Automation**.

---

## 🎯 Estrategia de Protección DUAL ADAPTATIVA

El sistema implementa **dos capas de protección independientes con límites diferentes**:

### 1️⃣ Rate Limit por IP SOLAMENTE
**Previene:** Credential stuffing masivo (probar muchas cuentas desde una misma IP)

- **Límite:** **10 intentos** por 15 minutos por dirección IP
- **Razón:** Permite múltiples usuarios legítimos en la misma red (hogares, oficinas, IPs públicas compartidas)
- **Implementación:** Middleware de ASP.NET Core (se ejecuta antes de deserializar el body)
- **Clave Redis:** `ratelimit:login-ip:<ip_address>`
- **Configuración:** `ServiceCollectionExtensions.AddLoginRateLimiting()`

**Ejemplo de escenario permitido:**
```
IP 192.168.1.100 (hogar con 4 personas):
  - usuario1@ort.edu.uy intenta 2 veces (total: 2)
  - usuario2@ort.edu.uy intenta 3 veces (total: 5)
  - usuario3@ort.edu.uy intenta 1 vez  (total: 6)
  - usuario4@ort.edu.uy intenta 2 veces (total: 8)
→ ✅ TODOS PERMITIDOS (8 < 10)
```

**Ejemplo de ataque bloqueado:**
```
IP 10.0.0.1 intenta (credential stuffing):
  - usuario1@ort.edu.uy + pass1
  - usuario2@ort.edu.uy + pass2
  ... (10 cuentas diferentes)
  - usuario11@ort.edu.uy + pass11
→ ❌ BLOQUEADO en intento 11 (excede 10)
```

---

### 2️⃣ Rate Limit por IP + Documento
**Previene:** Ataque focalizado a una cuenta específica

- **Límite:** **5 intentos** por 15 minutos por combinación única `IP + TipoDocumento + Documento`
- **Razón:** Protección estricta contra intentos repetidos a una cuenta desde la misma IP
- **Implementación:** Validación manual en `AuthController.Login()`
- **Clave Redis:** `ratelimit:login-account:<tipoDoc>:<documento_normalizado>:ip:<ip>`
- **Normalización:** Ignora puntos, guiones y espacios en el documento (ej: `1.234.567-8` → `12345678`)

**Ejemplo de escenario bloqueado:**
```
IP 192.168.1.100 ataca la cuenta CI:12345678:
  - Intento 1: password123  ❌
  - Intento 2: admin123     ❌
  - Intento 3: 12345678     ❌
  - Intento 4: qwerty       ❌
  - Intento 5: letmein      ❌
  - Intento 6: password     ❌ BLOQUEADO (excede 5)
→ HTTP 429 con código AUTH_RL_02
```

---

## 🎯 Tabla Comparativa de Límites

| Validación | Límite | Ventana | Alcance | Ejemplo de Uso |
|------------|--------|---------|---------|----------------|
| **Solo IP** | 10 intentos | 15 min | Todos los usuarios desde esa IP | Hogar: 5 personas × 2 intentos c/u = 10 ✅ |
| **IP + Documento** | 5 intentos | 15 min | Una cuenta específica desde esa IP | Usuario olvida contraseña → 5 intentos ❌ |

---

## 🧪 Ejemplos Prácticos

### Caso 1: Usuario Legítimo Olvidó Password
```bash
IP: 192.168.1.50
Usuario: CI:87654321

Intento 1: password → 401 (1/5 usado para cuenta, 1/10 para IP)
Intento 2: Password1 → 401 (2/5, 2/10)
Intento 3: admin → 401 (3/5, 3/10)
Intento 4: 87654321 → 401 (4/5, 4/10)
Intento 5: qwerty → 401 (5/5, 5/10)
Intento 6: 123456 → 429 AUTH_RL_02 ❌ (excede límite de cuenta)
```
**Mensaje:** "Se superó el límite de intentos para esta cuenta (5 intentos cada 15 minutos)."

---

### Caso 2: Familia/Oficina con IP Compartida (Escenario Real)
```bash
IP: 200.45.123.10 (IP pública de Antel)

Usuario 1 (CI:11111111): 2 intentos (total IP: 2/10) ✅
Usuario 2 (CI:22222222): 3 intentos (total IP: 5/10) ✅
Usuario 3 (CI:33333333): 1 intento  (total IP: 6/10) ✅
Usuario 4 (CI:44444444): 2 intentos (total IP: 8/10) ✅
Usuario 5 (CI:55555555): 2 intentos (total IP: 10/10) ✅
Usuario 6 (CI:66666666): 1 intento  → 429 AUTH_RL_01 ❌ (excede límite de IP)
```
**Mensaje:** "Se superó el límite de intentos desde esta red (10 intentos cada 15 minutos)."

---

### Caso 3: Ataque de Fuerza Bruta a Cuenta Específica
```bash
Atacante intenta CI:12345678 desde MÚLTIPLES IPs:

IP 10.0.0.1: 5 intentos → ❌ Bloqueado (AUTH_RL_02)
IP 10.0.0.2: 5 intentos → ❌ Bloqueado (AUTH_RL_02)
IP 10.0.0.3: 5 intentos → ❌ Bloqueado (AUTH_RL_02)

Cada IP se bloquea independientemente al 5to intento con esa cuenta.
```

---

### Caso 4: Credential Stuffing (Lista de Credenciales Robadas)
```bash
IP: 203.0.113.5 (atacante)

usuario1@ort.edu.uy + leaked_pass_1
usuario2@ort.edu.uy + leaked_pass_2
...
usuario10@ort.edu.uy + leaked_pass_10  (total IP: 10/10) ✅
usuario11@ort.edu.uy + leaked_pass_11  → 429 AUTH_RL_01 ❌
```
**Bloqueado** al 11vo intento por exceder límite de IP.

---

## 🔧 Arquitectura Técnica

### Componentes

```
┌─────────────────────────────────────────────────────────────┐
│ HTTP Request → /auth/login                                   │
└─────────────────────────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────┐
│ ✅ VALIDACIÓN 1: Middleware Rate Limiting por IP            │
│    - Lee IP del request                                      │
│    - Consulta Redis: ratelimit:login-ip:<ip>                │
│    - Si excede límite → HTTP 429 (no llega al controller)   │
└─────────────────────────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────┐
│ AuthController.Login() - Deserializa body                    │
└─────────────────────────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────┐
│ ✅ VALIDACIÓN 2: Rate Limiting por IP + Documento           │
│    - Lee TipoDocumento + Documento del body                 │
│    - Consulta Redis: login-account:<tipo>:<doc>:ip:<ip>     │
│    - Si excede límite → HTTP 429 (devuelve antes de LDAP)   │
└─────────────────────────────────────────────────────────────┘
						 │
						 ▼
┌─────────────────────────────────────────────────────────────┐
│ Autenticación LDAP                                           │
└─────────────────────────────────────────────────────────────┘
```

### Algoritmo: Sliding Window

Implementado con **Redis Sorted Sets**:

```lua
-- Estructura de datos en Redis
ZADD ratelimit:login-ip:192.168.1.1  <timestamp_1>  <timestamp_1>
ZADD ratelimit:login-ip:192.168.1.1  <timestamp_2>  <timestamp_2>
...

-- Proceso de validación (atómico)
1. ZREMRANGEBYSCORE key -inf (now - window)  → Limpiar requests antiguos
2. ZADD key <now> <now>                      → Agregar request actual
3. ZCOUNT key -inf +inf                      → Contar requests en ventana
4. IF count > limit THEN reject ELSE allow
5. EXPIRE key (window + 1min)                → Auto-limpieza
```

**Ventaja vs Fixed Window:** No hay "burst" al inicio de cada ventana. Cada request expira exactamente después de la ventana configurada.

---

## 📊 Monitoreo

### Métricas Prometheus

```csharp
// Contador de rechazos por IP (límite de 10 intentos)
login_rate_limit_rejections_total

// Contador de rechazos por cuenta (límite de 5 intentos IP+Doc)
login_account_rate_limit_rejections_total
```

**Consultas útiles:**
```promql
# Rate de rechazos por IP por minuto
rate(login_rate_limit_rejections_total[5m])

# Rate de rechazos por cuenta por minuto
rate(login_account_rate_limit_rejections_total[5m])

# Total de rechazos en las últimas 24h (ambos tipos)
increase(login_rate_limit_rejections_total[24h]) + increase(login_account_rate_limit_rejections_total[24h])

# Proporción de rechazos por tipo
login_rate_limit_rejections_total / (login_rate_limit_rejections_total + login_account_rate_limit_rejections_total)
```

### Logs

```csharp
// Cuando se excede el límite de IP (10 intentos)
[Warning] Rate limit EXCEEDED for login-ip:192.168.1.1. Current: 11/10 in window 00:15:00

// Cuando se excede el límite de cuenta (5 intentos IP+Doc)
[Warning] Rate limit EXCEEDED for account CI:12345678 from IP 192.168.1.1. 
		  Attempts: 0/5. Partition: login-account:CI:12345678:ip:192.168.1.1

// Validación exitosa (solo en Debug)
[Debug] Rate limit check OK for login-ip:192.168.1.1. Current: 8/10
```

---

## 🔍 Inspección en Redis

### Ver claves activas

```bash
# Conectar a Redis
redis-cli

# Listar todas las claves de rate limiting
KEYS ratelimit:*

# Ver claves específicas
KEYS ratelimit:login-ip:*
KEYS ratelimit:login-account:*
```

### Inspeccionar una clave

```bash
# Ver todos los timestamps en una ventana
ZRANGE ratelimit:login-ip:192.168.1.1 0 -1 WITHSCORES

# Ejemplo de output:
1) "1735689123456"  # timestamp en milisegundos
2) "1735689123456"  # score (mismo valor)
3) "1735689145789"
4) "1735689145789"

# Contar requests activos
ZCARD ratelimit:login-ip:192.168.1.1

# Ver TTL de la clave
TTL ratelimit:login-ip:192.168.1.1
```

### Resetear manualmente un bloqueo

```bash
# Opción 1: Eliminar la clave completamente
DEL ratelimit:login-ip:192.168.1.1

# Opción 2: Usar el servicio (desde código)
await redisRateLimiter.ClearAsync("login-ip:192.168.1.1");
```

---

## ⚙️ Configuración

### appsettings.json

```json
{
  "ConnectionStrings": {
	"Redis": "localhost:6379,abortConnect=false"
  },
  "Authentication": {
	"Login": {
	  "RateLimitIpAttempts": 10,          // Default: 10 (solo IP, permite múltiples usuarios)
	  "RateLimitAccountAttempts": 5,      // Default: 5 (IP+Documento, OWASP recomendado)
	  "RateLimitWindowMinutes": 15        // Default: 15 (OWASP recomendado)
	}
  }
}
```

### Valores Recomendados por Escenario

| Escenario | IP Attempts | Account Attempts | Window | Justificación |
|-----------|-------------|------------------|--------|---------------|
| **Producción (Default)** | 10 | 5 | 15 min | Balance entre seguridad y UX para IPs compartidas |
| **Alta Seguridad** | 5 | 3 | 20 min | Máxima protección, menos permisivo |
| **Testing/Dev** | 20 | 10 | 5 min | Muy permisivo para pruebas locales |
| **Corporativo (NAT)** | 15 | 5 | 15 min | Muchos usuarios detrás de un proxy/NAT |

**Recomendación OWASP:** Mantener `AccountAttempts` en 5 o menos para prevenir ataques de fuerza bruta efectivamente.

---

## 🧪 Testing

### Test 1: Rate Limit por Cuenta (IP+Documento)

```bash
# Probar la misma cuenta desde la misma IP
for i in {1..7}; do
  echo "=== Intento $i ==="
  curl -v -X POST http://localhost:5000/auth/login \
	-H "Content-Type: application/json" \
	-d '{"tipoDocumento":"CI","documento":"12345678","password":"wrongpass"}'
  echo ""
done

# Esperado:
# Intentos 1-5: HTTP 401 (credenciales inválidas)
# Intento 6+: HTTP 429 (AUTH_RL_02, límite de cuenta excedido)
# Headers en 429:
# X-RateLimit-Limit: 5
# X-RateLimit-Remaining: 0
# X-RateLimit-Reset: <timestamp>
# Retry-After: <segundos>
```

---

### Test 2: Rate Limit por IP Solamente (Múltiples Usuarios)

```bash
# Simular 11 usuarios diferentes desde la misma IP
for i in {1..11}; do
  echo "=== Usuario $i ==="
  curl -v -X POST http://localhost:5000/auth/login \
	-H "Content-Type: application/json" \
	-d "{\"tipoDocumento\":\"CI\",\"documento\":\"user$i\",\"password\":\"wrong\"}"
  echo ""
done

# Esperado:
# Intentos 1-10: HTTP 401 (credenciales inválidas, cada uno con documento diferente)
# Intento 11: HTTP 429 (AUTH_RL_01, límite de IP excedido)
# Mensaje: "Se superó el límite de intentos desde esta red (10 intentos cada 15 minutos)"
```

---

### Test 3: Escenario Realista (Familia con IP Compartida)

```bash
# Usuario 1: 2 intentos
curl -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" \
  -d '{"tipoDocumento":"CI","documento":"11111111","password":"wrong"}' # Intento 1 (Total IP: 1)
curl -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" \
  -d '{"tipoDocumento":"CI","documento":"11111111","password":"wrong2"}' # Intento 2 (Total IP: 2)

# Usuario 2: 3 intentos
curl -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" \
  -d '{"tipoDocumento":"CI","documento":"22222222","password":"wrong"}' # Total IP: 3
curl -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" \
  -d '{"tipoDocumento":"CI","documento":"22222222","password":"wrong2"}' # Total IP: 4
curl -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" \
  -d '{"tipoDocumento":"CI","documento":"22222222","password":"wrong3"}' # Total IP: 5

# Usuario 3: 2 intentos
curl -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" \
  -d '{"tipoDocumento":"CI","documento":"33333333","password":"wrong"}' # Total IP: 6
curl -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" \
  -d '{"tipoDocumento":"CI","documento":"33333333","password":"wrong2"}' # Total IP: 7

# Usuario 4: 3 intentos
curl -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" \
  -d '{"tipoDocumento":"CI","documento":"44444444","password":"wrong"}' # Total IP: 8
curl -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" \
  -d '{"tipoDocumento":"CI","documento":"44444444","password":"wrong2"}' # Total IP: 9
curl -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" \
  -d '{"tipoDocumento":"CI","documento":"44444444","password":"wrong3"}' # Total IP: 10

# Usuario 5: 1 intento (excede límite de IP)
curl -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" \
  -d '{"tipoDocumento":"CI","documento":"55555555","password":"wrong"}' # Total IP: 11 → 429 AUTH_RL_01

# Esperado:
# - Ningún usuario individual excede 5 intentos (no se dispara AUTH_RL_02)
# - El usuario 5 recibe HTTP 429 AUTH_RL_01 (excede 10 intentos de IP)
```

---

## 🚨 Fail-Safe Behavior

### Si Redis no está disponible

```csharp
// Configurado para FAIL-OPEN (permitir requests)
// Ver: RedisRateLimiterService.IsAllowedAsync()

try {
	// Validar con Redis
} catch (RedisConnectionException) {
	_logger.LogError("Redis unavailable. Failing open (allowing request).");
	return true; // ⚠️ Permitir para no bloquear la app
}
```

**Alternativa FAIL-CLOSED** (más segura pero afecta disponibilidad):
```csharp
return false; // Rechazar si Redis no responde
```

---

## 📝 Códigos de Error

| Código | Mensaje | Causa |
|--------|---------|-------|
| `AUTH_RL_01` | Límite de IP excedido | Middleware rechazó por demasiados intentos desde la IP |
| `AUTH_RL_02` | Límite de cuenta excedido | Controlador rechazó por demasiados intentos a la cuenta desde esa IP |

---

## 🔗 Referencias

- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html#login-throttling)
- [OWASP Anti-Automation](https://owasp.org/www-community/controls/Blocking_Brute_Force_Attacks)
- [RFC 6585 - HTTP Status 429](https://datatracker.ietf.org/doc/html/rfc6585#section-4)
- [Rate Limit Headers (IETF Draft)](https://datatracker.ietf.org/doc/html/draft-ietf-httpapi-ratelimit-headers)

---

## 📂 Archivos Relacionados

- `WebApiAdmisiones/Security/RedisRateLimiterService.cs` - Lógica core de rate limiting
- `WebApiAdmisiones/Security/RedisRateLimiter.cs` - Adaptador para ASP.NET Core middleware
- `WebApiAdmisiones/Extensions/ServiceCollectionExtensions.cs` - Configuración del middleware
- `WebApiAdmisiones/Controllers/AuthController.cs` - Validación en el endpoint Login
