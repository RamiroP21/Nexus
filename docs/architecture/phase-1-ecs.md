# Entity Foundation, Deterministic ECS y Multi-Fidelity Foundation

## Alcance y dependencias

Fase 1 añade `Nexus.ECS` y `Nexus.ECS.Tests`. El ECS contiene entidades genéricas, componentes de datos, queries secuenciales y cambios estructurales diferidos. Depende de `Nexus.Core` para identidades y primitivas canónicas y de `Nexus.Contracts` para aportar estado al kernel y adaptar playback/eventos. No depende de `Nexus.Diagnostics` ni de `Nexus.Simulation`.

El Host compone ECS y Simulation y registra cada `EcsWorld` como `IStateHashContributor`. Simulation mantiene sus dependencias de Fase 0; no necesita conocer sparse sets ni fidelidad. No se crea un proyecto `Nexus.World` ni se introducen sistemas de gameplay.

## EntityId y lifecycle

`EntityId` conserva su representación pública `ulong Value`, comparación y contribución canónica de Fase 0. Se añaden vistas `Index` (32 bits inferiores), `Generation` (32 bits superiores) y construcción explícita mediante `FromParts`. La representación previa y sus vectores congelados no cambian: la semántica adicional pertenece al allocator ECS.

El registry emite slots desde index 1 con generation 1. Index 0 y generation 0 nunca identifican una entidad viva de ECS. Un ID ajeno, no asignado, destruido u obsoleto no supera `IsAlive`. Destruir una identidad viva elimina todos sus componentes, incrementa su generación y libera el slot. Una segunda destrucción devuelve `false`. Cuando la generación alcanza `uint.MaxValue`, destruir retira el slot permanentemente; no hay wrap que reactive una referencia antigua.

Los slots libres forman una pila LIFO: la próxima creación reutiliza el último slot destruido. Agotada la pila, se asigna el siguiente index consecutivo. El allocator utiliza arrays de generaciones, estados y slots libres, sin un objeto heap por entidad. Crear es O(1) amortizado; destruir en el world es O(K), donde K es la cantidad de tipos de componente registrados, con eliminación O(1) por store.

La enumeración de identidades vivas usa index ascendente y un enumerador value type que detecta mutaciones del registry. `EcsWorld.CopyEntities` escribe en un `Span<EntityId>` aportado por el llamador; no asigna un array de resultados y exige espacio para todas las entidades vivas. Enumerar slots cuesta O(S), donde S incluye slots libres y retirados.

Las identidades son locales a su `EcsWorld`: dos worlds pueden emitir el mismo valor numérico. Las referencias que crucen worlds deben conservar también el ID estable de world; el evento de fidelidad y el adapter de playback ya lo hacen.

## Componentes e identidad

Cada componente es un `unmanaged struct` que satisface el contrato estático `IComponent<T>`. Declara `StableId`, identidad semántica única, explícita y versionada, y una contribución `ContributeToHash` que añade campo por campo los datos capaces de afectar el futuro. La restricción impide referencias a objetos gestionados dentro del payload y evita boxing por componente.

No se hashean padding, bytes arbitrarios de memoria, nombres CLR ni resultados de `GetHashCode`. No se usa reflection para descubrir campos ni serialización genérica. Cambiar campos o su interpretación exige versionar el stable ID. Los autores deben mantener el ID constante y aportar todos los datos autoritativos sin efectos laterales; el contrato no puede inferir esa semántica.

El world registra explícitamente sus tipos antes de usarlos y rechaza tipos o stable IDs duplicados. El registro se congela antes de la primera creación, query o hash; `FreezeComponents` permite hacerlo explícitamente. Se registran también stores vacíos porque el conjunto de esquemas disponibles forma parte del estado/configuración.

Cada store recibe un runtime ID compacto de registro. Un lookup por `Type` localiza el store tipado al entrar a una operación o query, sin enumerar el diccionario y sin reflection de campos. Ese ID runtime, el orden de registro y el hash interno del diccionario nunca entran al protocolo canónico. Para hashing, los stores se ordenan una sola vez por stable ID con comparación ordinal.

