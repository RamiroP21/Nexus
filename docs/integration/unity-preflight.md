# Unity Preflight 00

Inspección: 2026-09-08, `main` limpia sobre `a0ab727b9dc08186552c3faf906346cd77e669f4`, coincidente con origin. Presupuesto aproximado: seis minutos, sin subagentes. Resultado: decisiones y brief preparados; proyecto oficial pendiente, no validado.

## Entorno y decisiones provisionales

- Hub encontrado: `C:\Program Files\Unity Hub\Unity Hub.exe`.
- Editors encontrados bajo `C:\Program Files\Unity\Hub\Editor\`: `2022.3.56f1`, `2022.3.62f1`, `6000.1.4f1`, `6000.3.2f1`, `6000.4.5f1`.
- Selección provisional: `6000.3.2f1` (revisión `a9779f353c9b`), rama [Unity 6.3 LTS](https://unity.com/blog/unity-6-3-lts-is-now-available), ya instalada con Windows Standalone Support. Ejecutable: `C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe`. No se afirma que este parche sea el más reciente; no fija producción.
- Pipeline provisional: URP. La [plantilla oficial Universal 3D](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/creating-a-new-project-with-urp.html) permite configuración inicial reproducible; ofrece una base suficiente para impacto/VFX mínimos sin el setup adicional de HDRP. Es una decisión de iteración, no una comparación de rendimiento medida. HDRP queda para un bakeoff separado.

## Creación e Input

Se ensayó creación CLI con `-batchmode -nographics -quit -createProject`, `-cloneFromTemplate` y `-buildTarget Win64`, usando la plantilla local `com.unity.template.3d-cross-platform-17.0.14.tgz`. La licencia resolvió entitlement y Unity importó/compiló la plantilla: el log termina con `Exiting batchmode successfully now!` y retorno 0. El proceso ya había terminado al cerrar esta rama opcional; no fue necesario terminarlo por fuerza. No se ejecutó la escena en Play Mode ni un player, y no se instaló un Editor.

El ensayo está fuera del repo, en `C:\Users\Usuario\AppData\Local\Temp\nexus-unity-preflight-6c0abddae03945a1b78c0d2e432c23a0`; `editor.log` conserva evidencia local. No tratarlo como proyecto oficial. El Editor añadió dependencias de Navigation, Multiplayer Center, Visual Scripting y otras que exceden el mínimo previsto. Se observaron mensajes de fallback/dependencias de shaders durante la importación y `Curl error 42` al salir; aunque termina con retorno 0, no se afirma una Console limpia ni validación visual.

`client/Nexus.Unity/` NO fue creado en Git: reducir la plantilla, revisar referencias y revalidar excedería el presupuesto restante. En la próxima misión, crear allí una base mínima mediante Editor/CLI, reduciendo paquetes opcionales antes de validar; no copiar Library ni asumir que el ensayo demuestra runtime correcto. Mantener solo Assets con sus `.meta`, Packages (manifest/lock) y ProjectSettings pertinentes. Añadir ignores específicos para Library, Temp, Logs, obj, UserSettings, builds y soluciones/proyectos generados, sin ignorar `Nexus.sln` raíz ni fuentes/meta.

Paquetes fundamentales previstos: URP, Input System y Unity Test Framework; el Editor resolvió en el ensayo `17.3.0`, `1.17.0` y `1.6.0` respectivamente. Son evidencia de resolución, no un lock validado de Nexus. No se necesita Navigation, networking, Cinemachine, VFX Graph ni Visual Scripting para este preflight.

[Input System](https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.inputsystem.html) es el baseline previsto, aún no configurado en Nexus. Crear acciones semánticas Move, Look, Jump, Sprint, PrimaryPower y Pause; Secondary/Modifier solo si se necesita. Keyboard/mouse y gamepad separados, sensibilidad por dispositivo, deadzones de sticks, hot-plug y preparación para overrides de remapping; UI de remapping y glyphs dinámicos después. No reutilizar sin revisar los bindings de la plantilla.

## Acceso agentic y siguientes comprobaciones

Las guías Unity Workbench orientaron inspección y niveles de evidencia; no son un puente MCP. No hay herramientas Unity MCP disponibles en esta sesión ni configuración de servidor con nombre Unity detectada en Codex. Sí existe `C:\Users\Usuario\.unity\relay\relay_win.exe`: su presencia no prueba conexión, aprobación ni entitlement.

El [MCP oficial de Unity](https://unity.com/blog/unity-ai-mcp-how-to-get-started) usa AI Assistant, Unity 6, proyecto conectado a Unity Cloud y acceso trial/suscripción según la documentación consultada. Los clientes externos necesitan aprobación en `Edit > Project Settings > AI > Unity MCP`; comprobar requisitos vigentes antes de habilitarlo. No se instaló/configuró un bridge, no se entregaron credenciales y no se añadieron wrappers de terceros. MCP es opcional: CLI y pequeños métodos Editor pueden construir/verificar; pantalla para comprobar el resultado visual.

Antes de gameplay: terminar creación mínima, confirmar URP y Input System activos, escena vacía con cámara/luz en Build Settings, target Windows x64, importación/compilación terminada y smoke en Play Mode sin errores. Registrar por separado la validación visual y un player Windows cuando se ejecute. Estructura prevista: `Assets/Nexus/Experimental` (escena del spike), `Input` y `Settings`; conservar pocos directorios.

El prototype está aislado del kernel .NET: sin cambios Core/ECS/Simulation, sin Nexus.Server ni contrato de determinismo para física Unity. No inicia gameplay; el siguiente alcance está en [Gameplay Feel Spike 01](../design/gameplay-feel-spike-01.md). Evaluar después feel, renderer de producción, MCP autorizado y cualquier integración; no añadirlos para completar este preflight. No se repitieron los 211 tests .NET porque ninguna fuente/configuración .NET cambió.
