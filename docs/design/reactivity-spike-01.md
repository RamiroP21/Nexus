# Reactivity Spike 01 — Kinetic Reaction

> Brief original de preparación. La misión posterior Reactivity 01 / V1 autorizó su implementación el 2026-09-09. Ver [escena, alcance ejecutado y findings](reactivity-findings.md). La valoración experiencial de Reactivity continúa pendiente de playtest humano; no autoriza Triage.

## Pregunta

¿Un mundo que reacciona inmediatamente a las acciones y al poder del jugador hace que Kinetic Vector resulte más interesante?

El spike prueba principalmente **Reaction** dentro de `Power → Reaction → Consequence`. No prueba persistencia de largo plazo.

## Alcance

Reutilizar la escena, player, cámara y Kinetic Vector de Feel 01 en un microespacio urbano/greybox pequeño y legible. Añadir solo lo necesario para observar:

- objetos físicos móviles;
- civiles/dummies reactivos simples;
- una amenaza/enemigo simple;
- superficies y obstáculos que permitan cadenas físicas pequeñas;
- feedback provisional de impacto o sobresalto.

El mismo poder debe producir respuestas diferentes. Un objeto recibe fuerza, se desplaza y puede colisionar. Un civil detecta un poder o impacto cercano, se sobresalta y se aleja o huye con una reacción visible. Un enemigo puede ser desplazado, perder posición y recuperarse. El entorno muestra movimiento, sonido, feedback visual o un cambio espacial perceptible.

## Surface rule

No invisible simulation without visible consequence. Cada reacción debe manifestarse mediante movimiento, una pose simple, cambio de comportamiento, sonido, feedback visual o cambio espacial. No crear variables internas sin superficie observable. No implementar destrucción compleja.

## IA y targeting

Usar estados mínimos explícitos, por ejemplo `Idle → React → Flee/Recover`. No introducir behavior tree, framework general, Cognitive Society, LLM NPCs, memoria persistente, reputación, factions, economía, quests, narrativa profunda, crowd simulation o ciudad completa.

Registrar soft/contextual targeting como candidato. No exigir todavía una solución sofisticada ni un crosshair FPS permanente. Si el targeting bloquea la evaluación, usar la intervención experimental mínima que haga legible el objeto o actor afectado.

## No implementar

Persistencia de largo plazo, integración ECS o Nexus.Server, combate avanzado, health system completo, progression, armas, destrucción sistémica, arquitectura de IA general, nuevos paquetes o assets externos.

## Micro-street

La arena debe permitir identificar qué causó la reacción, quién reaccionó y la diferencia entre objeto, civil y enemigo. Mantener poco ruido visual y rutas cortas que permitan repetir el mismo estímulo. El player y Kinetic Vector existentes deben seguir siendo la base jugable.

## Criterios de éxito

No medir fun mediante tests ni inventar porcentajes con una muestra de una persona. Después del playtest humano, responder:

1. ¿Las reacciones hacen que el poder parezca más significativo?
2. ¿Se entiende qué causó cada reacción?
3. ¿El jugador presta atención a civiles y objetos alrededor?
4. ¿Aparecen cadenas pequeñas de causa y efecto?
5. ¿El mundo parece menos estático que Feel 01?
6. ¿Queremos profundizar esta capa?

## Evidencia y definición de terminado

La futura misión debe producir tests técnicos apropiados, validación Play Mode, observación visual y playtest humano; video solo si ayuda a comparar reacciones. Reactivity Spike 01 estará técnicamente listo cuando la escena abra, el controller y Kinetic Vector sigan funcionando, objeto/civil/enemigo reaccionen de forma diferenciada y visible, no haya errores runtime y exista una escena inmediatamente jugable. La experiencia queda pendiente del playtest humano.

Esta es una preparación documental. No inicia la implementación ni autoriza avanzar a Triage, Persistence o Vertical Slice.
