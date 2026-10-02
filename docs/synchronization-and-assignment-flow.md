# Sincronizacion de pedidos y gestion de asignaciones

## Responsabilidades

- OSIS es solo la fuente externa de pedidos. ReyplastCoreAPI nunca escribe en OSIS.
- RP_ReyPlastCoreBD conserva la copia consultable de pedidos, lineas, canales, grupos y cantidades suspendidas.
- MasterLogicAPI conserva asignaciones, solicitudes de trabajo, avances, tiempos y leases de edicion.
- SignalR notifica cambios ya confirmados; no sincroniza OSIS ni decide concurrencia.
- Hangfire ejecuta exclusivamente trabajos acotados de sincronizacion en segundo plano.

## Flujo por rango

```mermaid
sequenceDiagram
    participant UI as MasterLogic Angular
    participant ML as MasterLogicAPI
    participant Core as ReyplastCoreAPI
    participant HF as Hangfire
    participant OSIS as OSIS (solo lectura)
    participant DB as RP_ReyPlastCoreBD

    UI->>ML: Aplicar filtros (desde/hasta)
    ML->>Core: GET pedidos desde Core
    Core->>DB: SELECT paginado
    DB-->>UI: Resultado local inmediato
    UI->>ML: Actualizar rango permitido
    ML->>Core: POST sync/range (rango obligatorio, maximo configurable)
    Core->>HF: Enqueue job core-sync
    HF->>OSIS: Leer lotes del rango
    HF->>DB: Upsert por claves naturales
    HF->>DB: Actualizar watermark al completar
```

## Flujo puntual por pedido

```mermaid
flowchart LR
    UI[Detalle por DocNumber] --> ML[MasterLogicAPI]
    ML -->|refresh puntual| CORE[ReyplastCoreAPI]
    CORE -->|SELECT acotado| OSIS[(OSIS)]
    CORE -->|upsert pedido y lineas| DB[(RP_ReyPlastCoreBD)]
    ML -->|GET snapshot| CORE
    ML --> UI
```

La paginacion, los tabs, una asignacion, un retiro y un evento SignalR solo leen Core; no encolan sincronizaciones.

## Edicion concurrente

```mermaid
sequenceDiagram
    participant A as Supervisor A
    participant API as MasterLogicAPI
    participant DB as MasterLogicDB
    participant Hub as SignalR
    participant B as Supervisor B

    A->>API: POST /orders/{doc}/edit-lease
    API->>DB: Crear/renovar lease unico (60 s)
    API->>Hub: edit-lease-acquired (despues del commit)
    Hub-->>B: Pedido actualizado/en edicion
    B->>API: POST /orders/{doc}/edit-lease
    API-->>B: 409 + usuario propietario
    loop cada 20 s
      A->>API: PUT heartbeat
    end
    A->>API: DELETE edit-lease
    API->>Hub: edit-lease-released
```

## Tablas de sincronizacion

Hangfire ya conserva jobs, estados, reintentos y errores en su propio esquema. Por eso, a futuro,
`SyncExecutions` puede reducirse o retirarse una vez que se eliminen sus claves foraneas desde
pedidos, lineas, productos y canales y los contratos de API devuelvan el identificador de Hangfire.

`SyncCheckpoints` no duplica Hangfire: contiene el watermark de negocio desde el que comienza el
proximo delta. Debe conservarse (o reemplazarse por una tabla pequena de cursores) aunque se quite
`SyncExecutions`. El orden seguro es:

1. dejar de exponer `SyncExecutionId` y usar `BackgroundJobId`;
2. quitar las FK de catalogos y pedidos a `SyncExecutions`;
3. mover diagnostico/reintentos al dashboard de Hangfire;
4. migrar solo el ultimo watermark valido a un cursor de sincronizacion;
5. eliminar `SyncExecutions`; renombrar `SyncCheckpoints` a `SyncCursors` si se desea aclarar su funcion.
