# Especificación del kernel de Fase 0

Este documento define el comportamiento implementado por el kernel determinista de Nexus. Describe el protocolo actual; un cambio intencional en cualquiera de sus representaciones canónicas requiere una nueva versión y la actualización consciente de los vectores congelados.

## Alcance

La Fase 0 ofrece una simulación headless, secuencial y de paso fijo. Incluye tipos base, PRNG, streams aleatorios independientes, hash estable, pipeline ordenado, commands diferidos, events visibles al tick siguiente, hashing global explícito de todo estado capaz de afectar ticks futuros, métricas y una reconstrucción doble ejecutable.

No incluye gameplay, ECS, física, persistencia, snapshots, Event Sourcing, rollback, networking, rendering ni paralelismo.

## Tipos fundamentales

- `SimulationTick` envuelve un `ulong`. Su valor inicial es 0, se compara y ordena por valor, y su avance usa aritmética comprobada.
- `EntityId` envuelve un `ulong`. El valor 0 es inválido/no asignado; los valores distintos de 0 son identidades válidas y estables.
- `DeterministicSeed` envuelve el seed raíz de 64 bits sin imponer valores reservados.

## PRNG

`DeterministicRandom` implementa PCG-XSH-RR 64/32 (PCG32): estado interno de 64 bits, salida de 32 bits y multiplicador `6364136223846793005`. El selector de stream efectivo usa 63 bits; el incremento es `(stream << 1) | 1`. El stream predeterminado es 54.

La inicialización sigue el procedimiento de referencia: estado 0, un avance, suma del seed al estado y un segundo avance. `NextUInt64` concatena dos salidas y coloca la primera en los 32 bits altos. `NextInt` valida que el máximo exclusivo sea mayor que el mínimo y usa rejection sampling incluso cuando los límites abarcan desde `int.MinValue` hasta `int.MaxValue`; como el máximo es exclusivo, ese límite superior no puede ser un resultado. `NextDouble` toma los 53 bits altos de un `NextUInt64` y los multiplica por 2^-53, por lo que devuelve un valor en `[0, 1)`.

El vector congelado para seed 42 y stream 54 comienza con:

```text
A15C02B7 7B47F409 BA1D3330 83D2F293 BFA4784B
CBED606E BFC6A3AD 812FFF6D E61F305A F9384B90
```

## Derivación de seeds y streams

La derivación usa FNV-1a de 64 bits sobre una representación explícita. El offset basis es `14695981039346656037` y el primo es `1099511628211`.

Para una clave se hashean, en orden:

1. el dominio UTF-8 estricto `Nexus.Seed.v1\0` o `Nexus.Stream.v1\0`;
2. el seed raíz como `ulong` little-endian;
3. la longitud en bytes de la clave como `ulong` little-endian;
4. la clave codificada como UTF-8 estricto.

El selector PCG derivado se limita a 63 bits. Seed derivado y selector de stream usan dominios separados.

Dentro del pipeline, cada stream declara además un tipo de scope (`global` o `system`). La clave canónica incluye versión y, para tipo de scope, owner y clave local, su longitud UTF-8 y su contenido. En el scope de sistema, el owner es el ID estable del sistema; en el global es vacío. Dos sistemas pueden usar el mismo nombre local sin compartir estado, y un ID nunca colisiona con el scope global reservado. Solicitar o consumir valores en un stream nunca avanza otro stream.

## Hash estable

`StableHasher64` aplica FNV-1a de 64 bits de manera incremental. Cada valor comienza con un tag de un byte que distingue su tipo. Los tags actuales son: `bool` 01, `byte` 02, `int` 03, `uint` 04, `long` 05, `ulong` 06, `double` 07, `string` 08, `SimulationTick` 09, `EntityId` 0A y `DeterministicSeed` 0B.

Los payloads numéricos usan little-endian. Los valores con signo conservan su patrón binario. Un `double` incorpora sus bits IEEE 754 exactos, incluidos cero con signo y payloads NaN. Un string incorpora longitud en bytes como `ulong` little-endian y luego UTF-8 estricto, sin BOM. La salida canónica son 16 dígitos hexadecimales en mayúsculas. El vector unitario congelado que incorpora todos los tipos es `70822922AEB07F4E`.

## Reloj y fixed timestep

El reloj comienza en tick 0. Para una ejecución de `N` ticks se procesan los ticks 0 a `N - 1` y, al terminar, `CurrentTick` vale `N`. `LogicalTimeSeconds` se deriva como `CurrentTick / (double)TickRate`; `FixedDeltaTimeSeconds` se deriva como `1.0 / TickRate`. No existe un acumulador de tiempo lógico en coma flotante.

