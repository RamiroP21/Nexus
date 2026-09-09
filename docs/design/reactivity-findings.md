# Reactivity Spike 01 — V1

**EXPERIENTIAL VALIDATION: PASS FOR PROTOTYPE**

Fecha: 2026-09-09. Baseline: `662544835ca89eeafb6301123cd7cbe1d7a55568`, `main` limpia y publicada. Unity 6000.3.2f1 / URP existentes. Esta misión autoriza solo Reactivity V1; no Triage, persistencia ni integración con el kernel .NET.

## Misión

- Objective: probar si reacciones locales y visibles enriquecen Kinetic Vector en un microespacio urbano.
- Constraints / invariants: preservar GameplayFeel01, controller, cámara, Input System, tuning y física del poder. Sin paquetes, assets externos, IA general ni sistemas posteriores.
- Evidence: baseline de 9 tests Unity; recorridos de Reactivity con física real y dispositivos sintéticos; capturas URP revisadas en dos pasadas; regresión final y revisión Git antes de publicar.
- Budget: sin límite numérico impuesto; sin subagentes. Dos estrategias como máximo ante un blocker persistente; no perseguir polish.
- Stop conditions: dirty work inesperado, regresión de Feel no resuelta, necesidad de .NET, terceros, navegación compleja, framework o persistencia.
- Definition of Done: escena inmediatamente jugable, respuestas diferenciadas, cadena física observada, tests técnicos verdes, Console revisada y entrega Git verificada. La evaluación experiencial pertenece al playtest humano.

## Abrir y repetir

Abrir `client/Nexus.Unity` con Unity 6000.3.2f1. Usar **Nexus > Reactivity 01 > Open gameplay scene**, pulsar Play y enfocar Game View. Escena: `Assets/Nexus/Experimental/Reactivity01.unity`. Feel01 conserva su escena y su posición inicial en Build Settings; Reactivity está añadida, no la reemplaza.

Controles sin cambios: WASD/left stick, mouse/right stick, Space/South para saltar, Shift/L3 para sprint, left mouse/RT para Kinetic Vector, Escape/Start para pausa. Jump durante pausa o el botón Reset arena devuelve jugador, cuerpos y estados reactivos a sus condiciones iniciales.

Recorrido sugerido:

1. Comparar cajas LIGHT (2 kg) y HEAVY (12 kg).
2. Disparar cerca de un civil azul: amarillo con sobresalto, naranja mientras huye, azul al parar.
3. Golpear directamente la amenaza roja: se desplaza y permanece violeta durante recuperación; después vuelve a avanzar.
4. En el lateral oeste, impulsar CHAIN LIGHT hacia CHAIN HEAVY. El civil cercano al segundo objeto está fuera del radio del pulso inicial; el contacto físico puede provocar su reacción.
5. Moverse entre los tres actores, repetir desde otros ángulos y resetear. La cobertura desde el jugador sigue bloqueando disparos, aunque la cámara vea al blanco.

## Arquitectura experimental mínima

Una plaza de 24 × 28 m contiene cuatro cajas, dos civiles y una amenaza. Primitives y materiales existentes de Feel01; sin arte externa. Se reutilizan el rig de jugador/cámara, settings de Feel, acciones, recoil, sonido provisional y línea de impulso.

- `KineticPulse.Impact`: notificación opcional del impacto real, con punto, impulso y Rigidbody alcanzado. Feel01 no tiene suscriptores nuevos.
- `ReactionStage`: referencias explícitas a los tres actores; distribuye KineticImpact, PhysicalImpact y Threat por distancia. No es un bus global ni conserva historia.
- `ImpactRelay`: usa `OnCollisionEnter`, velocidad relativa y un cooldown por cuerpo. No lanza cajas ni agenda una secuencia. Dos cuerpos instrumentados pueden reportar el mismo contacto: el contador de diagnóstico mide callbacks, no colisiones únicas.
- `ReactiveActor`: estados pequeños, Rigidbody de 8 kg, movimiento horizontal en FixedUpdate y evitación local por SweepTest. No hay NavMesh, pathfinding ni memoria.
- `FeelArena.ResetPerformed`: reinicia estados después de restablecer cuerpos y jugador. Las suscripciones se retiran al deshabilitar la escena.
- `ReactionSettings`: asset de tuning propio siguiendo el enfoque existente. No se modificó Feel01Settings.

