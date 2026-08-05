# Encuesta Inicial de Admisión

## 1. Objetivo

La encuesta inicial de admisión permite recolectar información complementaria del postulante en tres secciones:

- Educación
- Decisión académica
- Experiencia con ORT

La encuesta se crea inicialmente con estado `TEMPORAL` y pasa a estado `DEFINITIVO` cuando el backend valida que todos los datos obligatorios, incluyendo los condicionales, están completos.

---

## 2. Alcance

Forman parte de la encuesta únicamente estas secciones:

```text
Educación
Decisión académica
Experiencia con ORT
```

No forman parte de la encuesta inicial:

```text
Información personal
Verificación de identidad
Reglamento estudiantil
```

Estos datos pueden pertenecer al flujo general de admisión, pero no deben considerarse parte de esta encuesta.

---

## 3. Estados de la encuesta

Campo de estado:

```sql
ESTADO_ENCUESTA_INI_ADMISION
```

| Estado | Descripción |
|---|---|
| `TEMPORAL` | La encuesta fue creada o guardada parcialmente. Todavía faltan datos obligatorios. |
| `DEFINITIVO` | La encuesta tiene todos los datos requeridos completos. |

### Regla general

1. La encuesta se crea en estado `TEMPORAL`.
2. Luego de cada guardado, el backend valida la completitud de la encuesta.
3. Si todos los campos obligatorios están completos, pasa a `DEFINITIVO`.
4. Si falta algún dato obligatorio, permanece en `TEMPORAL`.

---

## 4. Datos completados por backend

Los siguientes datos no deben ser enviados por el frontend. El backend los completa, resuelve o delega su carga a base de datos.

| Campo lógico | Campo destino | Regla |
|---|---|---|
| Tipo de documento | `TIPO_DOCUMENTO` | Se obtiene desde persona/documento asociado. |
| Documento | `DOCUMENTO` | Se obtiene desde persona/documento asociado. |
| Código de persona | `CODIGO_PERSONA` | Se obtiene desde el contexto del postulante. |
| ID turno | `ID_TURNO` | Se obtiene desde admisión/proceso asociado. |
| Tipo de inscripción | `TIPO_INSCRIPCION` | Siempre `SOLO_ENCUESTA_INI`. |
| Clave de encuesta | `CLAVE_ENCUESTA_INI` | Generada por backend. |
| ID comienzo | `ID_COMIENZO` | El backend lo resuelve según producto/proceso. |
| Fecha encuesta | `FECHA_ENCUESTA_INI` | Se setea al crear la encuesta. |
| Estado encuesta | `ESTADO_ENCUESTA_INI_ADMISION` | `TEMPORAL` o `DEFINITIVO`. |
| Usuario ingreso | `USUARIO_INGRESO` | Lo carga un trigger. |
| Fecha ingreso | `FECHA_INGRESO` | Lo carga un trigger. |

---

## 5. Datos generales del flujo

### `carreraId`

Representa la carrera/producto seleccionado.

| Propiedad | Valor |
|---|---|
| Tipo | `number` |
| Origen | `catalogs/degree-programs` |
| Campo destino | `ID_PRODUCTO` |
| Obligatorio | Sí |

### `comienzoId`

El frontend no lo envía.

| Propiedad | Valor |
|---|---|
| Tipo | `number` |
| Origen | Backend |
| Campo destino | `ID_COMIENZO` |
| Regla | Se resuelve en backend según carrera/producto y proceso. |

---

## 6. Sección: Educación

Esta sección releva la trayectoria educativa previa del postulante y el nivel de formación de sus tutores.

### 6.1 Campos

