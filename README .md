# VALUM — Gestión de Proyectos

API REST con **.NET 8**, **Clean Architecture**, **CQRS/MediatR**, **EF Core 8** y **SQLite** para gestionar proyectos y tareas. Incluye consultas GraphQL, paginación por cursor y trazabilidad con Serilog.

## 1. Inicio rápido

**Requisitos:** .NET SDK 8 (o posterior compatible con `net8.0`). SQLite es embebida; no requiere un servidor de base de datos.

Desde la raíz del repositorio, en PowerShell:

```powershell
dotnet run --project .\src\VALUM.Api --urls "http://localhost:4596"
```

Comprobar: [`GET /health`](http://localhost:4596/health). La configuración incluida utiliza `Data Source=valum.db`, aplica las migraciones al iniciar y **no carga datos de ejemplo** por defecto.

## 2. Configuración y base de datos

Los ajustes se leen de `src/VALUM.Api/appsettings.json`, de la configuración del entorno y de variables de entorno. Estas últimas permiten cambiar el comportamiento **sin editar archivos del repositorio**.

| Parámetro | Valor por defecto | Comportamiento |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `Data Source=valum.db` | Archivo SQLite; obligatorio (la aplicación falla al iniciar si no existe la configuración). |
| `Database:AutoMigrate` | `true` | Ejecuta migraciones EF Core al iniciar; crea la BD si no existe. |
| `SeedData:Enabled` | `false` | Permite cargar datos de ejemplo, **solo en `Development`**. |
| `ASPNETCORE_ENVIRONMENT` | Según el perfil de ejecución | `Development` habilita el seed si también está activado. |
| `Serilog` / `LogCleanup` | Configuración en `appsettings.json` | Registro estructurado en consola/archivo y limpieza de logs. |

**Ejemplo: arrancar con una base de pruebas nueva y datos de ejemplo** (PowerShell):

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__DefaultConnection = "Data Source=valum_demo.db"
$env:Database__AutoMigrate = "true"
$env:SeedData__Enabled = "true"
dotnet run --project .\src\VALUM.Api --urls "http://localhost:4596"
```

En .NET, `__` en el nombre de una variable de entorno corresponde a `:` en la clave de configuración. Para cambiar los valores de forma permanente, se pueden configurar las mismas claves en `appsettings.Development.json` o `appsettings.json`.

**Comportamiento del seed:** genera **5 proyectos y 20 tareas** con distintos estados y prioridades; 3 proyectos activos y 2 archivados. Solo se ejecuta si `SeedData:Enabled=true`, el entorno es `Development` y no existen registros previos en las tablas (incluidas las tareas eliminadas lógicamente). No duplica datos al reiniciar. Desactivarlo **no borra** los datos existentes.

**Consideraciones:** migración y seed son procesos independientes; la BD se inicializa aun con seed desactivado. Si `AutoMigrate=false`, la aplicación no aplicará migraciones automáticamente y el esquema deberá estar preparado. La ruta relativa de SQLite se resuelve respecto al directorio de trabajo del proceso. Los archivos `.db` y sus auxiliares están excluidos de Git.

## 3. Arquitectura

| Proyecto | Responsabilidad |
|---|---|
| `VALUM.Domain` | Entidades y enumeraciones |
| `VALUM.Application` | Commands, Queries, Handlers, DTOs, interfaces y validators |
| `VALUM.Infrastructure` | EF Core, SQLite, repositorios, migraciones y seed |
| `VALUM.Api` | REST, GraphQL, middleware y configuración |
| `VALUM.Tests` | Tests unitarios y de integración con xUnit y Moq |

Los controladores delegan en **MediatR**; `ValidationBehavior` ejecuta **FluentValidation** para las entradas y los **Command Handlers** aplican las reglas de negocio. El middleware centraliza las respuestas de error `400`, `404` y `422` con `{ status, error, message, details }`.

## 4. API REST y reglas

| Método | Ruta | Función |
|---|---|---|
| `POST` | `/api/projects` | Crear proyecto |
| `GET` | `/api/projects` | Listar proyectos; filtro y paginación |
| `GET` | `/api/projects/{id}` | Detalle y resumen de tareas |
| `PATCH` | `/api/projects/{id}/archive` | Archivar proyecto |
| `POST` | `/api/projects/{projectId}/tasks` | Crear tarea |
| `GET` | `/api/projects/{projectId}/tasks` | Listar tareas; filtros y paginación |
| `GET` | `/api/tasks/{id}` | Detalle de tarea |
| `PATCH` | `/api/tasks/{id}/status` | Actualizar estado |
| `DELETE` | `/api/tasks/{id}` | Soft delete |

**RN-01 a RN-06:** no crear tareas en proyectos archivados; no archivar proyectos con tareas `InProgress`; asignar `CompletedAt` UTC al marcar `Done`; soft delete idempotente (`204`); calcular el resumen excluyendo tareas eliminadas; y paginar todos los listados (`page=1`, `pageSize=10`, máximo `50`).

## 5. Extras implementados

- **GraphQL / Hot Chocolate:** `POST /graphql`, consultas `projects` y `project(id)` reutilizando las Queries de MediatR; interfaz de consulta de solo lectura.
- **Serilog y CorrelationId:** logs estructurados en consola y archivo; se acepta `X-Correlation-ID` por petición y se genera uno si falta o es inválido.
- **Cursor Pagination:** `GET /api/projects/cursor` y `GET /api/projects/{projectId}/tasks/cursor`, con `nextCursor` y `hasNext`; convive con la paginación `page`/`pageSize` original.

El **Result Pattern** no se implementó: se conserva el manejo centralizado de excepciones y el contrato HTTP requerido.

## 6. Pruebas y Postman

```powershell
dotnet test .\VALUM.ProjectManagement.sln
```

Los tests automatizados utilizan datos aislados y **no dependen del seed de desarrollo**. Las colecciones de Postman están en `postman/`:

| Carpeta | Cobertura |
|---|---|
| `01-Projects` | Creación, listados y validaciones |
| `02-Tasks` | Ciclo de vida, filtros y soft delete |
| `03-ProjectArchiving` | Archivado y reglas de negocio |
| `04-Extras` | Serilog, GraphQL y Cursor Pagination |

Importar también `postman/VALUM_Local.postman_environment.json`, configurar `baseUrl` según el puerto utilizado y ejecutar las colecciones desde Postman Runner. La colección `04-Extras` usa consultas de solo lectura sobre una base ya poblada.

## 7. Decisiones y supuestos

- `OwnerId` es una referencia lógica `Guid`; **no se implementa autenticación**, según el alcance de la prueba.
- `IsDeleted` preserva físicamente las tareas, pero las excluye de consultas y estadísticas; una segunda eliminación sigue devolviendo `204`.
- Los proyectos archivados conservan sus tareas anteriores, pero no admiten tareas nuevas. Las tareas `InProgress` bloquean el archivado.
- Los datos de ejemplo sirven para inspección manual; no se mezclan con los datos creados por tests automáticos.
- Las migraciones se aplican al arrancar para simplificar la evaluación; el seed es **optativo, seguro frente a reinicios y restringido a desarrollo**.
- GraphQL reutiliza la capa Application y la paginación por cursor se añadió sin sustituir ni romper los contratos REST anteriores.
