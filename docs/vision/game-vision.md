# Visión de Nexus

Nexus será un videojuego y simulador de superhumanos ambientado en una gran ciudad persistente. La intención es que las acciones tengan consecuencias sistémicas, reproducibles y duraderas, y que el cliente gráfico futuro sea una presentación desacoplada de una simulación autoritativa.

## Visión a largo plazo

La dirección futura contempla:

- mundo abierto, campaña, misiones secundarias y libertad sandbox;
- héroes, antihéroes, villanos, facciones e identidad secreta;
- reputación multidimensional e información imperfecta propagada por prensa, cámaras, testigos y propaganda;
- personajes no jugadores con rutinas, memoria, relaciones y objetivos, además de un sistema Némesis evolutivo y enemigos capaces de adaptarse al estilo del jugador;
- relaciones profundas, crisis simultáneas y consecuencias persistentes, incluidos daños colaterales, reconstrucción y efectos legales, políticos y económicos;
- poderes sistémicos y sus interacciones con el entorno;
- progresión mediante experiencia, niveles, especializaciones, árboles de habilidades y maestría por uso, con posibles condiciones del mundo;
- combate de acción en tiempo real en tercera persona, compañeros con órdenes tácticas, ataques combinados y civiles presentes durante los enfrentamientos;
- una física propia basada en masa, velocidad, impulso, colisiones, destrucción y materiales;
- simulación individual y colectiva, mapas de influencia y reglas de distrito;
- modificadores matemáticos, construcciones de personaje y niveles variables de fidelidad;
- grandes cantidades de entidades y ejecución headless muy superior a tiempo real;
- persistencia, memoria histórica de la ciudad y un cliente gráfico completamente desacoplado;
- un pipeline de contenido 3D externo, sin convertir herramientas de creación de modelos o animaciones en dependencias del gameplay.

Esta lista expresa una dirección de producto, no un compromiso de alcance ni una descripción de funciones existentes. No define todavía trama, personajes, ciudad, poderes ni lore concretos.

## Estado actual: Fase 0

La implementación actual se limita al **kernel de simulación determinista**. Proporciona tipos fundamentales, tiempo lógico por ticks, generación pseudoaleatoria reproducible con streams independientes, hashing estable, orden y frecuencia explícitos de sistemas, buffers deterministas de comandos y eventos, ejecución headless, diagnósticos externos y pruebas de reconstrucción completa.

Los sistemas ejecutados por el host son demostraciones numéricas sin significado de gameplay. Solo prueban las leyes del kernel.

## Fuera del alcance actual

No existen todavía ECS, física, combate, IA, navegación, ciudad, tráfico, distritos, facciones, economía, reputación, poderes, progresión, misiones, narrativa, persistencia real, networking, rendering ni assets. Las fases posteriores deberán construirlos sobre las garantías del kernel sin debilitar el determinismo.
