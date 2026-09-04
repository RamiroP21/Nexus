# Principios arquitectónicos

Estas reglas gobiernan la evolución de Nexus. Una excepción exige evidencia, una frontera clara y una decisión documentada.

## Determinismo primero

- La misma versión del código, estado inicial, seed, configuración, sistemas y comandos debe producir exactamente el mismo estado final.
- Todo orden relevante es explícito. Nunca depende de reflexión, registro en un contenedor, enumeración incidental, hash del runtime o timing del sistema operativo.
- No se usa `System.Random`, un generador global ni estado global mutable. Cada consumidor aleatorio obtiene un stream estable derivado de una clave estable.
- El estado determinista no incorpora reloj real, PID, máquina, rutas, timestamps, GUID aleatorios ni otros datos externos.
- El hash se calcula únicamente en fronteras de tick cerradas. Debe representar configuración determinista, reloj lógico, schedule congelado, streams, commands pendientes, events aún visibles y todo estado autoritativo registrado; una fase parcial se rechaza en lugar de fingir un estado completo.
- Commands y events declaran un ID de protocolo explícito y versionado y aportan cada campo relevante con primitivas canónicas. Un cambio de esquema exige avanzar el ID. Ni los nombres CLR, ni reflexión genérica, ni JSON, ni `GetHashCode` definen la identidad determinista.
- Los formatos persistentes y de intercambio futuros serán versionados, pero Fase 0 no introduce snapshots, Event Sourcing ni persistencia anticipada.

## Headless y tiempo fijo

- La simulación autoritativa nace headless. Rendering, audio, entrada y cualquier cliente futuro permanecen desacoplados.
- El tick entero es la fuente de verdad. El tiempo lógico se deriva de `CurrentTick / TickRate`; no se acumulan incrementos de coma flotante.
- El reloj real se reserva para diagnóstico y benchmarking y jamás decide comportamiento simulado.

## Límites y dependencias

- Las capas inferiores ignoran el gameplay y no conocen presentación, contenido ni infraestructura externa.
- No hay Service Locator global ni dependencias ocultas. El contexto de un sistema expone solo capacidades de simulación acotadas.
- Los contratos se agregan cuando protegen una frontera real, no como anticipación especulativa.
- El contenido futuro será data-driven y versionado. Las herramientas y modelos 3D externos estarán completamente desacoplados del core.

## Modelo de ejecución

- Los sistemas declaran identidad, orden y frecuencia estables. Un orden duplicado es un error de configuración.
- La fase secuencial es la referencia determinista. Commands y events cruzan fases mediante buffers de orden estable; no hay callbacks que muten la simulación a mitad de una iteración.
- Los commands se aplican al final del tick en FIFO; los que se encolan durante esa fase esperan al tick siguiente. Los events publicados en un tick forman el lote visible durante el siguiente.
- Un Job System futuro trabajará por batches. Nunca se lanzará `Task.Run` por entidad.
- El ECS futuro se reservará para datos masivos y recorridos donde aporte una ventaja medida. Un Domain Model seguirá siendo preferible cuando exprese mejor reglas e invariantes.

## Rendimiento basado en evidencia

- Se mide antes de optimizar y se mantiene una referencia correcta y reproducible.
- SIMD, estructuras especializadas, paralelismo y niveles de fidelidad se incorporarán solo cuando el profiling lo justifique.
- C++ podrá entrar únicamente detrás de una frontera estrecha cuando el profiling demuestre que C#/.NET no satisface un subsistema crítico.
