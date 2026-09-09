# Triage Spike 01 — Choice Under Pressure

**EXPERIENTIAL VALIDATION: PENDING HUMAN PLAYTEST**

## Pregunta central

¿Tener dos problemas simultáneos que realmente no pueden resolverse perfectamente hace que las decisiones del jugador sean significativas?

El spike prueba:

**Power → Reaction → Choice under pressure → Immediate consequence**

No prueba todavía persistencia de largo plazo.

## Principio de diseño

El jugador es poderoso. El conflicto no debe surgir porque su poder deje de funcionar. Debe surgir porque existen necesidades simultáneas, separadas espacial o temporalmente, que requieren atención real; actuar en una consume tiempo y afecta a la otra.

No construir una falsa elección mediante un menú “A o B”. La prioridad debe emerger jugando.

## Escenario

Un único microespacio jugable reutiliza el player, movement, camera, Kinetic Vector, objetos reactivos, civiles y amenaza simple de Feel01/Reactivity01.

Se proponen dos crisis simultáneas, como punto de partida y no como solución cerrada:

### Crisis A — Civil en peligro inmediato

Un civil queda expuesto a un objeto reactivo o a una trayectoria peligrosa. Su estado progresa mientras el jugador está lejos: peligro visible, sobresalto, desplazamiento o huida. Intervenir puede apartar el objeto, contener la fuente o abrir una ruta de escape.

### Crisis B — Amenaza activa en otra zona

La amenaza ocupa otra parte del espacio y sigue avanzando, golpeando objetos o empeorando una situación local. Ignorarla permite que gane posición, desplace cuerpos o aumente la presión visible sobre esa zona.

La separación debe obligar a desplazarse y consumir tiempo, sin convertir el escenario en una ciudad ni esconder la información necesaria.

El jugador puede ir primero a A, ir primero a B, alternar entre ambas o intentar una solución creativa. No debería poder resolver ambas perfectamente sin coste.

## Causalidad y presión

No hacer que una crisis falle arbitrariamente porque el jugador eligió la otra. Toda progresión debe tener causa observable:

- ignorar la amenaza → continúa actuando → gana posición o empeora el área B;
- ignorar el rescate → el peligro progresa → el civil huye, queda desplazado o pierde su ventana de seguridad;
- intervenir parcialmente → mejora una condición pero deja una consecuencia concreta en la otra;
- volver al lugar descuidado → permite entender retrospectivamente: “esto ocurrió porque estuve ocupado con aquello”.

Usar el mecanismo mínimo necesario: estados de crisis, distancia física, ventanas de intervención y progresión temporal local. Evitar un gran reloj arcade si la presión puede percibirse a través del mundo.

## Consecuencias inmediatas

Solo consecuencias locales y visibles para este spike:

- civil salvado o no salvado;
- amenaza contenida o no contenida;
- objeto o área dañada/desplazada;
- civil que huye;
- situación que empeora;
- amenaza que gana posición.

No implementar memoria persistente, reputación, noticias, policía, facciones ni guardado histórico. Esas capas pertenecen a Persistence y fases posteriores.

## No diseñar una solución única

No convertir el spike en un puzzle con respuesta correcta. Debe ser válido que el jugador diga:

- “Intenté salvar A y dejé que B empeorara.”
- “Fui por B primero y A tuvo consecuencias.”
- “Encontré una forma parcial de atender las dos.”

La creatividad sistémica es deseable. El experimento observa qué prioriza Ramiro, no si reproduce una secuencia prescrita.

## Surface rule

No invisible simulation without visible consequence. Todo progreso de crisis debe tener una señal perceptible mediante movimiento, cambio de estado, sonido provisional, feedback visual o modificación espacial. Debug puede ayudar durante desarrollo, pero la experiencia debe ser legible sin números de depuración.

## Criterios experienciales

El futuro playtest debe responder:

1. ¿Noté rápidamente que había más de un problema?
2. ¿Sentí que no podía atender perfectamente todo?
3. ¿Tomé una prioridad jugando y no mediante un menú?
4. ¿Entendí qué ocurrió en el problema que descuidé?
5. ¿Sentí responsabilidad por el resultado?
6. ¿Intenté improvisar una tercera solución?
7. ¿Quise volver a jugar para probar otra prioridad?

Pregunta especialmente importante: ¿el jugador piensa después “¿qué habría pasado si hubiera ido primero al otro lado?”?

## Fuera de alcance

No implementar Persistence Spike, save/load, reputación, morality meter, branching narrative, quests completas, dialogue system, Cognitive Society, LLM NPCs, crowd simulation, traffic, city simulation, police system, factions, economy, Nemesis, advanced combat, progression, inventory, production art, Nexus.Server ni ECS integration.

## Definition of Done futura

### Técnica

- escena jugable independiente;
- dos crisis simultáneas;
- ambas progresan sin esperar al jugador;
- intervenir altera su resultado;
- ignorarlas produce consecuencias observables;
- Feel y Reactivity siguen funcionando;
- tests técnicos apropiados;
- Play Mode limpio.

### Experiencial

**PENDING HUMAN PLAYTEST.** Codex no decide si el dilema es divertido o emocional.

## Preservación y siguiente decisión

Este documento define el experimento y no lo implementa. Reactivity 01 queda aceptada como prototipo; sus deudas conocidas permanecen no bloqueantes. El próximo trabajo requiere una misión explícita de implementación, evidencia técnica y playtest humano antes de avanzar a Persistence.
