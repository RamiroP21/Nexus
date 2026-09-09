# Nexus Unity — Foundation 01

Proyecto oficial, aislado del kernel .NET. Foundation y Gameplay Feel Spike 01 V1 autorizados el 2026-09-08. URP y versión de Editor son decisiones provisionales para iterar, no decisiones de producción.

## Jugar Reactivity 01 V1

Menú **Nexus > Reactivity 01 > Open gameplay scene**, o abrir `Assets/Nexus/Experimental/Reactivity01.unity` y pulsar Play. Controles de Feel01 sin cambios. Dos civiles se sobresaltan/huyen y una amenaza se recupera tras el impulso. Comparar cajas de 2/12 kg y probar la cadena física del lateral oeste. Ver [tuning, validación y preguntas para Ramiro](../../docs/design/reactivity-findings.md).

**EXPERIENTIAL VALIDATION: PENDING HUMAN PLAYTEST** para Reactivity. Feel01 ya fue aceptado como prototipo por Ramiro; su escena permanece disponible. No avanzar a Triage sin feedback.

## Jugar Kinetic Vector V1

Abrir `Assets/Nexus/Experimental/GameplayFeel01.unity`, también desde **Nexus > Feel 01 > Open gameplay scene**, pulsar Play y enfocar Game View. WASD/left stick mueve, mouse/right stick mira, Space/South salta, Shift/L3 corre y left mouse/RT dispara. Escape/Start pausa; Jump durante pausa reinicia la arena. Ver [parámetros, evidencia y preguntas de playtest](../../docs/design/gameplay-feel-findings.md).

Feel01: **HUMAN EXPERIENTIAL VALIDATION: PASS FOR PROTOTYPE**; ver la decisión al final de sus findings. No hay integración Nexus.Server/ECS ni assets externos.

## Abrir

Abrir esta carpeta con **Unity 6000.3.2f1** (revisión `a9779f353c9b`) y Windows Standalone Support. Editor usado: `C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe`.

Se partió de Assets/ProjectSettings de la plantilla oficial local `com.unity.template.3d-cross-platform-17.0.14.tgz`, reducida mediante APIs del Editor. No se copió Library ni contenido de terceros. Packages fija URP **17.3.0**, Input System **1.17.0** y Test Framework **1.6.0**, con módulos oficiales y dependencias transitivas en el lock.

`Assets/Nexus/Experimental/Bootstrap.unity` sigue siendo una escena de diagnóstico con Main Camera y Directional Light. El fondo azul oscuro vacío es intencional. GameplayFeel01 conserva la primera posición habilitada; Bootstrap es segunda y Reactivity01 tercera. Usar los menús para elegir experimento antes de Play.

- `Assets/Nexus/Experimental`: Bootstrap, GameplayFeel01/Feel01 y Reactivity01 (runtime experimental, tuning, autoría y tests).
- `Assets/Nexus/Input`: `NexusInput.inputactions`, también asignado como acciones globales del proyecto.
- `Assets/Nexus/Settings/Rendering`: assets URP PC, renderer y perfiles de la plantilla; GUIDs conservados.

Windows: Standalone x64, Mono, Direct3D 11, color Linear, ventana inicial 1280×720. Todas las calidades referencian el mismo pipeline PC; se conserva la nomenclatura de calidad de la plantilla. Input Handling usa únicamente el nuevo Input System. Serialización Force Text; conservar todos los `.meta`.

## Entrada semántica

Mapa `Gameplay`, control schemes `KeyboardMouse` y `Gamepad`:

| Acción | Tipo | Keyboard + mouse | Gamepad |
|---|---|---|---|
| Move | Value / Vector2 | WASD, composite 2D | Left stick |
| Look | Value / Vector2 | Mouse delta | Right stick |
| Jump | Button | Space | South |
| Sprint | Button | Left Shift | Left stick press |
| PrimaryPower | Button | Left mouse | Right trigger |
| Pause | Button | Escape | Start |

Los sticks usan deadzones del Input System. Mouse delta y stick tienen sensibilidades y unidades separadas en Feel01Settings. El controller consume acciones, nunca teclas concretas desde gameplay. Los bindings admiten overrides futuros; remapping UI, glyphs y perfiles quedan fuera de esta V1.

## Validación reproducible

Cerrar otra instancia del mismo proyecto. Desde la raíz del repositorio, en PowerShell:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe' -batchmode -projectPath "$PWD/client/Nexus.Unity" -buildTarget Win64 -runTests -testPlatform EditMode -testResults "$PWD/artifacts/unity-foundation/results.xml" -logFile "$PWD/artifacts/unity-foundation/editor.log"
```

Crear el directorio de resultados si no existe. No usar `-nographics`: se verifica render URP real. No añadir `-quit`: el runner termina el proceso tras completar los tests. También pueden ejecutarse desde Window > General > Test Runner > EditMode: cinco tests Foundation, cuatro escenarios Feel01 y cuatro Reactivity que entran en Play Mode.

La suite comprueba escena/ausencia de missing scripts, Windows/URP/Input backend, acciones/bindings, entrada sintética keyboard/mouse/gamepad con deadzone y desconexión, y dos ciclos de entrada/salida de Play Mode con render de Bootstrap. El test de entrada también entra en Play Mode; aísla dispositivos sintéticos y restaura sus ajustes temporales. La captura real se guarda en `Logs/bootstrap-render.png` y debe verse como fondo azul oscuro vacío.

La reconstrucción requiere solamente Assets con metadata, Packages y ProjectSettings, además del Editor/licencia y acceso a los paquetes oficiales (o caché de paquetes disponible). Library, Temp, Logs, UserSettings, builds y proyectos IDE generados no se versionan.

## Límites

La validación de dispositivos es sintética: no prueba un mando físico ni el feel. La imagen renderizada no es una captura de la interfaz del Editor. La revisión de Console se realiza mediante el log del Editor y el runner. No se ha generado ni ejecutado un player Windows; se verificó su configuración. Los 211 tests .NET no se repiten porque .NET permanece intacto.

El siguiente paso es el playtest humano de [Reactivity V1](../../docs/design/reactivity-findings.md); no iniciar Triage ni sistemas posteriores automáticamente.