## Sparse set y capacidad

Cada tipo tiene tres arrays paralelos: sparse index de entidad a posición dense, identidades dense y valores `T[]` contiguos. Sparse usa posición más uno para reservar cero como ausencia. El lookup verifica además la identidad completa, incluida su generación. Un slot reutilizado no hereda componentes de la entidad destruida.

Añadir agrega al final dense y rechaza duplicados. Remover reemplaza el hueco con la última fila, actualiza el índice sparse de esa fila y limpia la cola: swap-back O(1). La estructura es interna al world para que no se puedan eludir sus comprobaciones de lifecycle y fronteras estructurales.

La capacidad inicial es configurable, positiva y limitada por los arrays del runtime. Los arrays crecen geométricamente cuando falta espacio, hasta su límite soportado, sin cambiar identidades ni semántica. El costo de copiar se amortiza; reservar cerca del tamaño esperado reduce crecimiento durante creación masiva. No se encogen automáticamente ni se promete capacidad infinita. Capacidad sobrante y entradas inactivas sin semántica quedan fuera del hash.

El enfoque es SoA entre tipos de componente: `Value[]`, `Delta[]`, `FidelityComponent[]`. Dentro de cada struct sus campos siguen siendo AoS. No hay un grafo de objetos por entidad, archetypes/chunks ni transformación SIMD de campos individuales en esta fase.

`GetComponent` y `TryGetComponent` devuelven copias. `SetComponent` actualiza un valor existente. `HasComponent`, `ComponentCount` y la eliminación de un componente ausente completan la API básica. El acceso mutable por referencia ocurre dentro de callbacks de queries; no se publican refs persistentes al storage.

## Queries, refs y orden

Las queries soportan uno, dos o tres componentes. Reciben una acción struct con callback tipado `Execute`, identidad y referencias a los componentes de la fila. Resuelven los stores una vez y ejecutan un loop de arrays, sin LINQ, closure, enumerador heap ni despacho por interfaz de componente en cada fila.

La primera posición genérica fija el driver: `Query<A,B>` recorre el dense order de A y comprueba presencia/generación en B. No se elige automáticamente el store menor ni se ordena por EntityId. Cambiar el primer componente puede cambiar el orden y, por tanto, el resultado de operaciones sensibles al orden. La complejidad es O(cantidad del primer store), con comprobaciones O(1) en los restantes.

Swap-back modifica el orden físico. La garantía es que la misma secuencia estructural, esquema, estado inicial y código produce el mismo dense order y la misma ejecución. Dos historiales diferentes que llegan al mismo conjunto lógico pueden tener layouts y hashes distintos. Esto es intencional: el layout está hasheado porque puede alterar ticks futuros.

`QueryOrdered` ofrece explícitamente las tres variantes A, A+B y A+B+C cuando el consumidor necesita orden semántico. Ordena por el valor completo de `EntityId` (generation e index), no solo por slot. Copia y ordena las identidades del primer store en un `Span<EntityId>` del llamador y filtra la intersección sin alterar el layout. Exige capacidad para todo el primer store; un buffer insuficiente falla antes de ejecutar callbacks. Cuesta O(N log N) y O(N) espacio reutilizable, frente al O(N) y espacio constante de la ruta dense. No asigna resultados por query una vez reutilizado el scratch y calentado el runtime. El scratch queda prestado exclusivamente durante la llamada: callbacks y queries anidadas no deben acceder al mismo buffer. Ambas rutas mantienen iguales guards y alcance de refs.

La referencia recibida solo es válida durante su callback. No debe escapar ni conservarse para otra fase. Mientras exista una query activa, crear/destruir entidades, añadir/remover componentes y ejecutar playback inmediato lanza una excepción; se pueden actualizar datos o encolar cambios. El guard cubre también queries anidadas y se libera al salir, incluso por excepción. No existe rollback de mutaciones realizadas por un callback antes de fallar.

