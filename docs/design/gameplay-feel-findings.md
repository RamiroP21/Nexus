# Gameplay Feel Spike 01 — Kinetic Vector V1

**EXPERIENTIAL VALIDATION: PENDING HUMAN PLAYTEST**

Fecha: 2026-09-08. Baseline: `2d1373ef91e0392a5091ccbea2d4b6b304ca8038`, `main` limpia. Esta misión autorizó una V1 local jugable, con Unity 6000.3.2f1 y URP existentes. Las observaciones siguientes son técnicas y visuales del agente; no son una conclusión sobre diversión, impacto percibido o replayability.

## Misión y límites

- Objective: controlar una cápsula en tercera persona y explorar el mismo impulso cinético sobre objetos, dummy y movimiento propio.
- Constraints: solo Unity experimental; sin paquetes/assets externos, integración .NET, IA compleja, framework, V2 ni Reactivity. Bootstrap e Input Foundation conservados.
- Evidence: baseline de cinco tests; recorridos Play Mode con dispositivos sintéticos, física real de Unity, capturas URP y mediciones de movimiento. Publicación condicionada a tests y revisión final verdes.
- Budget: el usuario no fijó un límite numérico; no se usaron subagentes. Parar ante incompatibilidad, necesidad de framework/terceros/.NET o blocker persistente después de dos estrategias razonables.
- Definition of Done: V1 abrible y jugable, controles y tuning documentados, recorridos comprobados, errores reales resueltos y entrega Git verificada. Validación experiencial separada, pendiente de Ramiro.

## Abrir y jugar

Abrir `client/Nexus.Unity` con el Editor indicado y cargar `Assets/Nexus/Experimental/GameplayFeel01.unity` (también menú **Nexus > Feel 01 > Open gameplay scene**). Pulsar Play y enfocar Game View. Esta escena es ahora la primera de Build Settings; Bootstrap sigue habilitada como segunda escena de diagnóstico.

| Acción | Keyboard / mouse | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Look | Mouse | Right stick |
| Jump | Space | South / A / Cross |
| Sprint, mantener | Left Shift | Left stick press / L3 |
| PrimaryPower, una activación por pulsación | Left mouse | Right trigger |
| Pause / resume | Escape | Start |
| Reset arena durante pausa | Space o botón visible | South |

Pause libera el cursor y congela el tiempo. Perder foco pausa la escena. Escape/Start permite reanudar. El reset devuelve jugador y cuerpos a su origen; una caída por debajo de -8 m también recupera el objeto/jugador afectado. El cooldown corto sigue su reloj de juego al reiniciar.

## Decisiones experimentales

- `CharacterController` con gravedad y velocidad explícitas, aceleración finita, frenado, salto, air control, coyote/buffer breves y orientación de cápsula con visor amarillo frontal. El jugador no es Rigidbody. No hay animaciones.
- Cámara orbital con mouse delta en grados por unidad y stick en grados/segundo, límites de pitch, seguimiento exponencial y SphereCast de obstrucción. Al acercarse a menos de 1.3 m del pivote se oculta la cápsula para mantener visible el centro de apuntado; vuelve al alejarse.
- Power dispara un raycast de cámara y comprueba cobertura desde el origen del pulso sobre el jugador. Aplica `ForceMode.Impulse` en el punto de contacto de un Rigidbody; el mismo impulso produce diferente cambio de velocidad según masa y puede generar torque. No hay daño, modos Combat/Traversal, radio, falloff ni filtro de masa máxima.
- Cada disparo añade al jugador una velocidad opuesta a la dirección de apuntado, incluso sin blanco. El autoimpulso conserva la velocidad aérea previa; solo descarta el pequeño sesgo de adhesión al suelo. Salto y poder recibidos en el mismo update se componen. La velocidad adicional está limitada y tiene disipación configurable.
- Dummy: cápsula Rigidbody de 8 kg, sin IA ni health, con rotaciones X/Z restringidas. Puede desplazarse por impulso.
- Arena de 36 × 44 m: espacio abierto, marcas cada 5 m, rampa de unos 17°, plataforma de 3 m, ocho escalones de 0.25 m, plataforma de 2 m, bloques de salto, paredes y pared específica para obstrucción de cámara. Las etiquetas de masas marcan estaciones iniciales; el retículo identifica el Rigidbody apuntado tras moverlo.
- Feedback: retículo/cooldown mínimo, nombre del blanco, línea breve de pulso, marca breve de aterrizaje y sonido transitorio generado en memoria. Sin contenido importado ni VFX framework.

## Parámetros V1

Todos se editan en `Assets/Nexus/Experimental/Feel01/Data/Feel01Settings.asset`. Los componentes consultan el mismo asset durante Play; los cambios a un ScriptableObject en Play pueden persistir: registrar/restaurar ajustes al comparar.

| Grupo | Valores |
|---|---|
| Movimiento | Move 6 m/s; sprint 10 m/s; acceleration 28 m/s²; deceleration **30 m/s²**; gravity 24 m/s²; jump height 1.8 m; air control 0.35; terminal speed 35 m/s |
| Contactos | Coyote 0.10 s; buffer 0.12 s; giro 16 s⁻¹; cápsula 1.8 m × radio 0.35 m; skin 0.035 m; step 0.30 m; slope limit 48° |
| Cámara | Distancia 5.8 m; FOV 72°; mouse 0.12°/unidad; stick 150°/s; follow sharpness 12 s⁻¹; pitch −55°…75°; pivote 1.55 m; sphere radius 0.22 m |
| Poder | Range 24 m; impulse 36 kg·m/s; self impulse 5 m/s; cooldown 0.65 s; maximum self speed 14 m/s; drag adicional ground 8 m/s² / air 1 m/s² |
| Cuerpos | Light 2 kg; heavy 12 kg; dummy 8 kg; linear damping 0.15; angular damping 0.8; continuous collision detection; interpolación |

