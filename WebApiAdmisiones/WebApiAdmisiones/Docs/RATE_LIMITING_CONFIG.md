# 📊 Configuración de Rate Limiting - Resumen Visual

## ⚙️ Configuración Actual

```json
{
  "Authentication": {
	"Login": {
	  "RateLimitIpAttempts": 10,        // Solo IP → Permite IPs compartidas
	  "RateLimitAccountAttempts": 5,    // IP + Documento → Protección focal
	  "RateLimitWindowMinutes": 15      // Ventana deslizante de 15 minutos
	}
  }
}
```

---

## 🎯 Matriz de Decisión

```
┌─────────────────────────────────────────────────────────────────────┐
│                     VALIDACIÓN DE RATE LIMITING                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Request HTTP POST /auth/login                                      │
│                ↓                                                    │
│  ┌──────────────────────────────────────────────┐                  │
│  │ ✅ VALIDACIÓN 1: Middleware Rate Limit IP    │                  │
│  │    Límite: 10 intentos / 15 min              │                  │
│  │    Alcance: TODOS los usuarios desde esa IP  │                  │
│  └──────────────────────────────────────────────┘                  │
│                ↓ (si pasa)                                          │
│  ┌──────────────────────────────────────────────┐                  │
│  │ Deserializar body (TipoDocumento + Documento)│                  │
│  └──────────────────────────────────────────────┘                  │
│                ↓                                                    │
│  ┌──────────────────────────────────────────────┐                  │
│  │ ✅ VALIDACIÓN 2: Rate Limit IP+Documento     │                  │
│  │    Límite: 5 intentos / 15 min               │                  │
│  │    Alcance: UNA cuenta desde esa IP          │                  │
│  └──────────────────────────────────────────────┘                  │
│                ↓ (si pasa)                                          │
│  ┌──────────────────────────────────────────────┐                  │
│  │ Autenticación LDAP                            │                  │
│  └──────────────────────────────────────────────┘                  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 📋 Tabla de Comportamiento por Escenario

| Escenario | Usuarios | Intentos por Usuario | Total Intentos IP | Resultado | Código Error |
|-----------|----------|---------------------|-------------------|-----------|--------------|
| **1 usuario olvida contraseña** | 1 | 6 | 6 | ❌ Bloqueado al 6to | `AUTH_RL_02` |
| **Familia (4 personas × 2 intentos)** | 4 | 2 c/u | 8 | ✅ Permitido | - |
| **Oficina (10 personas × 1 intento)** | 10 | 1 c/u | 10 | ✅ Permitido | - |
| **Oficina (11 personas × 1 intento)** | 11 | 1 c/u | 11 | ❌ Bloqueado al 11vo | `AUTH_RL_01` |
| **Credential stuffing (11 cuentas)** | 11 | 1 c/u | 11 | ❌ Bloqueado al 11vo | `AUTH_RL_01` |
| **Ataque focal (1 cuenta, 6 IPs)** | 1 | 6 (desde 6 IPs) | 6 | ❌ Cada IP bloqueada al 6to | `AUTH_RL_02` |

---

## 🔐 Códigos de Error

| Código | Tipo de Bloqueo | Mensaje al Usuario |
|--------|-----------------|-------------------|
| **AUTH_RL_01** | IP excedida (>10 intentos) | "Se superó el límite de intentos de inicio de sesión desde esta red (10 intentos cada 15 minutos). Por tu seguridad, intentá nuevamente más tarde." |
| **AUTH_RL_02** | Cuenta excedida (>5 intentos IP+Doc) | "Se superó el límite de intentos de inicio de sesión para esta cuenta (5 intentos cada 15 minutos). Por tu seguridad, intentá nuevamente más tarde." |

---

## 🔍 Claves Redis Generadas

```bash
# Límite por IP (10 intentos)
ratelimit:login-ip:192.168.1.100

# Límite por cuenta (5 intentos)
ratelimit:login-account:CI:12345678:ip:192.168.1.100
ratelimit:login-account:Pasaporte:AB123456:ip:10.0.0.5
```

**Estructura de Sorted Set en Redis:**
```
Key: ratelimit:login-ip:192.168.1.100
Type: ZSET (Sorted Set)
Score: timestamp en milisegundos (ej: 1735689123456)
Value: timestamp en milisegundos (mismo que score)
TTL: 16 minutos (window + 1min de buffer)

Ejemplo:
ZRANGE ratelimit:login-ip:192.168.1.100 0 -1 WITHSCORES
1) "1735689123456"
2) "1735689123456"
3) "1735689145789"
4) "1735689145789"
... (hasta 10 elementos)
```

---

## 📈 Métricas Prometheus

```yaml
# Rechazo por límite de IP
- metric: login_rate_limit_rejections_total
  type: Counter
  labels: []
  description: "Intentos bloqueados por exceder 10 requests desde la misma IP"

# Rechazo por límite de cuenta
- metric: login_account_rate_limit_rejections_total
  type: Counter
  labels: []
  description: "Intentos bloqueados por exceder 5 requests a la misma cuenta desde la misma IP"
```

**Dashboards Grafana:**
```promql
# Panel 1: Rate de bloqueos totales
sum(rate(login_rate_limit_rejections_total[5m])) + sum(rate(login_account_rate_limit_rejections_total[5m]))