| Campo frontend | Tipo | Obligatorio | Condición | Destino |
|---|---|---|---|---|
| `ubicacionUltimoAnioSecundariaId` | `number` | Sí | Siempre | `ULTIMOANIO_SECUNDARIA_ENCUESTA_INI` |
| `institucionSecundariaId` | `number` | Condicional | Si cursó en Uruguay | `CODIGO_INSTITUCION_BAC` |
| `nombreInstitucionSecundaria` | `string` | Condicional | Si cursó en el exterior | `NOMBRE_INST_SEC_ENCUESTA_INI` |
| `anioBachillerato` | `number` | Sí | Siempre | `ANIOS_INSTRUCCION_ENCUESTA_INI` |
| `orientacionBachilleratoId` | `number` | Condicional | Si el año seleccionado requiere orientación | `CODIGO_TITULO` |
| `recursaAnioBachillerato` | `boolean` | Sí | Siempre | No se guarda directo |
| `vecesRecursaAnioBachillerato` | `number` | Condicional | Si recursa = `true` | `VECES_SEXTO_ENCUESTA_INI` |
| `estadoEducacionSuperiorPreviaId` | `number` | Sí | Siempre | `TIENE_EDUCACION_SUPERIOR_ENCUESTA_INI` |
| `universidadEducacionSuperiorIds` | `array<number>` | Condicional | Si tuvo educación superior previa | Tabla hija `EDUCACION_SUPERIOR_ADMISION` |
| `nivelFormacionPadreTutorId` | `number` | Sí | Siempre | `INSTRUCCION_PADRE_ENCUESTA_INI` |
| `padreTutorEgresadoOrt` | `boolean` | Condicional | Si nivel padre/tutor está en `[5, 6]` | `INSTRUCCION_PADRE_ORT_ENCUESTA_INI` |
| `nivelFormacionMadreTutorId` | `number` | Sí | Siempre | `INSTRUCCION_MADRE_ENCUESTA_INI` |
| `madreTutorEgresadoOrt` | `boolean` | Condicional | Si nivel madre/tutor está en `[5, 6]` | `INSTRUCCION_MADRE_ORT_ENCUESTA_INI` |

### 6.2 Valores posibles

#### `ubicacionUltimoAnioSecundariaId`

| Valor | Label |
|---:|---|
| `1` | Uruguay |
| `2` | En el exterior |

#### `estadoEducacionSuperiorPreviaId`

| Valor | Label |
|---:|---|
| `1` | Sí, en Uruguay |
| `2` | Sí, en el exterior |
| `3` | No |

En base (`TIENE_EDUCACION_SUPERIOR_ENCUESTA_INI`):

```text
1 (Uruguay)  -> "SI" + universidades en EDUCACION_SUPERIOR_ADMISION.
2 (Exterior) -> "SI" sin universidades.
3 (No)       -> "NO".

En la lectura el id se reconstruye: "SI" con universidades = 1, "SI" sin
universidades = 2, "NO" = 3. La columna es un booleano SI/NO compartido con
el sistema legacy (no guarda un tercer valor para exterior).
```

#### `nivelFormacionPadreTutorId` / `nivelFormacionMadreTutorId`

| Valor | Label |
|---:|---|
| `1` | Primaria |
| `2` | Secundaria |
| `3` | Formación técnica |
| `4` | Formación universitaria incompleta |
| `5` | Formación universitaria completa |
| `6` | Estudios de postgrado |
| `7` | Otros estudios |

#### `padreTutorEgresadoOrt` / `madreTutorEgresadoOrt`

En frontend:

| Valor | Label |
|---|---|
| `true` | Sí |
| `false` | No |

En base:

| Valor | Significado |
|---|---|
| `S` | Sí obtuvo título en ORT |
| `N` | No obtuvo título en ORT |
| `NULL` | No aplica |

### 6.3 Reglas

#### Institución secundaria

```text
Si ubicacionUltimoAnioSecundariaId = 1:
    El postulante cursó en Uruguay.
    institucionSecundariaId es obligatorio.
    nombreInstitucionSecundaria no es obligatorio.

Si ubicacionUltimoAnioSecundariaId = 2:
    El postulante cursó en el exterior.
    nombreInstitucionSecundaria es obligatorio.
    institucionSecundariaId no es obligatorio.
```

#### Orientación de bachillerato

```text
orientacionBachilleratoId es obligatorio solo si el año de bachillerato seleccionado requiere orientación.

La regla no debe depender de un valor fijo como anioBachillerato == 6.
Debe depender de si el catálogo del año seleccionado trae orientaciones.
```

#### Nivel de carrera y año de bachillerato

```text
Si la carrera es de nivel 1 (universitaria):
    anioBachillerato no puede ser 4to (solo 5to o 6to).
```

#### Recursa año de bachillerato

