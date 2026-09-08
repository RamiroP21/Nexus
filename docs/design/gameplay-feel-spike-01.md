# Gameplay Feel Spike 01 — Kinetic Vector

## Misión e hipótesis

¿Mover un superhumano y utilizar una fuerza cinética como herramienta física general resulta divertido incluso dentro de un greybox sin ciudad, historia ni sistemas RPG?

Fantasía: “My power is a physical force I can use creatively, not a damage button.”

Este documento autoriza diseño, no inicia implementación. La siguiente misión debe acordar presupuesto, preservar [AGENTS.md](../../AGENTS.md) y partir del [preflight](../integration/unity-preflight.md). El código experimental puede descartarse; el aprendizaje no.

## Alcance y orden de implementación

Máximo: un personaje, una arena greybox, un enemigo/dummy simple, un poder cinético y objetos físicos simples; keyboard/mouse y gamepad desde el primer recorrido. Sin conexión con Nexus.Server ni dependencias del kernel .NET. La física del prototype no se declara determinista ni redefine los contratos de Nexus.

1. Escena pequeña y cámara de tercera persona; acciones semánticas y movimiento básico en ambos dispositivos.
2. Aceleración/deceleración, sprint, salto, gravedad, air control, caída y aterrizaje; ajustar antes de añadir sistemas.
3. Aplicar una fuerza cinética a dummy y objetos; comprobar interrupción/reposicionamiento y respuesta a masas diferentes.
4. Probar una variante de impulso propio para traversal/recovery; conservarla solo si movimiento y poder se enriquecen mutuamente.
5. Iterar cámara, impacto y control. Mantle básico es opcional y se corta si requiere un subsistema de traversal.

No implementar wallrunning, climbing avanzado, barras, parkour complejo, stealth, motion matching completo, campaña, ciudad, NPC society, reputación, progression, inventory ni quests. Tampoco framework VFX, arquitectura de persistencia o integración con la simulación autoritativa.

## Input y cámara

Unity Input System como baseline previsto. Un mapa Gameplay con Move (Vector2), Look (Vector2), Jump, Sprint, PrimaryPower y Pause; Secondary/Modifier solo si un experimento lo necesita. Separar acciones de bindings: nada de teclas hardcodeadas en movimiento/poder.

Dos control schemes: keyboard/mouse y gamepad. Diferenciar delta de mouse de velocidad de stick y sus sensibilidades; deadzones para sticks, no mouse. Prever rebinding con overrides, hot-plug y cambio del dispositivo activo; glyphs dinámicos y UI de remapping quedan fuera. Pause debe liberar/capturar cursor correctamente según dispositivo; probar desconexión sin movimiento atascado.

La cámara debe permitir juzgar distancias, salto, contactos e impulso sin luchar contra el jugador. Empezar con seguimiento sencillo; limitar respuesta de impacto para no ocultar trayectoria ni control.

## Poder y arena

Kinetic Vector es fuerza, no un proyectil de daño. En combate debe empujar/interrumpir/reposicionar; en entorno mover objetos con respuestas distintas por masa; en traversal explorar autoimpulso o alternativa equivalente. La forma exacta (pulso, dirección, radio) es experimental: elegir una sola inicialmente y comparar variantes de una en una.

Arena compacta: piso abierto para acelerar/frenar, rampas y desniveles, plataformas y ledges de alturas diferentes, muros, cajas ligeras/pesadas y verticalidad moderada. Dejar rutas y objetos que inviten a usos no enseñados del poder. Exponer esquinas, contactos y aterrizajes problemáticos; no ocultarlos con geometría indulgente. Dummy inicialmente sin IA compleja.

Inspiración de traversal: continuity, believable contacts, momentum preservation, anticipation, impact absorption y recovery; no construir un sistema avanzado para perseguirlas todas.

## Live tuning

Settings are an API for iteration. Exponer parámetros agrupados en Inspector o un asset pequeño de settings, con unidades, rangos seguros y valores iniciales explícitos. Cambiar una variable por comparación y registrar los valores realmente probados; no prescribir cifras como si ya estuvieran validadas.

- Movement: MoveSpeed, SprintSpeed, Acceleration, Deceleration, Gravity, JumpHeight, AirControl.
- Power: Force, Radius, Falloff, Cooldown y SelfImpulse si se conserva.
- Camera: Distance, FOV, sensibilidades separadas, Follow/Lag e Impact response.

El prototype puede ser feo. Priorizar legibilidad, response, weight, momentum, impact, camera y control. VFX mínimos, sin assets de terceros necesarios para evaluar la hipótesis.

## Validación y cierre

Implement → Play → Capture/Observe → Identify specific weakness → Tune → Play again.

Technical Complete != Experiential Complete. Compilar, tener acciones conectadas y ejecutar la escena sin errores demuestra funcionamiento, no diversión. Hacer recorridos comparables con ambos dispositivos: desplazarse libremente, frenar/girar, saltar/aterrizar, recorrer desniveles, impulsar cajas/dummy e intentar combinar poder y movimiento.

Observar: ¿moverse sin objetivos resulta agradable?, ¿la cámara ayuda?, ¿salto y aterrizaje comunican peso/masa?, ¿el poder produce impacto?, ¿surgen usos no explicitados?, ¿poder y movimiento interactúan?, ¿se quiere seguir jugando al terminar? Registrar situaciones concretas, feedback y capturas, también resultados negativos; no inventar porcentajes con muestras pequeñas.

Detener expansión ante presupuesto agotado, necesidad de cambiar arquitectura central, IA/traversal complejos o tooling ajeno al experimento. Cierre: evidencias técnicas y jugables separadas, límites conocidos y decisión continuar/ajustar/descartar. La futura misión debe crear `docs/design/gameplay-feel-findings.md` con settings probados, decisiones, debilidades y conclusiones; no crearlo ahora con resultados imaginados.