## Pasadas y observaciones objetivas

1. La comprobación inicial de autoría detectó referencias vacías al asset de tuning. Se repararon y se corrigió el orden de creación de escena/assets en el método de autoría. No se publicó esa versión.
2. Primera pasada jugable: velocidad a 0.12 s ≈3.73 m/s, frenado desde 6 m/s ≈0.77 m, altura de salto muestreada ≈1.72 m. Light ≈17.32 m/s, heavy ≈2.46 m/s y dummy ≈3.70 m/s poco después del impulso. La diferencia medida incluye contacto/fricción y no debe interpretarse como un cociente ideal exacto de masas.
3. Ajuste de frenado: deceleration 22 → 30 m/s². Segunda pasada: distancia de frenado ≈0.55 m con el mismo recorrido; aceleración y salto conservados. La intención es reducir sobrepaso en plataformas pequeñas, todavía sujeta a evaluación humana.
4. Se corrigieron dos supuestos del recorrido: el trayecto frontal de caída estaba bloqueado por una caja real, por lo que se caminó por el lateral; un disparo muy oblicuo al suelo producía poca componente vertical, por lo que el ensayo de lanzamiento apunta cerca de los pies. Esto no se solucionó inflando el poder ni quitando la caja.
5. Segunda pasada: nueve tests pasaron. Autoimpulso a unos 49° de pitch produjo ≈0.30 m de elevación a 0.15 s; salto + poder alcanzó ≈3.17 m de altura muestreada. Caída de plataforma de 3 m: aterrizaje ≈12 m/s, recuperando grounded estable.
6. La revisión visual encontró la cápsula tapando la vista al comprimirse la cámara junto al muro (distancia ≈0.70 m). Se añadió ocultación cercana de la representación y una comprobación específica. También se añadió cobertura de salto + poder simultáneos y power/pause/reset con gamepad para la pasada final.
7. Pasada final: **9/9 tests, 0 fallos, salida 0**. La captura de cámara junto al muro muestra el centro despejado tras ocultar la cápsula cercana. El frenado se mantuvo en 0.55 m; elevación a 0.15 s ≈0.30 m y ápice muestreado de salto + poder ≈3.11 m. Pasaron también el disparo RT, la composición simultánea de salto/poder, pausa y reset con gamepad. No aparecieron errores C# ni de runtime.

Las capturas muestran plataformas distinguibles, sombras/contactos, cápsula separada del suelo en salto y desplazamiento diferente de las masas. No se observaron materiales magenta en estas capturas. La protección de cámara y la legibilidad se revisaron visualmente además de los asserts; no se atribuye a esta revisión una valoración humana del feel.

## Evidencia y límites de validación

Comando de pruebas desde la raíz (crear antes el directorio de resultados):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe' -batchmode -projectPath "$PWD/client/Nexus.Unity" -buildTarget Win64 -runTests -testPlatform EditMode -testResults "$PWD/artifacts/feel01/results.xml" -logFile "$PWD/artifacts/feel01/editor.log"
```

No usar `-nographics` ni `-quit`. La suite tiene cinco tests Foundation y cuatro escenarios del spike; estos últimos entran/salen de Play Mode y prueban el motor y física reales con acciones sintéticas. El test de Foundation conserva su escena y valida que siga habilitada, en lugar del antiguo requisito de que fuese la única escena.

Evidencia local ignorada: `artifacts/feel01/` (baseline, fallos preservados, resultados y logs por pasada); `client/Nexus.Unity/Logs/Feel01/` (capturas y medidas). Las capturas se obtienen mediante render request URP de la cámara: no incluyen la capa IMGUI ni son capturas del escritorio. No se hizo playtest con mando físico ni se evaluó audio subjetivamente. No se generó un player Windows; la entrega es el proyecto listo para Play en Editor. No se ejecutaron los 211 tests .NET: sus fuentes/configuración permanecieron fuera del cambio.

Los logs conservan el mensaje de actualización de token del servicio de licencia que no impide ejecutar Unity, mensajes de extensiones nativas y avisos de dependencias de shaders Terrain de los paquetes existentes. No representan errores de los scripts del spike; los materiales usados y la escena se renderizan. No hay Terrain en la arena.

## Problemas conocidos y preguntas para Ramiro

- La ocultación cercana del cuerpo es abrupta, sin fade. ¿Permite apuntar junto a paredes sin perder orientación? Probar especialmente esquinas, cámara baja y cambio brusco de yaw.
- ¿El frenado de 0.55 m resulta controlable o demasiado seco? Comparar caminar/sprint, giro de 180° y aterrizaje sobre los bloques.
- ¿Mouse y stick permiten apuntar al dummy y a las cajas sin pelear con la cámara? Comparar sensibilidades, pitch y seguimiento.
- ¿Se percibe con claridad cuál objeto es pesado? ¿La respuesta del dummy comunica reposicionamiento útil aunque no haya animación?
- ¿El recoil ayuda a combinar movimiento y poder, o interrumpe cada disparo? Probar suelo cercano, disparo aéreo, caída y salto + poder. La V1 mantiene un cooldown fijo y un impulso por pulsación.
- ¿Surge algún uso creativo no indicado y dan ganas de seguir experimentando? Registrar la situación concreta, también si la respuesta es negativa.

Mantle es solo candidato de V2. No continuar a V2 ni Reactivity antes de recibir el playtest humano.