```text
Si recursaAnioBachillerato = true:
    vecesRecursaAnioBachillerato es obligatorio.
    VECES_SEXTO_ENCUESTA_INI guarda la cantidad indicada.

Si recursaAnioBachillerato = false:
    vecesRecursaAnioBachillerato no aplica.
    VECES_SEXTO_ENCUESTA_INI queda NULL.
```

#### Educación superior previa

```text
Si estadoEducacionSuperiorPreviaId = 1:
    universidadEducacionSuperiorIds es obligatorio.
    Debe tener al menos un elemento.

Si estadoEducacionSuperiorPreviaId = 2 o 3:
    universidadEducacionSuperiorIds no aplica.
    El backend debe limpiar los registros hijos asociados.
```

#### Formación de tutores y título ORT

```text
Si nivelFormacionPadreTutorId está en [5, 6]:
    padreTutorEgresadoOrt es obligatorio.
    INSTRUCCION_PADRE_ORT_ENCUESTA_INI guarda S o N.

Si nivelFormacionPadreTutorId no está en [5, 6]:
    padreTutorEgresadoOrt no aplica.
    INSTRUCCION_PADRE_ORT_ENCUESTA_INI queda NULL.
```

```text
Si nivelFormacionMadreTutorId está en [5, 6]:
    madreTutorEgresadoOrt es obligatorio.
    INSTRUCCION_MADRE_ORT_ENCUESTA_INI guarda S o N.

Si nivelFormacionMadreTutorId no está en [5, 6]:
    madreTutorEgresadoOrt no aplica.
    INSTRUCCION_MADRE_ORT_ENCUESTA_INI queda NULL.
```

---

## 7. Sección: Decisión académica

Esta sección releva cuándo y cómo el postulante tomó la decisión de carrera e institución.

### 7.1 Campos

| Campo frontend | Tipo | Obligatorio | Condición | Destino |
|---|---|---|---|---|
| `anioDecisionCarreraId` | `number` | Sí | Siempre | `DECISION_CARRERA_ENCUESTA_INI` |
| `anioDecisionOrtId` | `number` | Sí | Siempre | `DECISION_UNIVER_ENCUESTA_INI` |
| `nivelDecisionId` | `number` | Sí | Siempre | `NIVEL_DECISION_ENCUESTA_INI` |
| `seInformoEnOtrasUniversidades` | `boolean` | Sí | Siempre | `INFOR_OTRAS_ANTES_ENCUESTA_INI` |
| `universidadConsideradaIds` | `array<number>` | Condicional | Si se informó en otras universidades | Tabla hija `EMPRESA_CONSIDERADA_ADMISION` |
| `informacionOtrasUniversidadesLinea1` | `string` | No | Opcional | `INFOR_OTRAS_LINEA1_INI` |
| `informacionOtrasUniversidadesLinea2` | `string` | No | Opcional | `INFOR_OTRAS_LINEA2_INI` |
| `apoyoDecisionId` | `number` | Sí | Siempre | Campos `COMPAR_*` |
| `motivoEleccionOrtIds` | `array<number>` | Sí | Siempre, mínimo 1 | Tabla hija `MOTIVO_ELECCION_ADMISION` |

### 7.2 Valores posibles

#### `anioDecisionCarreraId` / `anioDecisionOrtId`

| Valor | Label |
|---:|---|
| `2` | 1° EMS (4° año) |
| `3` | 2° EMS (5° año) |
| `4` | 3° EMS (6° año) |
| `0` | Otro |

#### `nivelDecisionId`

| Valor | Label |
|---:|---|
| `1` | Decidido/a |
| `2` | Con dudas |

#### `seInformoEnOtrasUniversidades`

En frontend:

| Valor | Label |
|---|---|
| `true` | Sí, lo hice |
| `false` | No |

En base:

| Valor | Campo |
|---|---|
| `S` | `INFOR_OTRAS_ANTES_ENCUESTA_INI` |
| `N` | `INFOR_OTRAS_ANTES_ENCUESTA_INI` |

#### `apoyoDecisionId`

