# Nexus

Nexus es un proyecto C#/.NET para construir una simulación determinista, headless y modular que pueda sostener en el futuro un videojuego de superhumanos en una ciudad persistente. El repositorio incluye **Fase 0: Deterministic Simulation Kernel** y **Fase 1: Entity Foundation + Deterministic ECS + Multi-Fidelity Foundation**.

El kernel implementa ticks de paso fijo, tipos de identidad y seed, PRNG reproducible con streams independientes, hashing estable, pipeline secuencial con orden y frecuencias explícitos, buffers de commands y events y diagnóstico. La base ECS añade entidades con index/generation, almacenamiento sparse set de componentes contiguos, queries tipadas de uno a tres componentes, cambios estructurales diferidos y niveles de fidelidad con transiciones explícitas. Son entidades y datos genéricos: no hay gameplay implementado.

## Requisitos

- Windows, Linux o macOS con .NET 9 SDK.
- Visual Studio 2022 para el flujo de desarrollo previsto en Windows, o la CLI de .NET 9.

El archivo `global.json` fija el SDK de la solución en la línea 9.0 instalada para esta fase.

## Restaurar, compilar y probar

Desde el directorio que contiene `Nexus.sln`:

```powershell
dotnet restore Nexus.sln
dotnet build Nexus.sln --no-restore
dotnet test Nexus.sln --no-build
```

Los warnings se tratan como errores. No se silencian para conseguir una compilación limpia.

## Ejecutar la simulación headless

```powershell
dotnet run --project src/Nexus.Host/Nexus.Host.csproj
```

Sin argumentos, el host usa seed `123456789`, una frecuencia de 60 ticks por segundo y 10.000 ticks. Construye y ejecuta dos simulaciones completamente independientes, compara sus hashes finales y muestra `DETERMINISM PASS` o `DETERMINISM FAIL`. El tiempo transcurrido y la velocidad frente a tiempo real son métricas externas: nunca forman parte del estado ni de su hash.

La demo técnica ECS es opcional:

```powershell
dotnet run --project src/Nexus.Host/Nexus.Host.csproj -c Release -- --ecs
dotnet run --project src/Nexus.Host/Nexus.Host.csproj -c Release -- --ecs --entities 100000 --ticks 100
```

Sus valores predeterminados son 100.000 entidades y 100 ticks. Reconstruye dos escenarios independientes, actualiza datos mediante queries, destruye el 10% de las entidades en una frontera diferida, reutiliza sus slots con una nueva generación y promueve fidelidad entre niveles adyacentes. Verifica hashes pendientes y finales y muestra tiempos y asignaciones de creación, queries y simulación. Los parámetros requieren entidades positivas y al menos cinco ticks; los tiempos son diagnósticos y no constituyen umbrales de rendimiento.

## Estructura

```text
.gitignore
Nexus.sln
global.json
Directory.Build.props
README.md
src/
  Nexus.Core/          Tipos, PRNG, derivación de seeds y hash estable
  Nexus.Contracts/     Fronteras mínimas del pipeline
  Nexus.ECS/           Lifecycle, sparse sets, queries y fidelidad genérica
  Nexus.Simulation/    Reloj, contexto, buffers, pipeline y hash de estado
  Nexus.Diagnostics/   Reportes sin influencia sobre la simulación
  Nexus.Host/          Composition Root y demostración ejecutable
tests/
  Nexus.Core.Tests/
  Nexus.ECS.Tests/
  Nexus.Simulation.Tests/
  Nexus.Determinism.Tests/
docs/
  vision/
  architecture/
```

## Garantía de determinismo

Para una misma versión, estado inicial, seed, configuración, conjunto ordenado de sistemas y commands, Nexus debe producir exactamente el mismo estado final. En una frontera de tick cerrada, el hash incluye el schedule congelado, el estado de todos los streams aleatorios creados, los commands pendientes con su orden y contexto, el lote de events publicado para el tick siguiente y todos los contributors registrados. Los tipos y payloads de commands y events se representan mediante contratos explícitos y versionados; no se usa reflexión, JSON ni `GetHashCode` para construir el estado canónico. La suite congela secuencias unitarias e incluye una reconstrucción integral para detectar cambios accidentales. Un cambio intencional de protocolo determinista exige actualizar su especificación y los vectores congelados de forma consciente.

El ECS contribuye además generations de slots vivos y muertos, freelist ordenada, tipos registrados, componentes en su orden físico y cambios estructurales pendientes. Las queries recorren el dense order del primer componente solicitado; la misma historia de commands reproduce ese layout. `QueryOrdered` permite ordenar explícitamente por EntityId completo con scratch reutilizable y costo O(N log N), sin modificar el dense layout. Las referencias mutables solo son válidas dentro del callback de query, que impide mutaciones estructurales inmediatas.

Las especificaciones están en [docs/architecture/phase-0-kernel.md](docs/architecture/phase-0-kernel.md) y [docs/architecture/phase-1-ecs.md](docs/architecture/phase-1-ecs.md). Los principios permanentes están en [docs/architecture/principles.md](docs/architecture/principles.md) y la visión de producto en [docs/vision/game-vision.md](docs/vision/game-vision.md).

## Qué no está implementado

Esta fase no implementa gameplay ni infraestructura de fases posteriores: no hay física, combate, IA, navegación, ciudad, tráfico, distritos, facciones, economía, reputación, poderes, progresión, misiones, narrativa, persistencia real, networking, rendering ni assets. La fidelidad no incluye heurísticas de promoción/degradación. Las demostraciones del host son estado numérico deliberadamente neutral, no prototipos de esos sistemas.