Civil: **Idle → React → Flee → Idle**. React muestra color amarillo y un breve gesto; Flee orienta y desplaza el cuerpo, cambia a naranja y actualiza la etiqueta. Estímulos durante huida redirigen/prolongan la huida sin reiniciar continuamente el sobresalto.

Amenaza: **Approach → Recover → Approach**. Se acerca hasta una distancia mínima al jugador. Un impacto cinético directo o una colisión suficientemente cercana interrumpe el avance; durante Recover no se cancela el impulso físico. Color violeta y etiqueta distinguen la recuperación. Emite estímulos locales de amenaza a intervalos, sin daño ni combate.

Los impactos muestran un marcador transitorio además de la línea/sonido existentes. Los cambios de estado tienen color, etiqueta y/o movimiento; no hay una simulación social oculta.

## Parámetros V1

Editar `Assets/Nexus/Experimental/Reactivity01/Data/ReactionSettings.asset`. Cambios a assets durante Play pueden persistir: registrar/restaurar tuning al comparar.

| Parámetro | Valor |
|---|---|
| Radio civil, poder/colisión | 5 m, distancia 3D |
| Sobresalto | 0.25 s |
| Velocidad/duración de huida | 3.5 m/s objetivo, 2.4 s |
| Aceleración horizontal de actores | 8 m/s²; el contacto/fricción influye en la velocidad real |
| Amenaza: avance / parada | 1.5 m/s objetivo / 2 m del jugador |
| Recuperación | 1.1 s |
| Amenaza: radio / intervalo | 3 m / 2 s |
| Umbral de colisión / cooldown | 2.5 m/s de velocidad relativa / 0.3 s por cuerpo |
| Masas | Cajas 2 y 12 kg; actores 8 kg |
| Feedback de impacto | 0.25 s |

El poder mantiene el impulso de 36 kg·m/s, cooldown de 0.65 s y recoil existentes. No se añadió targeting experimental: el raycast de cámara y la comprobación de cobertura siguen siendo los de Feel01, incluido su retículo. El soft targeting sin crosshair permanente sigue pendiente y no es requisito para esta prueba de reacción.

## Pasadas y observaciones

Baseline: **9/9 tests pasaron** con apertura, compilación y Play Mode de Foundation/Feel01. Working tree limpio antes de implementar.

Primera pasada Reactivity:

- Se observó sobresalto y huida; el muestreo inicial midió aproximadamente 0.62 m de desplazamiento al comienzo de la huida.
- La amenaza perdió posición y recuperó avance: z inicial 9.11 m, z al recuperar 9.35 m, z posterior 8.22 m. No son medidas de diversión ni de producción.
- Cadena real: dos callbacks físicos, reacción indirecta del civil y unos 0.72 m de desplazamiento muestreado tras comenzar a huir. El pulso inicial estaba fuera de su radio.
- Respuesta de masas: light ≈17.75 m/s y heavy ≈2.84 m/s poco después del mismo impulso, incluyendo contacto/fricción.
- La suite inicial falló: tres fallos por logs de observaciones que el test debía declarar esperados; otro porque la trayectoria desde el jugador estaba cubierta por una caja pesada. Se corrigió el registro del test y se colocó el recorrido directo con línea libre, sin quitar la cobertura del poder.
- Las capturas mostraron el muro de entrada tapando la parte inferior del encuadre inicial. Se movió el spawn de z=-10 a z=-7, manteniendo la cámara y la física existentes.

Segunda pasada: **12/13 tests pasaron**; los nueve anteriores no sufrieron regresiones. Se revisó visualmente el encuadre inicial corregido, sobresalto/huida y recuperación. El fallo restante reveló un problema de reset: el Rigidbody del civil estaba en z=5, pero el Transform interpolado conservaba z≈12. La percepción, que consultaba el Transform, descartaba el impacto cercano.

Corrección acotada: Reactivity consulta la posición del Rigidbody para percepción y despierta sus actores después del reset para refrescar la interpolación. La repetición dirigida pasó **4/4**: distancia al impacto 0.40 m, desfase visual 0.000 m y estado React. Se conserva el test de reset/impacto como regresión; no se cambió la física ni el controller de Feel01.

