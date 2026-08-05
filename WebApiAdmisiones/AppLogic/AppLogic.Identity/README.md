# AppLogic.Identity

**Nivel 3 — depende de `AppLogic.Contracts` y `AppLogic.Platform`.**

## Qué resuelve

Todo lo que tiene que ver con **la identidad de una persona y su documento**, antes de que exista
una sesión. Nació para romper un ciclo de dependencias: `Autenticacion` ↔ `Personas` ↔ `Registro`
se referenciaban entre sí porque los tres necesitaban las mismas reglas de documento y los mismos
stores de Redis. Todo eso bajó acá.

Si dos de esos tres módulos necesitan lo mismo, el lugar es éste.

## Qué expone

### Reglas puras

`IdentityDocumentRules` — sin dependencias, sin I/O:

- `IsNationalId(documentType)` — **la decisión de negocio central del registro**: cédula ⇒ flujo de
  alta de persona; cualquier otro documento ⇒ solicitud de alta que revisa admisiones a mano.
- `ValidateBaseDocument(tipo, documento)` — devuelve `DocumentValidationResult` con un enum
  `DocumentValidationError`, no un `OperationResult`. El código de error HTTP lo pone cada módulo
  (Registration usa `REG_DOC_01/02`, Authentication `LOGIN_LDAP_02/03`, `REC_PAS_02/03`).
- `NormalizeDocumentType`, `NormalizeIdentityDocument`.

### Servicio de documentos

`IdentityDocumentService` (estático) — imágenes de documento e foto de perfil: validación,
creación/actualización de `ImagenTemporal`, persistencia de las imágenes reconocidas, nombres de
archivo, y los helpers `*Safe*` que **no lanzan** si Redis falla.

### Stores de Redis

| Interfaz | Implementación | Keys |
|---|---|---|
| `IPendingPersonStore` | `RedisPendingPersonStore` | `registro:pending:{flowId}` y `registro:pending-doc:{tipo}:{doc}` |
| `IIdentityDocumentImageCache` | `RedisIdentityDocumentImageCache` | `registro:documento-imagenes:{tipo}:{doc}` |

Se registran con `services.AddIdentityModule()`.

### DTOs compartidos

`AuthenticatedPerson` (la identidad que viaja en la respuesta de login), `PendingPerson`,
`TemporaryDocumentImages`, `IdentityDocumentFile`, `IdentityDocumentQuery`.

## Trampas

- **`IPendingPersonStore`: Registration es dueño del ciclo de vida** (guarda y borra), Authentication
  solo lo consume para activar el link de la persona nueva. Si Authentication empieza a borrar, se
  rompe la invariante.
- **El mapeo documento→flowId puede quedar stale.** `ResolveFlowIdByDocumentAsync` detecta el caso
  (el mapeo apunta a un flowId cuyo pending ya expiró), lo limpia y devuelve null. No asumas que si
  existe el mapeo existe el pending.
- **`GetTemporaryImagesSafeAsync` y `DeleteTemporaryImagesSafeAsync` se tragan los errores a
  propósito**: loguean warning y siguen. Un fallo de Redis no debe abortar un alta de persona que ya
  se commiteó en la base.
- **`ValidateRecognizedDocumentImages` devuelve `OperationResult`, no enum.** Está documentado como
  excepción: propaga códigos que dependen del contexto del llamador.
- Las imágenes temporales viven en Redis con TTL. Si expiran entre el reconocimiento del documento y
  la confirmación del alta, la persona se crea **sin** las imágenes, y eso es aceptado: el flujo no
  falla por eso.

## Estructura

```
IdentityDocumentRules.cs      reglas puras (raíz, sin carpeta: es EL tipo del módulo)
Constants/                    tipos de imagen (Front/Back/Photo)
Dtos/                         AuthenticatedPerson, PendingPerson, TemporaryDocumentImages…
Interfaces/                   los dos stores de Redis
Services/                     IdentityDocumentService + las dos implementaciones Redis
```

## Códigos de error

`GEN_DA_*` (documento de admisión), `INS_CPI_*`.
