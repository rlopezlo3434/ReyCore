# Integración de productos y colores con OSIS

## Fuente autoritativa

El catálogo se lee directamente de la base `XIMESA` de OSIS mediante un `OsisDbContext`
independiente. No usa procedimientos almacenados, vistas ni `SqlCommand`.

Las conexiones tienen responsabilidades separadas:

- `ConnectionStrings:SqlServerConnection`: base propia `RP_ReyPlastCoreBD`.
- `ConnectionStrings:OsisConnection`: base `XIMESA`, usada únicamente como fuente de lectura.

En despliegue pueden reemplazarse con `ConnectionStrings__SqlServerConnection` y
`ConnectionStrings__OsisConnection`.

## Tablas y alcance funcional

- `dbo.PRODUCTOS_PRD`: código, descripción, estado, fecha de creación y clasificación.
- `dbo.PRODUCTOS_COLORES_PCO`: colores asociados y habilitación de venta.
- `dbo.COLORES_COL`: descripción y estado del color.

La lectura aplica internamente las reglas del catálogo usado por MasterLogic:

- `cia_codcia = '01'`.
- `tin_codtin = '01'`.

El segundo filtro es indispensable: limita la integración a productos terminados y evita importar materias primas y otras clases de
inventario que existen en `PRODUCTOS_PRD`, pero no pertenecen al catálogo funcional de
productos. Es una condición interna; `tin_codtin` no se expone en el DTO.

No se consultan fórmulas, pesos, unidades de medida, categorías, familias, subfamilias, EAN,
color por defecto ni armado. La fecha de creación sale de `PRODUCTOS_PRD.prd_feccre`; cuando
OSIS no la tiene, se usa la fecha de la primera sincronización local.

## Contrato y clasificación

El producto expone tipo, código genérico, descripción, estado y colores. Cada color expone
código, descripción, habilitación de venta y estado.

El código `505A` significa `SURTIDO`, no un color concreto. La integración lo excluye durante
la lectura y elimina cualquier asociación `505A` preexistente de `RP_ReyPlastCoreBD.catalog.Colors`. Las
operaciones manuales tampoco permiten registrarlo.

La clasificación se deriva de `prd_indset` y, cuando vale `0`, de `prd_prduni`:

| Valor OSIS | Tipo de API |
|---|---|
| `prd_indset = 0`, `prd_prduni = 1` | `PT-SIMPLE` |
| `prd_indset = 0`, otro `prd_prduni` | `SIMPLE-PARTE` |
| `prd_indset = 1` | `INTERMEDIO` |
| `prd_indset = 2` | `PT-COMPUESTO` |
| `prd_indset = 3` | `PT-SET` |
| otro valor | `NO-DEFINIDO` |

`GET /api/products` permite filtrar opcionalmente con `search`, uno o varios `productType`,
`isActive` y `sourceSystem`. Para varios tipos se repite el parámetro, por ejemplo
`productType=PT-SIMPLE&productType=PT-SET`. Siempre
devuelve una respuesta paginada.

## Sincronización

La API no sincroniza durante el arranque. El worker arranca con una cola vacía y solamente
trabaja cuando el catálogo se solicita mediante el endpoint:

`POST /api/products/sync/osis`

El endpoint responde `202 Accepted` con un `executionId`; la sincronización continúa aunque el
cliente cierre Swagger o corte la conexión. Levantar o reiniciar la aplicación no consulta OSIS,
no crea una ejecución y no bloquea Swagger.

La lectura recorre todo el origen mediante paginación interna por cursor (`ProductCode`). Por
cada página realiza una consulta de productos y otra consulta conjunta de colores, evitando
el problema N+1. La persistencia compara los registros existentes y usa inserciones y
actualizaciones masivas dentro de una transacción por página.

`ProductService` contiene únicamente las operaciones del catálogo local.
`ProductSyncService` contiene la orquestación general de importaciones. El método
`SynchronizeOsisAsync` y `OsisCatalogReader` encapsulan lo específico de XIMESA/OSIS.

Las sincronizaciones de una instancia se serializan con un bloqueo asíncrono. Los `DbContext`
no se comparten entre tareas paralelas.

## Historial y trazabilidad

No hay historial propio de sincronizaciones (la tabla `dbo.SyncExecutions` se eliminó en la
migración `ReplaceSyncTrackingWithIntegrationCursor`). Hangfire es la única fuente de verdad:
`POST /api/products/sync` responde `202 Accepted` con el `jobId`, y su estado, reintentos
(máximo 3), errores y duración se consultan en `/hangfire/jobs/details/{jobId}`. Desde el mismo
dashboard se puede borrar un job en cola o reencolarlo.

Cancelar un job en curso revierte la transacción de la página que se encuentra activa, pero
conserva las páginas que ya fueron confirmadas. Nunca ejecuta la desactivación final de
registros ausentes.

`Products` y `Colors` guardan:

- `SyncExecutionId`: última ejecución que observó el registro.
- `LastSynchronizedAt`: fecha en que el registro fue sincronizado por última vez.
- `SourceSystem`: procedencia del producto (`OSIS`, `SAP` o `MANUAL`). Los colores heredan
  la procedencia del producto y no duplican este valor.

Solo al terminar correctamente se desactivan registros OSIS ausentes de la ejecución actual.
Una ejecución fallida no ejecuta esta etapa.

## Escritura del catálogo local

`POST /api/products`, `PUT /api/products/{id}` y `PATCH /api/products/{id}/status` existen
para registros administrativos creados con `SourceSystem = MANUAL`. El `PUT` reemplaza los
datos editables y sincroniza el conjunto de colores: agrega los nuevos, actualiza los
existentes e inactiva los que ya no llegan en la solicitud.

Los productos cuyo origen sea `OSIS` o `SAP` no se pueden alterar por estos endpoints; la API
responde `409 Conflict`. Deben corregirse en el sistema propietario y volver a sincronizarse,
evitando que el siguiente proceso sobrescriba silenciosamente un cambio local.

## Ejecución por ambiente

`Properties/launchSettings.json` contiene perfiles Development, Production e IIS Express.
Los `appsettings.Development.json` y `appsettings.Production.json` personalizan cada ambiente;
las conexiones se pueden suministrar mediante variables de entorno.

Los endpoints están marcados como anónimos. Swagger está disponible en `/swagger`. No se
registra middleware CORS; un frontend de otro origen debe consumir mediante proxy o una
política configurada en la infraestructura que publique la API.

La carpeta `Properties/PublishProfiles` ofrece tres publicaciones Release hacia
`bin/Release/net6.0/publish`: `FolderProfileDefault`, `FolderProfileDevelopment` y
`FolderProfileProduction`. Cada perfil publica solamente su configuración seleccionada con el
nombre final `appsettings.json`; el proyecto valida que el destino no sea la carpeta fuente.

El esquema está versionado por la migración `InitialReyplastCoreCatalog` y los mapeos de
respuesta se centralizan en `Shared/Mappers/ProductMapper.cs`.
