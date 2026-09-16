# Fuente OWASP ASVS

La fuente normativa es **OWASP ASVS 5.0.0**, sin modificaciones. El catálogo oficial completo en inglés está guardado en [OWASP_Application_Security_Verification_Standard_5.0.0_en.json](./OWASP_Application_Security_Verification_Standard_5.0.0_en.json), descargado del tag `v5.0.0` de [OWASP/ASVS](https://github.com/OWASP/ASVS/tree/v5.0.0/5.0/docs_en).

- [URL de descarga oficial](https://raw.githubusercontent.com/OWASP/ASVS/v5.0.0/5.0/docs_en/OWASP_Application_Security_Verification_Standard_5.0.0_en.json).
- Fecha de descarga: 2026-09-16.
- SHA-256: `bcdbec214d70abcfad9284a31d4f9e5134305831d628aad3aa85d7e26626cb35`.
- Licencia: [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/).

El JSON conserva la estructura oficial: `Requirements` contiene capítulos, sus `Items` contienen secciones y los `Items` de cada sección contienen requisitos con `Shortcode`, `Description` y `L` (nivel mínimo). Para evaluar L2 se incluyen requisitos de niveles 1 y 2.

La selección evaluada actualmente sigue en `../controls/pilot.json`; incorporar esta fuente no amplía automáticamente el piloto. Los estados, la evidencia y las interpretaciones locales se mantienen fuera del catálogo oficial, en `../controls/`, `../../evidence/` y `../applicability/`.

Formato de ID: `v5.0.0-<requirement>`. No actualizar la versión sin revisar IDs y registrar una ADR.
