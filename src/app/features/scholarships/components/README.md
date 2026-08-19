Componentes de presentacion del flujo de becas (cards y onboarding): solo
reciben `input()` y no inyectan servicios.

Los pasos del proceso (`scholarship-*-step`) **no** viven aca: son parte de la
page que los renderiza y estan en
[`pages/scholarship-process/steps/`](../pages/scholarship-process/steps/), junto con las secciones que cada paso
usa. Ver el [README de la feature](../README.md) y
[`docs/arquitectura/flujo-pasos.md`](../../../../../docs/arquitectura/flujo-pasos.md).