| Valor | Label | Campo destino |
|---:|---|---|
| `1` | Padres u otros familiares | `COMPAR_PADRES_ENCUESTA_INI` |
| `2` | Amigos de la familia | `COMPAR_AMIGO_FAM_ENCUESTA_INI` |
| `3` | Amigos propios, compañeros | `COMPAR_AMIGO_PROP_ENCUESTA_INI` |
| `4` | Otros | `COMPAR_OTROS_ENCUESTA_INI` |
| `5` | Nadie | `COMPAR_NADIE_ENCUESTA_INI` |

Regla de guardado:

```text
Como apoyoDecisionId es una única opción, el backend debe marcar en S el campo correspondiente y dejar los demás en N.
```

Ejemplo:

```text
Si apoyoDecisionId = 3:

COMPAR_PADRES_ENCUESTA_INI = N
COMPAR_AMIGO_FAM_ENCUESTA_INI = N
COMPAR_AMIGO_PROP_ENCUESTA_INI = S
COMPAR_OTROS_ENCUESTA_INI = N
COMPAR_NADIE_ENCUESTA_INI = N
```

### 7.3 Reglas

#### Información en otras universidades

```text
Si seInformoEnOtrasUniversidades = true:
    universidadConsideradaIds es obligatorio.
    Debe tener al menos un elemento.

Si seInformoEnOtrasUniversidades = false:
    universidadConsideradaIds no aplica.
    El backend debe limpiar los registros hijos asociados.
```

#### Información libre sobre otras universidades

```text
informacionOtrasUniversidadesLinea1 e informacionOtrasUniversidadesLinea2 son campos libres opcionales.
No bloquean el pasaje a DEFINITIVO.
```

#### Motivos de elección ORT

```text
motivoEleccionOrtIds es obligatorio siempre.
Debe tener al menos un elemento.
```

---

## 8. Sección: Experiencia con ORT

Esta sección releva el contacto previo del postulante con ORT y la valoración de esa experiencia.

### 8.1 Campos

| Campo frontend | Tipo | Obligatorio | Condición | Destino |
|---|---|---|---|---|
| `tuvoAsesoramientoOrt` | `boolean` | Sí | Siempre | `ASESORAMIENTO_ORT_ENCUESTA_INI` |
| `valoracionAsesoramientoOrtId` | `number` | Condicional | Si tuvo asesoramiento | `VALORACION_ASESORAMIENTO_ORT_ENCUESTA_INI` |
| `visitoSitioWebOrt` | `boolean` | Sí | Siempre | `VISTA_SITIO_WEB_ORT_ENCUESTA_INI` |
| `valoracionSitioWebOrtId` | `number` | Condicional | Si visitó sitio web | `VALORACION_SITIO_WEB_ORT_ENCUESTA_INI` |
| `visitoInstalacionesOrt` | `boolean` | Sí | Siempre | `VISTA_INSTALACIONES_ORT_ENCUESTA_INI` |
| `valoracionInstalacionesOrtId` | `number` | Condicional | Si visitó instalaciones | `VALORACION_INSTALACIONES_ORT_ENCUESTA_INI` |
| `recuerdaPublicidadOrt` | `boolean` | Sí | Siempre | `PUBLICIDAD_ORT_ENCUESTA_INI` |
| `publicidadOrtIds` | `array<number>` | Condicional | Si recuerda publicidad | Tabla hija `PUBLICIDAD_ELECCION_ADMISION` |

### 8.2 Valores posibles

#### Booleanos

Aplica para:

- `tuvoAsesoramientoOrt`
- `visitoSitioWebOrt`
- `visitoInstalacionesOrt`
- `recuerdaPublicidadOrt`

En frontend:

| Valor | Label |
|---|---|
| `true` | Sí |
| `false` | No |

En base:

| Valor | Significado |
|---|---|
| `S` | Sí |
| `N` | No |

#### Valoraciones

Aplica para:

- `valoracionAsesoramientoOrtId`
- `valoracionSitioWebOrtId`
- `valoracionInstalacionesOrtId`

| Valor | Label |
|---:|---|
| `1` | 1 |
| `2` | 2 |
| `3` | 3 |
| `4` | 4 |
| `5` | 5 |

### 8.3 Reglas

#### Asesoramiento

```text
Si tuvoAsesoramientoOrt = true:
    valoracionAsesoramientoOrtId es obligatorio.

Si tuvoAsesoramientoOrt = false:
    valoracionAsesoramientoOrtId no aplica.
    El backend debe limpiar el valor anterior si existía.
```

