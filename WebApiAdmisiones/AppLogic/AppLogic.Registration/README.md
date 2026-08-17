# AppLogic.Registration

**Nivel 5 — depende de `Contracts`, `DevartDtos`, `Identity`, `Platform`, `Authentication`, `People`
y `Core/AzureService`.** Es el módulo con más dependencias, porque el registro toca todo.

## Qué resuelve

El **onboarding**: desde que alguien escribe su documento hasta que existe como persona con usuario
LDAP y contraseña.

## La decisión central: cédula vs. resto

`IdentityDocumentRules.IsNationalId()` (que vive en `AppLogic.Identity`) bifurca todo el módulo:

- **Cédula uruguaya** → flujo automático. Se evalúa contra el padrón, y según el caso se verifica la
  identidad (persona ya existente) o se da de alta una persona nueva.
- **Pasaporte o documento extranjero** → **solicitud de alta**. No se crea ninguna persona: queda una
  fila en `T_SOLICITUD_ALTA` que revisa admisiones a mano.

## Los caminos

```
POST registration/evaluate-document        IEvaluateDocument
   ├─ no es CI → ¿ya hay solicitud?  → RegistrationRequestPending / RequiresRegistrationRequest
   ├─ CI, no existe la persona       → RequiresPersonRegistration
   ├─ CI, existe y tiene LDAP        → UserAlreadyRegistered  (no se crea flowId)
   └─ CI, existe sin LDAP            → RequiresIdentityVerification
                                        ↓
POST registration/verify-identity          IVerifyIdentity
   valida apellido y mail contra el padrón, crea usuario LDAP,
   registra la admisión y manda el mail de activación

POST registration/confirm-new-person       IRegistrationFlowService.ConfirmNewPersonAsync
   ⚠️ NO crea la persona: valida (IValidateNewPerson), guarda en Redis y manda el mail.
   La creación real se difiere a auth/complete-initial-password.

POST registration/confirm-registration-request   IConfirmRegistrationRequest
   crea la fila en T_SOLICITUD_ALTA
```

Los dos endpoints de confirmación devuelven el mismo `RegistrationFlowResult`. El flag que separa los
dos finales es **`PendingReview`**: en `true` no hay persona, ni usuario LDAP, ni mail de activación —
el flujo termina ahí y admisiones revisa a mano. El front no puede deducirlo del mensaje.
`Docs/contracts/registro.contract.json` documenta los cinco caminos y los tres finales; lo verifica
`RegistroContractTests`.

El alta diferida se cierra desde Authentication, que llama a `ICompleteNewPerson` a través de
`IPendingRegistrationCompletion`.

## La sesión de flujo (`X-Flow-Id`)

`EvaluateDocument` crea una sesión en Redis (`registro:flow-session:{flowId}`) y devuelve el `flowId`
en la respuesta. Los pasos siguientes lo mandan en el header `X-Flow-Id`.

El controller valida en cada paso que la sesión exista, que esté en el `Step` esperado
(`RegistrationFlowConstants.Step`: `evaluado` → `identidad_verificada` / `confirmado`) y que el
documento del body coincida con el de la sesión. Sirve para que no se pueda saltar pasos ni cambiar
de documento en el medio.

## ⚠️ `CompleteNewPerson`: dos transacciones con LDAP en el medio

Esto es lo más delicado del módulo. El orden es:

1. **Tx #1** — `CreatePersonInDb`: inserta la persona y **commitea**.
2. **Fuera de transacción** — crea el usuario LDAP y le fuerza la contraseña.
3. **Tx #2** — `PersistMetadataAndAdmission`: metadata de contraseña, alta de admisión e imágenes.

No se puede hacer todo en una transacción porque LDAP no es transaccional. Las consecuencias están
asumidas y documentadas en el código:

- Si falla LDAP en el paso 2, **la persona queda creada** en la base. Se loguea "estado
  inconsistente". El reintento la encuentra por `GetByDocumento` y entra por
  `CompletePasswordForPendingExistingPersonAsync`, que solo completa la contraseña.