## Cambios estructurales diferidos

`EcsWorld.Commands` es una cola FIFO especializada: create, destroy, add, remove y transición de fidelidad. Guarda registros compactos; los payloads de add se copian a almacenamiento tipado por componente, sin objeto/boxing por entrada. Crear diferido no devuelve una identidad anticipada ni un ticket en esta fase. Para crear e inicializar entidades en una sola operación, un command de simulación puede hacerlo durante la frontera segura.

Encolar no cambia la visibilidad del world. `Playback` requiere que no haya queries activas y aplica la cola en su orden declarado. La validez de una operación dependiente de estado se comprueba al ejecutarla, por lo que el FIFO puede crear condiciones para la entrada siguiente. Los intentos de aplicar recursivamente o alterar una fase de aplicación activa se rechazan; la publicación en un contexto externo tampoco permite callbacks que muten el mismo world. Si una aplicación falla, el world queda faulted y no puede continuar como si la fase se hubiera completado; no se revierten operaciones anteriores.

La separación con `Simulation.CommandBuffer` protege dos fronteras: Simulation decide la fase/orden entre sistemas y commands de alto nivel, mientras ECS expresa mutaciones tipadas de su almacenamiento. `AsSimulationCommand` devuelve un adapter que drena la cola del world en la fase de commands; el Host lo encola mediante `context.EnqueueCommand`. No se duplica el reloj ni se añade un pipeline ECS paralelo.

El adapter identifica semánticamente el world destino. Sus entradas se conservan y hashean en ese world, que debe estar registrado como contributor; el adapter no copia la cola. El lote aplicado es el pendiente al ejecutar el adapter, de modo que varios sistemas pueden agregar entradas al mismo world antes de la frontera. El FIFO global del kernel continúa vigente y un command de Simulation encolado desde otro command espera al tick siguiente. Ninguna cola estructural se procesa automáticamente si no se invoca playback o se programa su adapter.

## Hash completo de ECS

`EcsWorld` implementa `IStateHashContributor` con ID y orden explícitos. El protocolo `Nexus.ECS.World.v1` incorpora:

- Identidad y orden del world.
- Estado del allocator: slots emitidos, cantidad viva, generación y flag vivo de cada slot, incluidos muertos/retirados, más cantidad y orden de la freelist.
- Inventario de tipos registrados ordenado por stable ID.
- Cada store con su stable ID, cantidad, posición dense, EntityId completo y payload explícito de cada componente.
- Cola estructural pendiente con cantidad, posición FIFO, operación, destino, identidad estable del componente y payload o destino de fidelidad correspondiente.

Las particiones y payloads usan dominios y digests hijos para conservar límites inequívocos. Cambiar el layout, el futuro ID asignable, un payload o el orden pendiente cambia la representación canónica. La capacidad asignada, índices runtime de componentes y métricas diagnósticas no afectan el futuro lógico y no se incluyen.

Un hash ECS exige una frontera inactiva: no hay query ni playback en curso, el world no está faulted y no se está reentrando en hashing. Se impiden mutaciones durante su contribución. La cola puede contener trabajo pendiente: ese trabajo forma parte del hash. Dentro de Simulation rige además la invariancia de Fase 0 de tick cerrado, con commands generales y events publicados todavía visibles incluidos en `Nexus.Simulation.State.v2`.

El hash ECS es una extensión mediante contributors, no una modificación de los tags de `StableHasher64` ni del protocolo global. La demo original de Fase 0 conserva su hash congelado `988DF50CBA896652`. No se añaden persistencia, snapshots, reconstrucción desde hash ni Event Sourcing.

## Base de fidelidad

`SimulationFidelity` ordena cuatro niveles: Statistical = 0, Simplified = 1, Agent = 2 y Full = 3. `FidelityComponent` contiene el nivel validado y aporta ese valor mediante su schema ID estable. Su constructor permite establecer cualquiera de los cuatro niveles iniciales.

