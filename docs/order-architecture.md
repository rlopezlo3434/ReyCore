# Integración de pedidos OSIS

## Fuentes XIMESA

La integración consulta XIMESA directamente con Entity Framework Core, sin procedimientos
almacenados ni `SqlCommand`:

- `dbo.PEDIDOS_VENTA_PPC`: cabecera del pedido.
- `dbo.PEDIDOS_VENTA_PPD`: productos del pedido.
- `dbo.AUXILIARES_AUX`: nombre del cliente.
- `dbo.CLIENTES_CIA_CLC`: grupo comercial asignado al cliente.
- `dbo.VENDEDOR_CLIENTE_GRUPOS`: código y nombre del canal.
- `dbo.PRODUCTOS_PRD`: descripción del producto del detalle.

Solo se consulta `CIA_CODCIA = '01'`, correspondiente a Reyplast.

## Mapeo de cabecera

| Campo API | Origen OSIS | Regla |
|---|---|---|
| `DocNum` | `PPC.PPC_NUMPPC` | Se conserva como texto. |
| `DocDate` | `PPC.PPC_FECCRE` | Fecha de creación, no `PPC_FECDOC`. |
| `CardCode` | `PPC.AUX_CODAUX` | Código del cliente. |
| `CardName` | `AUX.AUX_NOMAUX` | Maestro de auxiliares. |
| `SituationCode` | `PPC.PPC_SITPPC` | Conserva exactamente el código OSIS de `0` a `7`. |
| `SituationDescription` | `OrderSituations.Description` | Descripción normalizada del catálogo de situaciones. |
| `DocTotal` | `PPC.PPC_IMPTOT` | Importe total del documento. |
| `ChannelCode` | `VCG.CANAL_COD` | Grupo enlazado por `CLC.IDGRUPO_VEND`. |
| `ChannelName` | `VCG.GRUPO_VEND` | Nombre comercial, por ejemplo AUTOSERVICIOS. |
| `IsActive` | `PPC.PPC_INDEST` | Activo cuando coincide con `Osis:ActiveStatusCode`. |

Las situaciones se identifican mediante la clave compuesta `SourceSystem + Code`; por ello un
código nunca se interpreta sin conocer su origen. OSIS registra los códigos 0 a 7 descritos
anteriormente. SAP registra `O` como abierto y `C` como cerrado. Cerrado no implica anulado;
para distinguir una anulación SAP, el conector deberá recibir y mapear el indicador `CANCELED`.

## Mapeo del detalle

| Campo API | Origen OSIS |
|---|---|
| `LineNumber` | `PPD.PPD_CORPPD` |
| `ItemCode` | `PPD.PRD_CODPRD` |
| `Description` | `PRODUCTOS_PRD.PRD_DESESP` |
| `Quantity` | `PPD.PPD_CANSOL` |
| `Price` | `PPD.PPD_PREUNI` |
| `LineTotal` | `PPD.PPD_IMPBRU` (cantidad por precio, sin IGV) |
| `IsActive` | `PPD.PPD_INDEST` |

La cabecera y el detalle se guardan en `ReyplastCoreDB.dbo.Orders` y
`ReyplastCoreDB.dbo.OrderDetails`. La relación es `OrderDetails.OrderId -> Orders.Id`. La cabecera
también se relaciona con `OrderSituations` mediante `Orders.SituationCode`. El listado de
cabeceras no carga detalles; estos tienen su propia paginación para evitar respuestas grandes.

`SourceDocumentId` identifica internamente el documento en cada origen: OSIS usa
`BranchCode|DocNum` y SAP usará `DocEntry`. No se expone en las respuestas de pedidos. La
sucursal también es interna y puede ser null porque el contrato SAP no proporciona BranchCode.

`SourceDetailId` identifica internamente cada línea y tampoco se expone. En OSIS usa `ItemCode`,
reflejando la llave primaria comprobada de PPD: `CIA_CODCIA + SUC_CODSUC + PPC_NUMPPC +
PRD_CODPRD`. `PPD_CORPPD` se conserva para presentación, pero puede repetirse. El contrato SAP
actual no entrega `LineNum`, por lo que `LineNumber` permanece en null y no se inventa. Para
evitar confundir `ItemCode` con el número de línea, la integración SAP deberá reemplazar el
conjunto completo de detalles del pedido en cada actualización hasta que el origen entregue una
identidad estable de línea.

## API