#### Sitio web

```text
Si visitoSitioWebOrt = true:
    valoracionSitioWebOrtId es obligatorio.

Si visitoSitioWebOrt = false:
    valoracionSitioWebOrtId no aplica.
    El backend debe limpiar el valor anterior si existía.
```

#### Instalaciones

```text
Si visitoInstalacionesOrt = true:
    valoracionInstalacionesOrtId es obligatorio.

Si visitoInstalacionesOrt = false:
    valoracionInstalacionesOrtId no aplica.
    El backend debe limpiar el valor anterior si existía.
```

#### Publicidad

```text
Si recuerdaPublicidadOrt = true:
    publicidadOrtIds es obligatorio.
    Debe tener al menos un elemento.

Si recuerdaPublicidadOrt = false:
    publicidadOrtIds no aplica.
    El backend debe limpiar los registros hijos asociados.
```

---

## 9. Tablas hijas

La encuesta utiliza tablas hijas para campos de selección múltiple.

### Universidad considerada

Campo frontend:

```text
universidadConsideradaIds
```

Destino:

```text
EMPRESA_CONSIDERADA_ADMISION
```

Regla:

```text
Si seInformoEnOtrasUniversidades = false:
    eliminar registros hijos asociados.

Si seInformoEnOtrasUniversidades = true:
    reemplazar registros existentes por los enviados.
```

### Educación superior previa

Campo frontend:

```text
universidadEducacionSuperiorIds
```

Destino:

```text
EDUCACION_SUPERIOR_ADMISION
```

Regla:

```text
Si estadoEducacionSuperiorPreviaId = 2 o 3:
    eliminar registros hijos asociados.

Si estadoEducacionSuperiorPreviaId = 1:
    reemplazar registros existentes por los enviados.
```

### Publicidades recordadas

Campo frontend:

```text
publicidadOrtIds
```

Destino:

```text
PUBLICIDAD_ELECCION_ADMISION
```

Regla:

```text
Si recuerdaPublicidadOrt = false:
    eliminar registros hijos asociados.

Si recuerdaPublicidadOrt = true:
    reemplazar registros existentes por los enviados.
```

### Motivos de elección ORT

Campo frontend:

```text
motivoEleccionOrtIds
```

Destino:

```text
MOTIVO_ELECCION_ADMISION
```

Regla:

```text
Siempre que venga informado, se reemplazan los registros existentes.
Para pasar a DEFINITIVO debe existir al menos un motivo.
```

---

## 10. Reglas de limpieza de datos

Cuando una respuesta condicional deja de aplicar, el backend debe limpiar los valores dependientes.

```text
Si ubicacionUltimoAnioSecundariaId = 1 Uruguay:
    limpiar nombreInstitucionSecundaria si corresponde.

Si ubicacionUltimoAnioSecundariaId = 2 Exterior:
    limpiar institucionSecundariaId si corresponde.
```

```text
Si recursaAnioBachillerato = false:
    VECES_SEXTO_ENCUESTA_INI queda NULL.
```

```text
Si estadoEducacionSuperiorPreviaId = 2 o 3:
    eliminar universidadEducacionSuperiorIds.
```

```text
Si seInformoEnOtrasUniversidades = false:
    eliminar universidadConsideradaIds.
```

```text
Si tuvoAsesoramientoOrt = false:
    limpiar valoracionAsesoramientoOrtId.
```

```text
Si visitoSitioWebOrt = false:
    limpiar valoracionSitioWebOrtId.
```

```text
Si visitoInstalacionesOrt = false:
    limpiar valoracionInstalacionesOrtId.
```

```text
Si recuerdaPublicidadOrt = false:
    eliminar publicidadOrtIds.
```

```text
Si nivelFormacionPadreTutorId no está en [5, 6]:
    INSTRUCCION_PADRE_ORT_ENCUESTA_INI queda NULL.

Si nivelFormacionMadreTutorId no está en [5, 6]:
    INSTRUCCION_MADRE_ORT_ENCUESTA_INI queda NULL.
```

---

## 11. Regla de completitud para pasar a DEFINITIVO

