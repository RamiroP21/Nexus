# Nexus — contrato operativo para agentes

## Identidad y dirección

Nexus es un RPG/simulador single-player de superhumanos para Windows PC.
Hipótesis: Being a superhuman in a city that reacts, remembers, and changes because of your actions can be an exceptional game experience.
Loop conceptual: Power → Reaction → Consequence.

La base implementada es headless: kernel determinista y ECS (Fases 0 y 1). La visión futura no autoriza implementar sus sistemas. No introducir gameplay, Fase 2 o Unity sin una misión explícita.

## Fuentes de verdad y autoridad

Prioridad para resolver el estado y los contratos del proyecto: código + tests → documentación arquitectónica versionada → Git → AGENTS.md. Los prompts temporales no reemplazan contratos permanentes; una revisión autorizada debe quedar explícita y versionada, no asumirse silenciosamente.

Leer solo lo relevante: [README](README.md), [principios](docs/architecture/principles.md), [kernel](docs/architecture/phase-0-kernel.md), [ECS](docs/architecture/phase-1-ecs.md) y [visión](docs/vision/game-vision.md). Las descripciones históricas de fase no sustituyen el estado comprobado en código/tests. Este archivo orienta la operación, no duplica las especificaciones.

External Agents Are Non-Authoritative. Pueden investigar, proponer, implementar dentro de una misión autorizada y ejecutar herramientas/tests. No pueden cambiar silenciosamente la visión o arquitectura central. Una crítica externa, otro modelo, una librería de moda o una alternativa no constituye una decisión. Las decisiones existentes permanecen hasta que evidencia o un spike autorizado justifique revisarlas y se apruebe/documente el cambio.

## Git safety

- Inspeccionar raíz, rama, origin, status, diff e instrucciones aplicables antes de editar; preservar todo dirty work y detenerse ante cambios inesperados o solapamientos inseguros.
- No ejecutar reset/clean/revert destructivos ni sobrescribir trabajo sin autorización explícita. Nunca force push; no cambiar remotes fuera de alcance.
- No incorporar secretos, configuración personal ni artefactos `.vs`, `bin`, `obj`.
- Build/tests antes de commit. Commit/push solo autorizados y con staging limitado a la misión; no publicar trabajo incompleto salvo instrucción explícita.
- Revisar diff/staging antes de publicar; verificar SHA remoto y status después. Git es el mecanismo de rollback, no la memoria del modelo; la existencia de Git no vuelve recuperable trabajo nunca guardado.

## Alcance y misión

Small and validated beats broad and incomplete. Depth before breadth.
No añadir features no solicitadas, refactors cosméticos, frameworks prematuros, abstracciones especulativas ni optimizaciones sin medición.

Para misiones importantes, concretar Objective, Constraints / Invariants, Evidence, Budget, Stop Conditions y Definition of Done antes de implementar; usar el [brief mínimo](docs/process/agentic-development.md). Si una contradicción exige cambiar arquitectura central, detenerse y reportar evidencia en vez de ampliar scope. Agotar el presupuesto no significa completar la misión.

## Gameplay y superficie

No construir un cerebro impresionante alrededor de un cuerpo olvidable. Technical Complete no equivale a Experiential Complete: gameplay feel, reacción, decisiones, pacing y replayability necesitan validación jugable, no solo tests técnicos.

No invisible simulation without visible consequence. Si un sistema de gameplay carece de una superficie perceptible para el jugador, cuestionar su prioridad; esto no obliga a añadir presentación al kernel headless.

La secuencia experimental prevista es Feel → Reactivity → Triage → Persistence → Vertical Slice. Cada experimento requiere una misión autorizada y evidencia jugable antes de adelantar sistemas posteriores.

## Determinismo

Preservar los contratos enlazados, sus versiones y vectores; no aceptar nuevos hashes a ciegas. No usar `GetHashCode`, identidad CLR runtime, reflexión o serialización incidental como identidad determinista, ni semánticas ocultas sin orden. El lookup runtime de stores no define identidad canónica.

La ejecución secuencial sigue siendo baseline hasta que evidencia justifique y se autorice cambiarla. Mantener commands FIFO al final del tick, commands anidados al siguiente tick y events visibles durante el siguiente. Hash completo solo en fronteras cerradas, incluido todo estado futuro relevante y trabajo pendiente. Dense order no promete orden semántico: usar la ruta explícita por EntityId cuando sea necesario.

## Economía y validación

Quota-Bounded Autonomous Development. Tool Leverage > repeated reasoning: preferir scripts, tests, herramientas deterministas y APIs semánticas; reutilizar evidencia vigente y evitar lecturas/razonamiento repetitivos.

No crear subagentes por defecto. Delegar solo una tarea claramente independiente, cuyo coste de verificar sea menor que producirla y que aporte paralelismo real o proteja contexto; respetar además los límites de delegación de la misión.

Produce → Execute → Observe → Compare → Correct. No afirmar que algo funciona sin ejecutarlo cuando sea razonablemente ejecutable. Desde la raíz: `dotnet build Nexus.sln -c Release` y `dotnet test Nexus.sln -c Release --no-build`. Para cambios de simulación, añadir los diagnósticos del Host descritos en README y comparar resultados, no solo exit codes. Exigir 0 errores/warnings, todos los tests pasando y diff limitado al alcance. Distinguir comprobaciones realizadas, omitidas y bloqueadas.

## Integraciones futuras

Unity/Blender: código/API para construir; pantalla para verificar. Preferir APIs, scripting, CLI y tooling semántico reproducible. Computer Use principalmente para inspección visual, validación y operaciones sin API práctica. Estas reglas no autorizan implementar las integraciones ahora.
