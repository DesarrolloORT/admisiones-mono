# 🧪 Testing Manual de Rate Limiting

## 📋 Prerequisitos

- API corriendo en `https://localhost:7150` (o el puerto configurado)
- Redis corriendo en `192.168.35.13:6379`
- Herramienta: `curl`, Postman, o REST Client de VS Code

---

## 🔒 Test 1: Login Rate Limiting (5 intentos / 15 min)

### **PowerShell Script**

```powershell
# Variables
$baseUrl = "https://localhost:7150"
$endpoint = "/Auth/Login"

# Hacer 6 intentos de login (5 permitidos + 1 bloqueado)
for ($i = 1; $i -le 6; $i++) {
	Write-Host "`n=== Intento $i ===" -ForegroundColor Cyan

	$body = @{
		codigoPersona = "12345"
		password = "wrongpassword"
	} | ConvertTo-Json

	try {
		$response = Invoke-WebRequest -Uri "$baseUrl$endpoint" `
			-Method POST `
			-ContentType "application/json" `
			-Body $body `
			-SkipCertificateCheck

		Write-Host "Status: $($response.StatusCode)" -ForegroundColor Green
		Write-Host "Body: $($response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 3)"
	} catch {
		$statusCode = $_.Exception.Response.StatusCode.value__

		if ($statusCode -eq 429) {
			Write-Host "Status: 429 Too Many Requests" -ForegroundColor Red

			$headers = $_.Exception.Response.Headers
			Write-Host "X-RateLimit-Limit: $($headers['X-RateLimit-Limit'])" -ForegroundColor Yellow
			Write-Host "X-RateLimit-Remaining: $($headers['X-RateLimit-Remaining'])" -ForegroundColor Yellow
			Write-Host "X-RateLimit-Reset: $($headers['X-RateLimit-Reset'])" -ForegroundColor Yellow
			Write-Host "Retry-After: $($headers['Retry-After'])" -ForegroundColor Yellow

			# Leer body del error
			$stream = $_.Exception.Response.GetResponseStream()
			$reader = New-Object System.IO.StreamReader($stream)
			$errorBody = $reader.ReadToEnd()
			Write-Host "Body: $errorBody" -ForegroundColor Gray
		} else {
			Write-Host "Status: $statusCode" -ForegroundColor Yellow
			Write-Host "Error: $($_.Exception.Message)"
		}
	}

	Start-Sleep -Seconds 1
}
```

---

### **cURL (Bash/Linux/WSL)**

```bash
#!/bin/bash

BASE_URL="https://localhost:7150"
ENDPOINT="/Auth/Login"

for i in {1..6}; do
	echo ""
	echo "=== Intento $i ==="

	curl -v -X POST "$BASE_URL$ENDPOINT" \
		-H "Content-Type: application/json" \
		-d '{
			"codigoPersona": "12345",
			"password": "wrongpassword"
		}' \
		--insecure \
		2>&1 | grep -E "< HTTP|< X-RateLimit|errorCode|message"

	sleep 1
done
```

---

### **Resultado Esperado**

```
=== Intento 1 ===
< HTTP/1.1 401 Unauthorized
{
  "errorCode": "AUTH_LOGIN_02",
  "message": "Credenciales inválidas"
}

=== Intento 2 ===
< HTTP/1.1 401 Unauthorized

=== Intento 3 ===
< HTTP/1.1 401 Unauthorized

=== Intento 4 ===
< HTTP/1.1 401 Unauthorized

=== Intento 5 ===
< HTTP/1.1 401 Unauthorized

=== Intento 6 ===
< HTTP/1.1 429 Too Many Requests
< X-RateLimit-Limit: 5
< X-RateLimit-Remaining: 0
< X-RateLimit-Reset: 1735689600
< Retry-After: 900

{
  "errorCode": "AUTH_RL_01",
  "message": "Se superó el límite de intentos de inicio de sesión (5 intentos cada 15 minutos). Por tu seguridad, intentá nuevamente más tarde."
}
```

---

## 📄 Test 2: Reconocimiento de Documento Rate Limiting (5 req / 1 min)

### **PowerShell Script**

```powershell
$baseUrl = "https://localhost:7150"
$endpoint = "/Registro/AnalizarAdjunto"

# Crear un archivo base64 fake (imagen pequeña)
$fakeImageBytes = [System.Text.Encoding]::UTF8.GetBytes("fake-image-data")
$base64Image = [Convert]::ToBase64String($fakeImageBytes)

for ($i = 1; $i -le 6; $i++) {
	Write-Host "`n=== Intento $i ===" -ForegroundColor Cyan

	$body = @{
		archivoAdjunto = @{
			archivo = $base64Image
			nombreArchivo = "documento.jpg"
		}
		tipoMime = "image/jpeg"
	} | ConvertTo-Json

	try {
		$response = Invoke-WebRequest -Uri "$baseUrl$endpoint" `
			-Method POST `
			-ContentType "application/json" `
			-Body $body `
			-SkipCertificateCheck

		Write-Host "Status: $($response.StatusCode)" -ForegroundColor Green
	} catch {
		$statusCode = $_.Exception.Response.StatusCode.value__
		Write-Host "Status: $statusCode" -ForegroundColor $(if ($statusCode -eq 429) { 'Red' } else { 'Yellow' })
	}

	Start-Sleep -Milliseconds 500
}
```

---

## 🔍 Test 3: Verificar Estado en Redis

### **Conectar a Redis**

```bash
redis-cli -h 192.168.35.13 -a Desarrollo2026
```

### **Comandos de Verificación**

```redis
# Listar todas las claves de rate limiting
KEYS ratelimit:*

# Ver requests de login para una IP específica
ZRANGE ratelimit:login-ip:192.168.1.100 0 -1 WITHSCORES