La encuesta pasa a `DEFINITIVO` cuando se cumplen todas las condiciones siguientes.

### 11.1 Datos técnicos

```text
ID_PRODUCTO completo.
ID_COMIENZO completo.
CODIGO_PERSONA completo.
TIPO_DOCUMENTO completo.
DOCUMENTO completo.
ID_TURNO completo.
TIPO_INSCRIPCION = SOLO_ENCUESTA_INI.
CLAVE_ENCUESTA_INI completa.
```

### 11.2 Educación completa

```text
ULTIMOANIO_SECUNDARIA_ENCUESTA_INI completo.

Si cursó en Uruguay:
    CODIGO_INSTITUCION_BAC completo.

Si cursó en el exterior:
    NOMBRE_INST_SEC_ENCUESTA_INI completo.

ANIOS_INSTRUCCION_ENCUESTA_INI completo.

Si el año seleccionado tiene orientaciones:
    CODIGO_TITULO completo.

recursaAnioBachillerato respondido.

Si recursaAnioBachillerato = true:
    VECES_SEXTO_ENCUESTA_INI completo.

Si recursaAnioBachillerato = false:
    VECES_SEXTO_ENCUESTA_INI puede quedar NULL.

TIENE_EDUCACION_SUPERIOR_ENCUESTA_INI completo.

Si estadoEducacionSuperiorPreviaId = 1:
    Debe existir al menos un registro en EDUCACION_SUPERIOR_ADMISION.

INSTRUCCION_PADRE_ENCUESTA_INI completo.

Si INSTRUCCION_PADRE_ENCUESTA_INI está en [5, 6]:
    INSTRUCCION_PADRE_ORT_ENCUESTA_INI completo con S o N.

Si INSTRUCCION_PADRE_ENCUESTA_INI no está en [5, 6]:
    INSTRUCCION_PADRE_ORT_ENCUESTA_INI puede quedar NULL.

INSTRUCCION_MADRE_ENCUESTA_INI completo.

Si INSTRUCCION_MADRE_ENCUESTA_INI está en [5, 6]:
    INSTRUCCION_MADRE_ORT_ENCUESTA_INI completo con S o N.

Si INSTRUCCION_MADRE_ENCUESTA_INI no está en [5, 6]:
    INSTRUCCION_MADRE_ORT_ENCUESTA_INI puede quedar NULL.
```

### 11.3 Decisión académica completa

```text
DECISION_CARRERA_ENCUESTA_INI completo.
DECISION_UNIVER_ENCUESTA_INI completo.
NIVEL_DECISION_ENCUESTA_INI completo.
INFOR_OTRAS_ANTES_ENCUESTA_INI completo con S o N.

Si INFOR_OTRAS_ANTES_ENCUESTA_INI = S:
    Debe existir al menos un registro en EMPRESA_CONSIDERADA_ADMISION.

Debe existir un único apoyo de decisión representado en COMPAR_*.
Debe existir al menos un registro en MOTIVO_ELECCION_ADMISION.
```

Los siguientes campos no bloquean definitivo:

```text
INFOR_OTRAS_LINEA1_INI
INFOR_OTRAS_LINEA2_INI
```

### 11.4 Experiencia con ORT completa

```text
ASESORAMIENTO_ORT_ENCUESTA_INI completo con S o N.

Si ASESORAMIENTO_ORT_ENCUESTA_INI = S:
    VALORACION_ASESORAMIENTO_ORT_ENCUESTA_INI completo.

VISTA_SITIO_WEB_ORT_ENCUESTA_INI completo con S o N.

Si VISTA_SITIO_WEB_ORT_ENCUESTA_INI = S:
    VALORACION_SITIO_WEB_ORT_ENCUESTA_INI completo.

VISTA_INSTALACIONES_ORT_ENCUESTA_INI completo con S o N.

Si VISTA_INSTALACIONES_ORT_ENCUESTA_INI = S:
    VALORACION_INSTALACIONES_ORT_ENCUESTA_INI completo.

PUBLICIDAD_ORT_ENCUESTA_INI completo con S o N.

Si PUBLICIDAD_ORT_ENCUESTA_INI = S:
    Debe existir al menos un registro en PUBLICIDAD_ELECCION_ADMISION.
```