# Panel 2: Proporción de bloqueos por tipo
(login_rate_limit_rejections_total / (login_rate_limit_rejections_total + login_account_rate_limit_rejections_total)) * 100

# Panel 3: Top IPs bloqueadas (requiere labels adicionales)
# (Considerar agregar label 'ip' en futuras versiones)
```

---

## 🧪 Scripts de Testing

### PowerShell - Test Automático

```powershell
# Test 1: Límite de cuenta (5 intentos IP+Doc)
Write-Host "=== TEST 1: Rate Limit por Cuenta ===" -ForegroundColor Cyan
for ($i = 1; $i -le 7; $i++) {
	Write-Host "Intento $i..." -NoNewline
	$response = Invoke-WebRequest -Uri "http://localhost:5000/auth/login" `
		-Method POST `
		-ContentType "application/json" `
		-Body '{"tipoDocumento":"CI","documento":"12345678","password":"wrongpass"}' `
		-SkipHttpErrorCheck

	if ($response.StatusCode -eq 429) {
		Write-Host " ❌ BLOQUEADO (AUTH_RL_02)" -ForegroundColor Red
	} else {
		Write-Host " ✅ Permitido (HTTP $($response.StatusCode))" -ForegroundColor Green
	}
}

# Test 2: Límite de IP (10 intentos con diferentes usuarios)
Write-Host "`n=== TEST 2: Rate Limit por IP ===" -ForegroundColor Cyan
for ($i = 1; $i -le 12; $i++) {
	Write-Host "Usuario $i..." -NoNewline
	$body = @{
		tipoDocumento = "CI"
		documento = "user$i"
		password = "wrongpass"
	} | ConvertTo-Json

	$response = Invoke-WebRequest -Uri "http://localhost:5000/auth/login" `
		-Method POST `
		-ContentType "application/json" `
		-Body $body `
		-SkipHttpErrorCheck

	if ($response.StatusCode -eq 429) {
		Write-Host " ❌ BLOQUEADO (AUTH_RL_01)" -ForegroundColor Red
	} else {
		Write-Host " ✅ Permitido (HTTP $($response.StatusCode))" -ForegroundColor Green
	}
}
```

---

## 🎓 Casos de Uso Educativos

### Caso A: Estudiante Olvida Contraseña
```
Usuario: CI:54321098
IP: 192.168.0.15 (WiFi de casa)

Intento 1: "miclave123" → 401 ❌
Intento 2: "MiClave123" → 401 ❌
Intento 3: "54321098"   → 401 ❌
Intento 4: "estudianteort" → 401 ❌
Intento 5: "password2024" → 401 ❌
Intento 6: "admin123" → 429 AUTH_RL_02 ❌

→ Sistema lo protege de sí mismo. Debe esperar 15 minutos o usar recuperación de password.
```

---

### Caso B: Hogar con 5 Personas
```
IP compartida: 181.44.123.90 (Antel Fibra)

Persona 1 (CI:11111111): 2 intentos → Total IP: 2/10 ✅
Persona 2 (CI:22222222): 1 intento  → Total IP: 3/10 ✅
Persona 3 (CI:33333333): 3 intentos → Total IP: 6/10 ✅
Persona 4 (CI:44444444): 2 intentos → Total IP: 8/10 ✅
Persona 5 (CI:55555555): 2 intentos → Total IP: 10/10 ✅

→ Todos pueden intentar sin bloquear al resto.
```

---

### Caso C: Ataque de Fuerza Bruta
```
Atacante objetivo: CI:98765432
Botnets IPs: 203.0.113.{1..50}

IP 203.0.113.1:
  - Intento 1: "password" → 401 (cuenta: 1/5) ✅
  - Intento 2: "123456"   → 401 (cuenta: 2/5) ✅
  - Intento 3: "admin"    → 401 (cuenta: 3/5) ✅
  - Intento 4: "qwerty"   → 401 (cuenta: 4/5) ✅
  - Intento 5: "letmein"  → 401 (cuenta: 5/5) ✅
  - Intento 6: "welcome"  → 429 AUTH_RL_02 ❌

IP 203.0.113.2:
  - Intento 1: "password1" → 401 (cuenta: 1/5) ✅
  ... (ciclo se repite)

→ Cada IP solo puede probar 5 passwords por cuenta. 
   Ataque mitigado efectivamente.
```

---

## 🔧 Troubleshooting

### "No veo claves en Redis Insight"

**Verificar:**
```bash
# 1. Redis está corriendo?
redis-cli ping
# Esperado: PONG

# 2. Estás en la base de datos correcta?
redis-cli
127.0.0.1:6379> SELECT 0  # Default DB
127.0.0.1:6379> KEYS ratelimit:*

# 3. Hay claves activas?
# Hacer un request de login y buscar inmediatamente:
127.0.0.1:6379> KEYS *
127.0.0.1:6379> TTL ratelimit:login-ip:127.0.0.1
```

### "Bloqueos permanentes"

**Resetear manualmente:**
```bash
# Desde Redis CLI
DEL ratelimit:login-ip:192.168.1.100
DEL ratelimit:login-account:CI:12345678:ip:192.168.1.100

# O limpiar todo el rate limiting (¡CUIDADO EN PROD!)
KEYS ratelimit:* | xargs redis-cli DEL
```