- Si falla la Tx #2, la persona tiene LDAP activo pero sin metadata ni admisión. También se loguea.

**No conviertas esto en una sola transacción** ni reordenes los pasos sin entender los tres logs de
estado inconsistente.

Detalle no obvio: `T_PERSONA.FECHA_CONF_DATOS_PERSONA` tiene `DEFAULT sysdate` en Oracle, así que se
completa sola en el INSERT. Se la pone explícitamente en `null` con un segundo `Save()` para forzar
la confirmación de datos en Autoservicio.

## Qué expone

| Interfaz | Endpoint |
|---|---|
| `IEvaluateDocument` | `POST registration/evaluate-document` |
| `IVerifyIdentity` | `POST registration/verify-identity` |
| `IValidateNewPerson` | interno, lo usa `RegistrationFlowService` |
| `ICompleteNewPerson` | interno, lo llama Authentication |
| `IConfirmRegistrationRequest` | `POST registration/confirm-registration-request` |
| `IRegistrationFlowService` | sesión de flujo + `ConfirmNewPersonAsync` |

`IRegistrationFlowService` **extiende** `IPendingRegistrationCompletion` (declarada en
Authentication). Ese es el binding que rompe el ciclo Authentication ↔ Registration:

```csharp
services.AddScoped<IPendingRegistrationCompletion>(sp =>
    sp.GetRequiredService<IRegistrationFlowService>());
```

## Colaboradores compartidos

- **`Services/LdapUserDirectory`** — `UserExistsAsync`, `CreateUserAsync`, `ForcePasswordAsync`.
  Cuando hay `IServiceScopeFactory` resuelve `ILdap` en un scope propio (las llamadas corren fuera de
  la transacción y no deben quedar atadas al scope de la request). En tests el factory va en null.
- **`Services/AdmissionRecords`** — alta en `T_REGISTRO_ADMISIONES`, colgando o de la persona o de la
  solicitud de alta, nunca de las dos.

## Reconocimiento de documento

`POST registration/analyze-attachment` usa `Core/AzureService` (Azure Document Intelligence). Los
DTOs de Core están en español y **no se pueden renombrar** (submódulo compartido), así que
`Mapping/DocumentRecognitionMapper` los traduce a `DocumentRecognitionResponse`. Esa es la única
razón por la que este proyecto referencia `AzureService`.

Las imágenes reconocidas se guardan en Redis (`registro:documento-imagenes:{tipo}:{doc}`) y se
persisten recién al completar el alta.

## Estructura

```
Contracts/IRegistrationUseCases.cs   las 5 interfaces de caso de uso
UseCases/                            una clase por caso de uso
Services/RegistrationFlowService     sesión Redis + orquestación de ConfirmNewPerson
Services/LdapUserDirectory           acceso a LDAP scope-aware
Services/AdmissionRecords            alta en T_REGISTRO_ADMISIONES
Mapping/RegistrationMapper           request → Persona / SolicitudAlta / ParamCrearUsuarioLdap
Mapping/DocumentRecognitionMapper    Core/AzureService → DTO propio
Validators/RegistrationValidation    MatchesExistingPerson + ResolveDocumentValidationCode
Constants/RegistrationErrorCodes     códigos compartidos entre casos de uso
Constants/RegistrationFlowConstants  los Step válidos
Constants/SgiPersonRecordConstants   valores fijos del alta en SGI
```

## Códigos de error

`REG_REQUEST_01` (body ausente), `REG_DOC_01/02/03` (documento), `REG_PERSONA_*`,
`REG_PERSONA_VERIF_01`, `REG_USUARIO_01`, `REG_CIUDAD_01`, `REG_SOLICITUD_99`, `REG_ADMISIONES_99`,
`INI_PAS_02` (contraseña), `FLOW_*` (sesión de flujo).
