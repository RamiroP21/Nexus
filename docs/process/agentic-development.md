# Mission Brief mínimo

Usar este formato en la propia misión o informe; no crear un expediente por tarea. Las reglas permanentes están en [AGENTS.md](../../AGENTS.md).

- **Objective:** un resultado concreto y acotado, con superficie perceptible si es gameplay.
- **Constraints / Invariants:** contratos que se preservan, rutas autorizadas/protegidas y permiso o prohibición de publicar.
- **Evidence:** baseline (SHA/status), reproducción y comprobaciones que demostrarán el resultado; criterio jugable si corresponde.
- **Budget:** límite acordado de tiempo/cuota y punto de revisión. No inventar un presupuesto no proporcionado ni usarlo como permiso para ampliar alcance.
- **Stop Conditions:** contradicción arquitectónica, trabajo local inesperado, necesidad de autorización nueva, presupuesto agotado o intentos de reparación que no convergen. Reportar estado recuperable y siguiente decisión necesaria.
- **Definition of Done:** resultado observable, validación técnica/experiencial pertinente, diff revisado y estado de entrega/publicación verificable.

## Medición ligera

En misiones importantes, anotar inicio/fin wall-clock y, si está disponible, porcentaje de cuota usado antes/después, ventana, reset y actividad concurrente. El consumo es compartido: una diferencia no demuestra el coste exclusivo de la misión; si cruza un reset o faltan datos, marcarla no comparable. No estimar porcentajes inventados ni consultar continuamente.

Agent Leverage compara resultado aceptado y esfuerzo humano evitado con coste total de producción, revisión y retrabajo. Sin baseline humano, reportar indicadores observables, no una cifra de productividad ficticia: alcance validado, tiempo, cuota disponible, fallos corregidos y revisión requerida.

Antes de delegar, identificar entregable independiente, verificación concreta y ahorro esperado. Si coordinar/verificar cuesta más que resolver con una herramienta o test, mantenerlo local. Automatizar tareas repetibles antes de gastar más razonamiento; no construir un framework para medirlo.