Cierre completo de regresión: **13/13 tests pasaron, 0 fallos, 0 omitidos, salida 0** (`artifacts/reactivity01/final.xml` y `final.log`). La última ejecución incluye emisión automática de Threat por el enemigo al acercarse al civil, además de impactos directos/adyacentes, umbrales, distancia, huida, recuperación, pausa/reset, movimiento/cámara y diferencias de masa. Se revisaron las capturas finales de amenaza, recuperación y cadena. No se observaron NaN, física explosiva, referencias rotas ni exceptions en estos recorridos.

**TECHNICAL VALIDATION: PASS**. Tres pasadas jugables de Reactivity (la segunda incluyó regresión completa), un diagnóstico dirigido del reset y una regresión final completa. No se declara cobertura exhaustiva de todas las configuraciones físicas.

## Human Playtest / Decision

Feedback confirmado por Ramiro:

> “Las reacciones se entienden y hacen que el mundo resulte más interesante; esto alcanza para seguir.”

Decisión formal:

- **TECHNICAL VALIDATION: PASS**
- **HUMAN EXPERIENTIAL VALIDATION: PASS FOR PROTOTYPE**
- **STATUS: ACCEPTED TO CONTINUE**

Reactivity queda aceptada como prototipo experimental. No es production-ready y no se inicia Reactivity V2. Se mantienen como deuda no bloqueante la oclusión cuando se alinean varios cuerpos, el targeting aún básico, el arte/animaciones/VFX provisionales, el mando físico no probado y el Windows player no probado.

Console: sin errores C# ni errores runtime del experimento. El log conserva mensajes conocidos del entorno: actualización de token de licencia no disponible (el entitlement permite ejecutar), búsqueda de extensiones nativas y dependencias AddPass/BaseMapGen de plantillas Terrain de URP/ShaderGraph. Son avisos ajenos a los materiales utilizados; no hay Terrain en esta escena ni materiales magenta observados. No se ocultaron ni resolvieron cambiando paquetes.

## Evidencia y límites

Comando desde la raíz, con el directorio de resultados creado y sin otra instancia del mismo proyecto abierta:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe' -batchmode -projectPath "$PWD/client/Nexus.Unity" -buildTarget Win64 -runTests -testPlatform EditMode -testResults "$PWD/artifacts/reactivity01/results.xml" -logFile "$PWD/artifacts/reactivity01/editor.log"
```

No usar `-nographics` ni `-quit`. Los escenarios entran/salen de Play Mode y ejecutan motor/física reales. Evidencia local ignorada: `artifacts/reactivity01/` y `client/Nexus.Unity/Logs/Reactivity01/`. Los tests guardan/restauran sus ajustes temporales y usan dispositivos sintéticos.

Las capturas son renders de cámara URP, no capturas de la interfaz del Editor; no incluyen IMGUI. La Console se revisa mediante el log del Editor y los asserts del runner. No se valida aquí mando físico, audio subjetivo ni player Windows distribuible. No se ejecutan los 211 tests .NET porque esa superficie permanece intacta.

Límites deliberados: percepción radial sin oclusión, steering local sin garantías para laberintos, actores primitivos, etiquetas provisionales, una única marca de impacto visible (el último contacto puede reemplazarla) y targeting de Feel01. Cuando jugador/civil/enemigo se alinean, cuerpos y etiquetas se pueden tapar; cambiar de ángulo revela la respuesta. No se añadió un sistema de transparencia o targeting para resolverlo. No hay health, armas, persistencia ni consecuencias sociales futuras.

## Preguntas para Ramiro

1. ¿Se entiende qué causó el sobresalto: poder directo, caja o amenaza?
2. ¿La pausa antes de huir se percibe o es demasiado breve? ¿La huida es suficientemente clara?
3. ¿La amenaza parece recuperar una intención distinta de la evitación del civil?
4. ¿Puedes provocar la cadena de cajas sin instrucciones paso a paso y repetirla desde otro ángulo?
5. ¿Las reacciones hacen que mires alrededor antes de disparar? ¿Resulta más interesante que Feel01?
6. ¿El targeting actual impide evaluar estas reacciones, o puede seguir esperando?

La siguiente experiencia autorizada es Triage Spike 01; su definición está en [triage-spike-01.md](triage-spike-01.md). No se implementa Triage dentro de este cierre.