- `POST /api/orders/sync/osis`: encola la sincronización en segundo plano.
- `GET /api/orders/sync/executions`: historial de sincronización de pedidos.
- `GET /api/orders/sync/executions/{executionId}`: avance y duración.
- `DELETE /api/orders/sync/executions/{executionId}`: cancelación cooperativa.
- `GET /api/orders`: cabeceras paginadas con filtros.
- `GET /api/orders/{id}`: una cabecera.
- `GET /api/orders/doc-number/{docNum}`: pedido completo por `PPC_NUMPPC`, con cabecera y líneas.
- `GET /api/orders/{docNum}/complete`: alias para consultar el pedido completo por `PPC_NUMPPC`.
- `GET /api/orders/doc-number/{docNum}/details`: todas las líneas del pedido por `PPC_NUMPPC`.
- `GET /api/orders/{id}/details`: detalle paginado.

El cursor interno recorre `PPC_NUMPPC` en orden descendente para importar primero los pedidos
recientes. No existe un límite total de registros. Cada página usa Bulk Insert/Update dentro
de una transacción y queda identificada mediante `SyncExecutionId`.

`pageNumber` y `pageSize` pueden cambiarse y solo deben ser mayores que cero; no existe un tope
hardcodeado para `pageSize`. `situationCode`, `channelCode`, `search` y `sourceSystem` aceptan
varios valores, repetidos (`?situationCode=1&situationCode=2`) o separados por comas
(`?situationCode=1,2`). Los valores de un mismo filtro se combinan con OR y los filtros distintos
con AND. `dateFrom` y `dateTo` delimitan `PPC_FECCRE` y permanecen como valores individuales.

## Búsqueda paginada de pedidos

| Parámetro | Regla |
|---|---|
| `pageNumber` | Página solicitada; predeterminado 1 y debe ser mayor que cero. |
| `pageSize` | Cabeceras por página; predeterminado 20, debe ser mayor que cero y no tiene tope hardcodeado. |
| `search` | Coincidencia parcial en `DocNum`, `CardCode` o `CardName`. Acepta varios valores. |
| `situationCode` | Códigos del origen: OSIS 0-7 y SAP O/C. Acepta varios valores. |
| `channelCode` | Códigos comerciales de canal. Acepta varios valores. |
| `sourceSystem` | Sistemas de origen sin distinguir mayúsculas y minúsculas. Acepta varios valores. |
| `dateFrom` | Inicio inclusivo del rango sobre `PPC_FECCRE`. |
| `dateTo` | Fin inclusivo del rango sobre `PPC_FECCRE`. |

Listado general, solo cabeceras:

```http
GET /api/orders?pageNumber=1&pageSize=20
```

Búsqueda por número de pedido, cliente o nombre:

```http
GET /api/orders?pageNumber=1&pageSize=20&search=26091153
GET /api/orders?pageNumber=1&pageSize=20&search=C20108258731
GET /api/orders?pageNumber=1&pageSize=20&search=SANI
```

Varios textos de búsqueda; cualquiera puede coincidir:

```http
GET /api/orders?search=26091153,C20108258731&pageNumber=1&pageSize=20
GET /api/orders?search=26091153&search=C20108258731&pageNumber=1&pageSize=20
```

Rango de fechas de creación inclusivo:

```http
GET /api/orders?dateFrom=2026-09-01&dateTo=2026-09-10&pageNumber=1&pageSize=20
```

Múltiples situaciones, canales y sistemas de origen:

```http
GET /api/orders?situationCode=1,2&channelCode=01,03&sourceSystem=osis,SAP&pageNumber=1&pageSize=100
```

Combinación completa. Los valores dentro de cada parámetro usan OR y los distintos
parámetros usan AND:

```http
GET /api/orders?search=26091153,C20108258731&situationCode=1,2&channelCode=01,03&sourceSystem=osis,SAP&dateFrom=2026-09-01&dateTo=2026-09-10&pageNumber=1&pageSize=100
```

Pedido completo por `PPC_NUMPPC`, incluyendo todas sus líneas:

```http
GET /api/orders/26091153/complete
```

Esta consulta aplica lectura bajo demanda: primero busca en `ReyplastCoreDB`; si no existe,
consulta exclusivamente `CIA_CODCIA = '01'` y ese `PPC_NUMPPC` en OSIS, persiste una cabecera
con todos sus PPD y devuelve el resultado. No inicia ni recorre la sincronización completa.
Si tampoco existe en OSIS, responde 404.

Solo los detalles del pedido por `PPC_NUMPPC`:

```http
GET /api/orders/doc-number/26091153/details
```
