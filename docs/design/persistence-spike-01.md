# Persistence Spike 01 — memory after consequence

**TECHNICAL VALIDATION: NOT STARTED**  
**HUMAN EXPERIENTIAL VALIDATION: NOT STARTED**

## Pregunta central

¿Ver posteriormente una consecuencia producida por una decisión anterior hace que el mundo parezca recordar al jugador?

Cadena conceptual: **Power → Reaction → Choice → Consequence → Memory**.

Este documento define el siguiente experimento de Nexus. No implementa Persistence01 ni congela la arquitectura final de persistencia.

## Hipótesis y alcance

Una consecuencia no debe desaparecer porque el jugador se alejó, recargó la escena o reinició una pequeña sesión experimental. La sensación buscada es: “Esto sigue así porque antes hice —o no hice— algo”. La memoria debe tener superficie: una variable guardada que no cambia nada visible no cuenta.

Crear en el futuro una escena independiente `Persistence01`, posiblemente una versión simplificada de Triage, reutilizando Feel/Reactivity/Triage conceptualmente. Probar únicamente memoria factual mínima, por ejemplo `RescueOutcome` y `DepotOutcome`, sin construir una ciudad persistente, campaña o arquitectura final de save games.

## Resultados propuestos

### Resultado A — priorizar rescate

El civil queda protegido, pero el depósito queda dañado o invadido. Al salir, recargar y volver, el depósito conserva una huella visible: franjas dañadas, props reorganizados, una barrera rota o una zona acordonada.

### Resultado B — priorizar contención

La amenaza queda contenida, pero el área de rescate conserva la huella del accidente: marca de impacto, carga desplazada, zona dañada o ausencia/presencia distinta del civil. Al volver, esa zona sigue diferente.

Durante la implementación pueden simplificarse los ejemplos, pero A y B deben reconstruir estados perceptiblemente distintos. No alcanza con guardar `choice = A` si ambos mundos se ven iguales.

## Persistencia técnica mínima

El futuro spike debe demostrar: jugar una decisión → producir una consecuencia → guardar un registro mínimo → recargar/reabrir → reconstruir la consecuencia → observarla nuevamente. La primera opción técnica será un JSON local pequeño en `Application.persistentDataPath`, o un mecanismo Unity equivalente si resulta más verificable. Debe sobrevivir a la recarga y, si resulta barato, a cerrar y reabrir Play Mode.

Marcar cualquier mecanismo como **EXPERIMENTAL / LOCAL TO PERSISTENCE01**. No crear `SaveSystem` global, slots, cloud saves, perfiles, migraciones complejas, cifrado, autosave framework ni integración con `Nexus.Server`.

## Reset y clear memory

Reset normal de escena ≠ borrar memoria. El reset de escena puede reconstruir la escena para continuar validando el registro y no debe borrarlo accidentalmente. Debe existir una operación explícita **RESET EXPERIMENT / CLEAR MEMORY** que elimine el registro local y permita repetir desde cero. Ambos caminos se documentarán y probarán por separado.

## Causalidad y retorno

Debe ser posible asociar acción anterior → outcome registrado → modificación posterior. No usar consecuencias aleatorias ni una cutscene que simplemente anuncie “elegiste A”. Texto/debug puede apoyar desarrollo, pero la evidencia principal debe ser espacial, física o comportamental. El retorno puede ser recarga de escena, salir y volver o una transición breve; no hace falta simular días.

## Prueba humana futura

- **RUN 1:** priorizar A, observar resultado, salir/recargar/volver y observar la huella.
- **CLEAR MEMORY:** usar la operación explícita de limpieza.
- **RUN 2:** priorizar B, salir/recargar/volver y comparar la huella.

Preguntas: ¿recordaste qué decisión tomaste?, ¿reconociste la consecuencia sin depender solo del texto?, ¿sentiste que el lugar conservaba historia?, ¿la huella hizo que la decisión pareciera más importante?, ¿quisiste probar la otra prioridad? Pregunta fundamental: **¿el mundo parece recordar algo que hice?**

## Criterios técnicos futuros

Persistence01 estará lista cuando una decisión produzca un outcome; el outcome sobreviva a la recarga; el escenario reconstruya una huella visible; A y B produzcan estados distintos; `CLEAR MEMORY` funcione; Feel, Reactivity y Triage sigan intactos; los tests pasen; y la Console no tenga errores reales.

## Fuera de alcance y stop conditions

No implementar reputación, morality meter, noticias, prensa, rumores, relaciones, facciones, Nemesis, quests, campaña ramificada, slots, cloud save, perfiles, persistencia de ciudad, miles de entidades, memoria autobiográfica de NPCs, Cognitive Society, LLM NPCs, tráfico, crowds, economía, policía, streaming, `Nexus.Server` ni ECS integration.

Detenerse si la prueba requiere arquitectura global, más de dos outcomes, una ciudad/sistema social, integración .NET/ECS/Server, o si una consecuencia no puede reconstruirse visible y causalmente después de recargar.

**No implementar nada todavía.**