Una transición solicitada pasa por la cola estructural y se aplica en playback. Solo se permiten niveles adyacentes en ambos sentidos; saltos, valores indefinidos y solicitudes sin cambio se rechazan. El origen se determina al aplicar la entrada, de modo que Statistical → Simplified → Agent en el mismo FIFO son dos transiciones válidas. La entidad debe estar viva y poseer el componente.

Con contexto de Simulation, cada cambio aplicado publica `FidelityChangedEvent` con world ID, EntityId, origen y destino. Su identidad/payload son explícitos y el evento queda visible durante el tick siguiente conforme al kernel. Sin contexto, playback aplica la transición sin publicar en un bus externo. No hay heurísticas de promoción, cámara, distancia, jugador, IA ni decisiones de qué trabajo corresponde a cada nivel.

La edición genérica de componentes permite inicializar/reemplazar datos explícitamente; las reglas de adyacencia pertenecen a la operación de transición. Los consumidores que busquen esas reglas y la emisión del evento deben solicitar la transición, no sustituir directamente el componente.

## Demo y medición

El modo normal de `Nexus.Host` conserva la demo de Fase 0. `--ecs` ejecuta dos worlds y pipelines nuevos, con 100.000 entidades y 100 ticks por defecto. `--entities` configura una cantidad positiva y `--ticks` exige al menos cinco ticks para completar las fronteras demostradas.

La demo usa valores y deltas enteros neutrales y fidelidad, sin posiciones físicas ni gameplay. Inicializa tres componentes por entidad, comprueba que una transición pendiente cambia el state hash, actualiza los valores con una query de tres componentes y aplica esta secuencia:

1. Tick 0: solicita destruir el 10% por una regla explícita de index y drena el buffer al final del tick.
2. Tick 1: un command restaura la población creando e inicializando entidades; los slots se reutilizan con generation incrementada.
3. Ticks 2, 3 y 4: solicita promociones adyacentes hasta Full, aplicadas en cada frontera.
4. Los ticks restantes actualizan datos; se comprueban población, identidades obsoletas, componentes, todas las fidelidades, cola vacía y hashes reconstruidos.

La corrección no se reduce a igualar hashes. Se verifica cada valor final contra una fórmula independiente del loop: para T ticks, los multiplicadores acumulados son 4T−12 para supervivientes generales, 4T−10 para la primera entidad promovida anticipadamente y 4T−14 para los reemplazos inicializados al final del tick 1. Se validan los deltas, N×T−floor(N/10) filas actualizadas y 3N eventos observados (2N si T=5, pues el último lote aún espera al tick siguiente). La suite adicional de 100.000 entidades compara hashes en cada una de 20 fronteras y verifica payloads/fidelidad de supervivientes y reemplazos, sin assertions de tiempo.

La salida mide creación, acumulado de queries numéricas, simulación incluyendo hash final, bytes asignados en el thread y una diferencia aproximada del heap sin forzar GC. Los cambios estructurales y eventos de transición pueden asignar memoria; no ocurren por entidad en todos los ticks. Las medidas de queries aíslan únicamente el loop numérico y sus guards. La primera ejecución puede incluir calentamiento/JIT y la diferencia de heap está afectada por recolecciones: no son benchmarks científicos ni límites de aceptación. Ninguna métrica controla decisiones simuladas o contribuye al hash.

## Límites de la fase

- Ejecución secuencial y ownership de un thread; no sincronización, jobs ni paralelismo.
- Queries de uno a tres tipos; no lenguaje dinámico, filtros complejos, archetypes ni scheduler adicional.
- No tickets para referenciar una creación diferida antes de aplicarla.
- No rollback de queries o playback fallidos ni reanudación de un world faulted.
- Stable IDs y payloads completos siguen siendo contratos explícitos del autor; no hay validación semántica por reflection.
- La capacidad práctica depende de componentes y memoria disponibles. El diseño usa crecimiento amortizado y mediciones concretas, sin prometer rendimiento para una población no medida.
- Sin física, IA, mundo geográfico, contenido, gameplay, persistencia, rendering ni sistemas de Fase 2.