`SimulationOptions` exige `TickRate > 0`, recibe un seed y puede fijar `MaxTicks`. `Run()` requiere ese máximo; `Run(tickCount)` permite un tramo explícito, rechaza el overflow del reloj y no puede exceder el máximo configurado.

## Sistemas, orden y frecuencia

Cada `ISimulationSystem` declara un ID no vacío, un `Order` entero y una frecuencia positiva. Al congelar el pipeline:

- se rechazan IDs duplicados;
- se rechazan órdenes duplicados;
- se rechaza la frecuencia cero, incluido el valor default no inicializado;
- los sistemas se ordenan por `Order` ascendente;
- el conjunto deja de admitir registros.

No hay desempate implícito. `Freeze` es idempotente. La ejecución es secuencial en un único thread.

Una frecuencia de intervalo `N` corresponde cuando `CurrentTick % N == 0`; por tanto, tick 0 siempre corresponde. Existen formas convenientes para cada tick, cada 2, cada 10 y cada N ticks, todas representadas por el mismo intervalo positivo.

## Ciclo exacto de un tick

Para el tick actual, el pipeline realiza estas fases:

1. abre el buffer de events; la vista de lectura aún contiene el lote terminado en el tick anterior;
2. recorre los sistemas congelados por orden y ejecuta solo los que corresponden por frecuencia;
3. procesa en FIFO la cantidad de commands que existía al comenzar la fase de commands;
4. cierra el tick de events y publica, como un lote de solo lectura, todos los events emitidos durante sistemas y commands;
5. avanza el reloj exactamente un tick.

Si una fase lanza una excepción, se descartan los events emitidos en el tick incompleto, el pipeline queda marcado como faulted y no puede reanudarse. No hay rollback de mutaciones que ya hayan ocurrido.

## Commands

Un command es una mutación determinista diferida. Los systems lo encolan durante su ejecución y el pipeline lo aplica al final del mismo tick, después de todos los systems, en FIFO. Esto permite evitar cambios estructurales inmediatos sobre datos que otro system todavía podría estar recorriendo.

La fase toma una cantidad inicial: un command que encola otro command no lo ejecuta recursivamente, sino que lo deja para el tick siguiente. Los commands originados por un sistema conservan el contexto de ese sistema, incluido su scope de random. Los commands inyectados externamente usan un contexto externo separado. No hay callbacks concurrentes, dispatch por reflexión ni CQRS general.

Todo command declara un `StableTypeId` no vacío, explícito, único para su esquema semántico y versionado, y contribuye sus campos capaces de cambiar el futuro mediante operaciones tipadas de `StableHasher64`. El ID es parte del protocolo persistente, no el nombre CLR. Si cambia el esquema o la interpretación de un payload, su versión debe avanzar aunque el tipo de implementación conserve el mismo nombre. La contribución debe ser determinista y libre de efectos laterales. La representación no usa reflexión genérica, JSON ni `GetHashCode`.

## Events

Un event emitido durante un tick no es visible para otros systems en ese mismo tick. El lote completo pasa a ser `Events` durante el tick siguiente y mantiene el orden de publicación. Cada cierre reemplaza el lote anterior, incluso por un lote vacío. No existen suscripciones arbitrarias ni un EventBus global.

Cada event sigue el mismo contrato explícito de identidad y payload canónico que un command: `StableTypeId` único y versionado, y contribución determinista, sin efectos laterales, de todos los campos relevantes. Cambiar el esquema exige avanzar la versión del ID.

## Hash global de estado

`SimulationStateHasher` usa el dominio `Nexus.Simulation.State.v2`. Solo representa una frontera de tick completada: el lote de events ya fue cerrado y publicado, el reloj ya avanzó y no existe una fase en ejecución. `ComputeStateHash` rechaza el intento durante un tick abierto o una ejecución en curso; los propios buffers también rechazan su contribución mientras events estén abiertos o commands estén procesándose, incluido el entrypoint estático del hasher. Hashear esos puntos sin una fase y un contador de programa explícitos produciría un estado incompleto.

En una frontera válida, se añade en este orden:

1. versión `Nexus.Simulation.State.v2`;
2. seed raíz;
3. TickRate;
4. CurrentTick;
5. schedule congelado de systems;
6. versión y estado de todos los streams creados;
7. command buffer pendiente;
8. lote publicado del event buffer;
9. contributors registrados en orden explícito.