# Contar requests activos
ZCOUNT ratelimit:login-ip:192.168.1.100 -inf +inf

# Ver tiempo de expiración de la clave
TTL ratelimit:login-ip:192.168.1.100

# Limpiar manualmente (para reset de testing)
DEL ratelimit:login-ip:192.168.1.100

# Flush ALL (¡CUIDADO! Solo en testing)
FLUSHDB
```

### **Explicación de Sorted Set**

```
Clave: ratelimit:login-ip:192.168.1.100
Estructura: Sorted Set (ZSET)

ZRANGE salida:
1) "1735685123456"    # Member: timestamp del request 1
2) "1735685123456"    # Score: mismo timestamp
3) "1735685124789"    # Member: timestamp del request 2
4) "1735685124789"    # Score: mismo timestamp
5) "1735685126012"    # Request 3
6) "1735685126012"
7) "1735685127333"    # Request 4
8) "1735685127333"
9) "1735685128567"    # Request 5
10) "1735685128567"
```

**Cómo funciona:**
- Cada request se agrega con su timestamp (en milisegundos)
- Se eliminan requests fuera de la ventana (15 min)
- Si `COUNT` > límite (5) → Rechazar (429)

---

## 📊 Test 4: Monitoreo con Prometheus

### **Verificar Métricas**

```bash
# GET /metrics endpoint
curl https://localhost:7150/metrics | grep rate_limit

# Salida esperada:
# HELP login_rate_limit_rejections_total Cantidad de intentos de login rechazados por rate limit
# TYPE login_rate_limit_rejections_total counter
login_rate_limit_rejections_total 3

# HELP reconocimiento_documento_rate_limit_rejections_total Cantidad de solicitudes rechazadas
# TYPE reconocimiento_documento_rate_limit_rejections_total counter
reconocimiento_documento_rate_limit_rejections_total 1
```

### **Queries Prometheus**

```promql
# Tasa de rechazos por segundo
rate(login_rate_limit_rejections_total[1m])

# Total de rechazos en las últimas 24h
increase(login_rate_limit_rejections_total[24h])

# Alertar si tasa > 10 rechazos/min
rate(login_rate_limit_rejections_total[1m]) * 60 > 10
```

---

## 🧹 Test 5: Limpiar Estado (Reset Testing)

### **PowerShell**

```powershell
# Conectar a Redis y limpiar claves de testing
$redisHost = "192.168.35.13"
$redisPassword = "Desarrollo2026"

# Eliminar todas las claves de rate limiting
redis-cli -h $redisHost -a $redisPassword --scan --pattern "ratelimit:*" | ForEach-Object {
	redis-cli -h $redisHost -a $redisPassword DEL $_
}

Write-Host "✅ State de rate limiting limpiado" -ForegroundColor Green
```

### **Bash**

```bash
redis-cli -h 192.168.35.13 -a Desarrollo2026 --scan --pattern "ratelimit:*" | \
	xargs redis-cli -h 192.168.35.13 -a Desarrollo2026 DEL
```

---

## 🎯 Test 6: Escenarios Específicos

### **Escenario 1: Usuario Legítimo Bloqueado**

```powershell
# Simular usuario que olvidó su contraseña
for ($i = 1; $i -le 5; $i++) {
	# Intentos con contraseña incorrecta
	Invoke-WebRequest -Uri "https://localhost:7150/Auth/Login" `
		-Method POST -Body '{"codigoPersona":"54321","password":"wrong"}' `
		-ContentType "application/json" -SkipCertificateCheck
}

# Ahora el usuario recuerda la contraseña correcta pero está bloqueado
# Resultado: 429 (debe esperar 15 minutos o admin debe limpiar Redis)
```

**Solución:**
1. Usuario debe esperar 15 minutos
2. O contactar soporte para manual reset

---

### **Escenario 2: Ataque Distribuido**

```bash
# Simular requests desde 3 IPs diferentes
# (Requiere múltiples máquinas o VPN)

# IP 1: 192.168.1.10
for i in {1..5}; do curl ...; done

# IP 2: 192.168.1.11
for i in {1..5}; do curl ...; done

# IP 3: 192.168.1.12
for i in {1..5}; done

# Resultado: Cada IP tiene su propio contador (15 requests total aceptados)
```

**Mejora futura:** Rate limiting global adicional (ej: 50 req/min total)

---

### **Escenario 3: Redis Caído**

```powershell
# 1. Detener Redis
Stop-Service Redis  # (si está en Windows como servicio)

# 2. Hacer requests
Invoke-WebRequest ...

# Resultado esperado:
# - Log: "Redis connection error... Failing open (allowing request)"
# - Request PERMITIDO (fail-open strategy)
# - Status: 401 (credenciales inválidas) NO 429
```

---

## ✅ Checklist de Testing

- [ ] Login rate limiting funciona (429 después del 5to intento)
- [ ] Headers `X-RateLimit-*` presentes en respuesta 429
- [ ] Reconocimiento documento rate limiting funciona
- [ ] Redis almacena claves correctamente (`KEYS ratelimit:*`)
- [ ] Métricas Prometheus incrementan en `/metrics`
- [ ] Fail-open funciona cuando Redis está caído
- [ ] Contador se resetea después de la ventana (15 min)
- [ ] IPs diferentes tienen contadores independientes

---

## 📝 Notas

- **Localhost Testing:** En localhost, IP puede ser `::1` (IPv6). Redis usará `ratelimit:login-ip:::1`
- **Load Balancer:** En producción con LB, asegurar que `X-Forwarded-For` header esté configurado
- **Captcha Alternativo:** Considerar agregar CAPTCHA después del 3er intento fallido

---

**Happy Testing! 🚀**
