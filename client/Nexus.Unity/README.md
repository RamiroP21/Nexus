# Nexus Unity — Foundation 01

Proyecto oficial, aislado del kernel .NET. Foundation autorizada el 2026-09-08: no contiene gameplay, greybox, integración con Nexus.Server ni MCP. URP y versión de Editor son decisiones provisionales para iterar, no decisiones de producción.

## Abrir

Abrir esta carpeta con **Unity 6000.3.2f1** (revisión `a9779f353c9b`) y Windows Standalone Support. Editor usado: `C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe`.

Se partió de Assets/ProjectSettings de la plantilla oficial local `com.unity.template.3d-cross-platform-17.0.14.tgz`, reducida mediante APIs del Editor. No se copió Library ni contenido de terceros. Packages fija URP **17.3.0**, Input System **1.17.0** y Test Framework **1.6.0**, con módulos oficiales y dependencias transitivas en el lock.

Abrir `Assets/Nexus/Experimental/Bootstrap.unity`: única escena habilitada en Build Settings, con Main Camera y Directional Light. El fondo azul oscuro vacío es intencional. No existe todavía un personaje ni comportamiento de juego.

- `Assets/Nexus/Experimental`: Bootstrap y tests exclusivos del Editor.
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

Los sticks usan deadzones del Input System. Mouse delta y stick no tienen las mismas unidades: el próximo consumidor debe tratar delta de mouse y velocidad de stick por separado, con sensibilidades propias. No hay controlador, cámara de gameplay ni gestión de pausa/cursor todavía. Consumir acciones, nunca teclas desde gameplay. Los bindings admiten overrides futuros; remapping UI, glyphs y perfiles quedan fuera de esta foundation.

## Validación reproducible

Cerrar otra instancia del mismo proyecto. Desde la raíz del repositorio, en PowerShell:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe' -batchmode -projectPath "$PWD/client/Nexus.Unity" -buildTarget Win64 -runTests -testPlatform EditMode -testResults "$PWD/artifacts/unity-foundation/results.xml" -logFile "$PWD/artifacts/unity-foundation/editor.log"
```

Crear el directorio de resultados si no existe. No usar `-nographics`: se verifica render URP real. No añadir `-quit`: el runner termina el proceso tras completar los tests. También pueden ejecutarse los cinco tests desde Window > General > Test Runner > EditMode.

La suite comprueba escena/ausencia de missing scripts, Windows/URP/Input backend, acciones/bindings, entrada sintética keyboard/mouse/gamepad con deadzone y desconexión, y dos ciclos de entrada/salida de Play Mode con render de Bootstrap. El test de entrada también entra en Play Mode; aísla dispositivos sintéticos y restaura sus ajustes temporales. La captura real se guarda en `Logs/bootstrap-render.png` y debe verse como fondo azul oscuro vacío.

La reconstrucción requiere solamente Assets con metadata, Packages y ProjectSettings, además del Editor/licencia y acceso a los paquetes oficiales (o caché de paquetes disponible). Library, Temp, Logs, UserSettings, builds y proyectos IDE generados no se versionan.

## Límites

La validación de dispositivos es sintética: no prueba un mando físico ni el feel. La imagen renderizada no es una captura de la interfaz del Editor. La revisión de Console se realiza mediante el log del Editor y el runner. No se ha generado ni ejecutado un player Windows; se verificó su configuración. Los 211 tests .NET no se repiten porque .NET permanece intacto.

El siguiente trabajo requiere una misión explícita para [Gameplay Feel Spike 01](../../docs/design/gameplay-feel-spike-01.md).