El schedule incorpora su versión, cantidad y, por cada system en orden de ejecución, índice, `Order`, ID y frecuencia. Así, una configuración capaz de ejecutar ticks futuros de otra manera no comparte hash aunque el estado mutable actual coincida.

Los streams se ordenan por clave canónica con comparación ordinal y aportan su clave, estado PCG y selector de stream. Para cada contributor se añade primero su `Order`, luego su ID estable y finalmente su estado. IDs y órdenes de contributors deben ser únicos. La representación final es hexadecimal canónica en mayúsculas.

El command buffer incorpora dominio y cantidad y recorre la cola FIFO sin mutarla. Cada entrada añade su índice, si usa scope random global, el owner estable de su scope y un digest hijo. Ese digest está enmarcado por un dominio propio y contiene el `StableTypeId` capturado al encolar más el payload canónico del command. Por ello distinguen el hash el tipo, los datos, el orden y el contexto de ejecución; también quedan representados los commands anidados que esperan hasta el tick siguiente.

El event buffer incorpora dominio, identidad del lote publicado, cantidad, índices y un digest hijo por event. Cada digest contiene su dominio, `StableTypeId` capturado al publicar y payload canónico. Solo se representa el lote cerrado que será visible durante el tick siguiente; los events de un tick abierto no pueden confundirse con un estado completo porque el hashing se rechaza en esa fase.

El conjunto de contributors incorpora dominio, cantidad y orden explícito. Cada contributor se calcula en un hasher hijo con dominio, `Order`, ID y payload; el hasher padre incorpora índice y digest. Este framing evita ambigüedades entre límites de particiones aun cuando sus secuencias de valores pudieran parecer iguales.

Por tanto, el estado completo de Fase 0 en una frontera cerrada es: configuración determinista activa (seed y TickRate), reloj lógico, schedule congelado, inventario y estado de streams, commands pendientes con su contexto futuro, events publicados todavía observables y estado autoritativo de todos los contributors registrados.

`MaxTicks` es un horizonte externo de ejecución y no cambia el estado de una frontera ya alcanzada. También quedan fuera del hash elapsed real, velocidad, conteos de ejecución y otras métricas diagnósticas, PID, máquina, rutas y tiempo del sistema operativo. El host mide únicamente alrededor de cada ejecución y construye un `DeterminismReport` después de obtener el resultado.

## Demostración y reconstrucción

El host registra dos sistemas numéricos sin semántica de gameplay:

- un contador cada tick cuya mutación ocurre mediante command FIFO y que observa events del tick anterior;
- un acumulador cada 10 ticks que usa un stream propio y publica un event de muestra.

Los valores predeterminados son seed `123456789`, 60 Hz y 10.000 ticks. La frecuencia cada 10 produce 1.000 ejecuciones porque tick 0 está incluido. Run A y Run B se crean por separado, con nuevas opciones, reloj, buffers, provider, sistemas y contributors. Solo se comparan sus hashes finales.

## Vectores de integración

La suite de determinismo congela el escenario predeterminado completo. Para esta versión del protocolo, su hash es:

```text
988DF50CBA896652
```

Modificar intencionalmente el PRNG, las representaciones canónicas, el scope de streams, el lifecycle, los IDs/órdenes o el estado demostrativo puede cambiarlo. Eso exige revisar la decisión y actualizar conjuntamente esta especificación y la prueba; nunca se debe aceptar el nuevo hash a ciegas.

## Límites actuales

- Los autores de commands, events y contributors deben declarar IDs estables y aportar todos los campos que puedan afectar el futuro. Reutilizar un ID después de cambiar el esquema o dejar fuera un campo autoritativo rompe el contrato; esos errores semánticos no pueden inferirse automáticamente sin introducir la reflexión genérica que el protocolo evita.
- La comparación presupone la misma versión del protocolo y del comportamiento implementado. No existe migración automática entre `StableTypeId` o formatos de estado diferentes.
- El hash cubre todos los contributors registrados y la infraestructura indicada; registrar correctamente cada nueva partición de estado autoritativo sigue siendo una responsabilidad explícita.
- El pipeline conserva internamente sus instancias mutables de reloj, streams y buffers. Expone como lectura el tick actual, el lote publicado y el estado de fase; la inyección externa de commands pasa por una operación controlada.
- No existe serialización, snapshot, Event Sourcing, migración de saves, command log persistente ni compatibilidad entre versiones.
- No hay paralelismo. El orden secuencial actual es la referencia que una implementación futura deberá preservar.
