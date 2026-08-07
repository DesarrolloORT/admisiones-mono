# AppLogic.DevartDtos

**Nivel 1 — código generado. NO SE EDITA A MANO.**

## Qué resuelve

Contiene los DTOs y converters que genera **Entity Developer (Devart)** a partir del modelo de la
base Oracle: 159 archivos, ~18.500 líneas. Son la representación serializable de las entidades de
`BusinessLogic`.

## Por qué no se toca

Estos archivos se **regeneran** desde la herramienta de Devart. Cualquier cambio hecho a mano se
pierde en la próxima generación. Los nombres están en español porque reflejan las columnas del
esquema Oracle (`CodigoPersona`, `IdInscripto`, `NombreExtensoProducto`).

Durante el refactor a inglés este proyecto quedó **explícitamente excluido**, junto con
`BusinessLogic`, `DataAccess` y el submódulo `Core/`.

## Excepción: los `.Extensions.cs`

Los archivos `*.Extensions.cs` (por ejemplo `DtoVdInscripcionesFresco1y2Devart.Extensions.cs`) son
**partial classes escritas a mano** que sobreviven a la regeneración. Ahí sí se puede tocar.

## Trampa conocida: churn en cada build

`dotnet build` regenera ~280 archivos de este proyecto con timestamp nuevo, aunque el contenido no
cambie. Antes de commitear, verificá qué cambió de verdad:

```bash
git diff --numstat -- WebApiAdmisiones/AppLogic/AppLogic.DevartDtos | awk '$1!="0"||$2!="0"'
```

Si aparecen archivos con líneas cambiadas que no tocaste a propósito, revertilos:

```bash
git checkout -- WebApiAdmisiones/AppLogic/AppLogic.DevartDtos
```

## Trampa: los renames masivos lo pisan

Cualquier `sed -i` recursivo sobre `*.cs` va a tocar este proyecto. **Excluilo siempre**:

```bash
find . -name "*.cs" -not -path "./AppLogic.DevartDtos/*" -not -path "./BusinessLogic/*" ...
```

## Quién lo usa

`Scholarships`, `Registration`, `Enrollments` y `Catalogs`. En todos los casos **solo como fuente**:
ninguno de esos DTOs sale al front, se mapean a DTOs propios en inglés. Ese es el límite
anticorrupción de la capa AppLogic.
