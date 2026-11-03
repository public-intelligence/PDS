# Localidades, zonas horarias y calendarios: qué implementamos

**Objetivo:** que cada localidad (en RD o en el exterior) tenga su zona horaria, su horario laboral y sus feriados, para calcular bien los plazos.

---

## Pantallas que ya existían (mejoradas)

### Lista de localidades
- Columna **Ubicación**: "Provincia · Municipio" en RD, "Ciudad, País" en el exterior.
- Columna **Zona horaria** con el desfase actual (ej. `America/New_York (UTC−4)`).
- Columna y filtro **Funciones**, filtro **País** y "Solo las que gestiono".

### Mapa
- Arranca mostrando todas las localidades, incluidas las del exterior.
- El popup muestra país, ciudad, zona horaria y **hora local en vivo**.

### Formulario de localidad
- **País**: todos los países. RD mantiene la cascada; otros países piden Ciudad y Estado/Provincia.
- **Zona horaria** obligatoria, filtrada por país (se preselecciona si el país tiene una sola).
- **Funciones** (Embajada, Consulado…). Regla: "Sección consular" requiere "Embajada".
- Responsables: casilla **Gestiona calendario**.
- Se eliminó "Horarios de excepción". Ahora eso vive en el Calendario laboral.

### Detalle de localidad
- Encabezado con zona horaria, hora local y chips de funciones.
- Badge y acción **Localidad predeterminada de calendario**.
- Bitácora con las nuevas acciones y el canal (PDS, API externa, Sistema).

### Modales suspender / reactivar / cerrar
- Explican el efecto sobre el calendario laboral.

### Responsables
- Vínculo con **Usuario GobID** (solo admins).

---

## Pantallas nuevas

### Calendario laboral (dentro del detalle de la localidad)
- Franja superior: hora local, UTC, horario de verano, próximo cambio de hora, **Abierta / Cerrada ahora**.
- **Horario semanal** con versiones (vigente, programado a futuro e historial).
- **Editor de horario**: plantillas, copiar de otra localidad, hasta 4 intervalos por día.
- **Suscripción a calendarios de feriados.**
- **Días no laborables y excepciones**: cierre, jornada especial o día laborable adicional.
- **Vista anual**: 12 meses con leyenda y tooltip por día, y opción de agregar una excepción desde ahí.

### Calendarios de feriados (`/settings/calendarios-feriados`)
- Lista por país y ámbito, con aviso "Sin feriados cargados" para el año siguiente.
- Detalle con pestañas **Feriados** (por año) y **Localidades suscritas**.
- Editor de feriado: día completo o jornada reducida.
- **Importar** desde CSV o JSON, con validación previa.

### Funciones de localidad (`/settings/funciones-localidad`)
- Catálogo de funciones diplomáticas, consulares y multilaterales.

---

## Transversal
- **Feature flag** `calendarioLaboral` para encender o apagar todo lo de calendarios.
- **Permisos**: admin de calendario o gestor de la localidad. Las acciones no permitidas se ocultan.
- Horas siempre en la **zona de la localidad**, nunca en la del navegador.
- Errores claros: conflictos (409), y aviso "Otra persona modificó este registro" con botón Recargar (412).
- Ventana retroactiva: los no admins no pueden editar fechas demasiado antiguas.
