# Esquemas por módulo en RP_ReyPlastCoreBD

> Estado: **implementada el 2026-09-30** mediante la migración
> `20260930154604_MoveCoreTablesToModuleSchemas` y
> `20260930161037_RestoreLowercaseSchemaNames`.
> Fecha del análisis: 2026-09-30. Plan hermano para MasterLogicDB: `MasterLogicApi_V2/DB-SCHEMAS-PLAN.md`.

RP_ReyPlastCoreBD distribuye las tablas de la API entre `dbo`, `orders` y `catalog`; las tablas
de Hangfire permanecen en su esquema propio `hangfire`. No hay procedimientos almacenados, vistas,
funciones ni SQL escrito a mano sobre estas
tablas (todo el acceso es LINQ/EF y EFCore.BulkExtensions, que toma el esquema del modelo de EF).

## Distribución aplicada: `dbo` + 2 esquemas (+ `hangfire`)

La división sigue los módulos del propio código (`Domain/Orders`, `Domain/Catalog`) y los
controladores (`Orders*`, `Products*`, `Channels`).

| Esquema | Módulo | Tablas | Filas aprox. |
|---|---|---|---|
| `dbo` | Transversal (lo compartido) | `__EFMigrationsHistory`, `IntegrationCursors` | — |
| `orders` | Pedidos (OSIS/SAP) | `Orders`, `OrderDetails`, `OrderDetailQuantityHistory`, `OrderSituations` | 270 mil / 1,96 M / 1,95 M / 10 |
| `catalog` | Catálogo | `Products`, `Colors`, `Channels`, `ChannelGroups` | 12 mil / 69 mil / 39 / 1 |
| `hangfire` | Jobs | Tablas de Hangfire | — |

### Por qué quedan así

- **`IntegrationCursors` en `dbo`:** guarda el cursor del Delta Sync por origen y recurso; lo usan tanto
  pedidos como productos, así que es compartido.
- **`OrderSituations` en `orders`:** es el catálogo de situaciones del pedido y solo lo referencia
  `Orders` (FK `Orders` → `OrderSituations`).
- **`Channels`/`ChannelGroups` en `catalog`:** en el código viven en `Domain/Catalog`. `Orders` guarda
  `ChannelCode` como texto, sin llave foránea, así que no hay dependencia de base de datos entre
  `orders` y `catalog`.

### Llaves foráneas

Todas quedan dentro de su esquema: `Channels` → `ChannelGroups`, `Colors` → `Products`,
`OrderDetails` → `Orders`, `OrderDetailQuantityHistory` → `OrderDetails`, `Orders` → `OrderSituations`.
No hay llaves entre esquemas.

## Implementación aplicada

1. `ReyplastCoreDbContext.OnModelCreating` declara explícitamente los esquemas `orders` y `catalog`.
2. La migración crea ambos esquemas y mueve las ocho tablas mediante `RenameTable`/`ALTER SCHEMA`.
   Solo cambia metadatos: no copia los ~4 millones de filas y conserva índices y llaves.
3. `IntegrationCursors` y `__EFMigrationsHistory` permanecen en `dbo`; Hangfire usa `hangfire`.

## Riesgos y precondiciones

- **Consumidores externos:** MasterLogicApi y demás consumen Core por HTTP, no leen la base directo. Aun
  así, confirmar que ningún reporte o consulta guardada lea `dbo.Orders` u otras tablas directamente.
- **Despliegue conjunto:** la migración y la API nueva se publican juntas (la base es compartida entre
  el entorno local y el servidor 192.168.1.40). Un job de Hangfire en curso durante el cambio fallaría y
  Hangfire lo reintentaría (máximo 3 veces).
- **Permisos:** hoy todo usa `sa`; si algún usuario tiene permisos limitados a `dbo`, darle acceso a los
  esquemas nuevos.
