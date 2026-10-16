# Documentación técnica — Sistema de Suscripciones

## 1. Propósito del sistema

Sistema de Suscripciones es una aplicación web para administrar facturas de socios y controlar el ciclo de recordatorios de pago. Permite crear, consultar, editar y eliminar facturas; enviar avisos por correo; procesar recordatorios pendientes; configurar SMTP durante el desarrollo; y generar un resumen administrativo de la factura en PDF.

La solución separa las reglas de negocio, los casos de uso, la infraestructura, la API HTTP y la interfaz gráfica. Esta separación reduce el acoplamiento y permite probar la lógica principal sin conectarse a MongoDB ni enviar correos reales.

## 2. Tecnologías y requisitos

| Componente | Tecnología | Finalidad |
| --- | --- | --- |
| Backend | .NET 10 / ASP.NET Core | API REST, inyección de dependencias y servidor web |
| Base de datos | MongoDB Atlas | Persistencia de facturas y socios embebidos |
| Frontend | Angular 22 | Interfaz web, formularios y consumo de la API |
| Correo | `System.Net.Mail` | Envío de recordatorios mediante SMTP |
| PDF | jsPDF 4.2.1 | Generación del resumen de factura desde el navegador |
| Documentación API | Swashbuckle 10.3.0 | Swagger/OpenAPI en desarrollo |
| Variables locales | DotNetEnv 3.2.0 | Lectura del archivo `.env` durante desarrollo |
| Pruebas .NET | xUnit 2.9.3 | Pruebas unitarias de las reglas de recordatorios |
| Pruebas Angular | Vitest 5 | Pruebas unitarias del frontend |
| Cobertura .NET | coverlet.collector 6.0.4 | Recolección opcional de cobertura |

Requisitos para ejecutarlo:

- SDK de .NET 10.
- Node.js 22.22.3 o una versión 22 compatible posterior.
- npm 10 o compatible.
- Una cuenta y un clúster de MongoDB Atlas accesibles desde la IP del equipo.
- Credenciales SMTP válidas únicamente si se probarán los envíos de correo.
- Git es recomendable, aunque no es necesario para ejecutar la aplicación.

Para comprobar las versiones instaladas:

```bash
dotnet --version
node --version
npm --version
```

## 3. Arquitectura de la solución

La solución aplica una arquitectura por capas inspirada en Clean Architecture:

```text
SistemaSuscripciones.Domain
          ↑
SistemaSuscripciones.Application
          ↑
SistemaSuscripciones.Infrastructure
          ↑
SistemaSuscripciones.Api ← HTTP → SistemaSuscripciones.Web
```

La dirección importante es la de las dependencias del código:

- `Domain` no depende de otras capas.
- `Application` depende de `Domain` y define interfaces, no tecnologías concretas.
- `Infrastructure` implementa las interfaces de `Application` mediante MongoDB y SMTP.
- `Api` compone las dependencias, expone los endpoints y puede servir el frontend compilado.
- `Web` consume la API usando rutas `/api` y no accede directamente a MongoDB ni a SMTP.

### 3.1. SistemaSuscripciones.Domain

Contiene el modelo principal del negocio:

- `Entities/Factura.cs`: representa la factura, su valor, vencimiento, socio, estado y último correo enviado.
- `Entities/Socio.cs`: representa al titular de la suscripción.
- `Enums/EstadoRecordatorio.cs`: define `PrimerRecordatorio`, `SegundoRecordatorio` y `Desactivado`.

No contiene código de MongoDB, HTTP, Angular ni SMTP. Por eso puede reutilizarse y probarse sin infraestructura externa.

### 3.2. SistemaSuscripciones.Application

Contiene contratos, DTO y casos de uso:

- `IFacturaRepository`: define cómo consultar, crear, actualizar y eliminar facturas.
- `IEmailService`: define el envío de un recordatorio sin decidir qué proveedor SMTP se utiliza.
- `IRecordatorioService`: expone el procesamiento individual y masivo de recordatorios.
- `RecordatorioService`: implementa la regla de transición de estados.
- `FacturaResumenDto`, `GuardarFacturaDto` y `ResultadoRecordatorioDto`: transportan datos sin exponer directamente el modelo persistido.

La lógica de `RecordatorioService.EnviarAsync(id)` es:

1. Busca la factura mediante `IFacturaRepository.ObtenerPorIdAsync`.
2. Devuelve un resultado fallido si no existe.
3. Impide el envío si la factura está desactivada.
4. Solicita el envío a `IEmailService` usando el estado actual.
5. Solo después de un envío exitoso cambia el estado:
   - `PrimerRecordatorio` → `SegundoRecordatorio`.
   - `SegundoRecordatorio` → `Desactivado`.
6. Guarda `UltimoCorreoEnviado` en UTC.
7. Persiste el cambio con `IFacturaRepository.ActualizarAsync`.

Este orden evita avanzar el estado cuando el proveedor SMTP falla.

`ProcesarPendientesAsync()` consulta todas las facturas, excluye las desactivadas y procesa secuencialmente las restantes. Devuelve un resultado por cada factura procesada.

### 3.3. SistemaSuscripciones.Infrastructure

Implementa las dependencias externas:

- `MongoDb/FacturaMongoRepository.cs`: implementación de `IFacturaRepository` con `MongoDB.Driver`.
- `MongoDb/MongoDbSettings.cs`: opciones de conexión, base de datos y colección.
- `Email/SmtpEmailService.cs`: implementación de `IEmailService` con `System.Net.Mail`.
- `Email/SmtpSettingsStore.cs`: conserva la configuración SMTP cifrada en el equipo local.
- `DependencyInjection.cs`: registra repositorio, correo, configuración y protección de datos.

MongoDB usa una colección llamada `facturas`. Cada documento de factura almacena el objeto `Socio` embebido, por lo que una lectura obtiene toda la información necesaria sin una consulta adicional.

`SmtpEmailService` elige la plantilla según el estado actual y reemplaza estas variables:

- `{NombreSocio}`
- `{CorreoSocio}`
- `{PlanSuscripcion}`
- `{NumeroFactura}`
- `{Valor}`
- `{FechaVencimiento}`

La configuración SMTP guardada desde la interfaz se cifra con ASP.NET Core Data Protection antes de almacenarse en `src/SistemaSuscripciones.Api/.smtp-settings.enc`. La contraseña no vuelve al navegador y ese archivo está excluido de Git.

### 3.4. SistemaSuscripciones.Api

Es el punto de entrada del backend. `Program.cs` realiza estas tareas:

1. Detecta el entorno de ejecución.
2. En `Development`, busca y carga `.env` con DotNetEnv.
3. Agrega nuevamente las variables de entorno para que sobrescriban `appsettings.json`.
4. Configura controladores y serialización de enums como texto.
5. Registra Swagger/OpenAPI.
6. Ejecuta `AddInfrastructure(...)` y registra `RecordatorioService`.
7. Habilita CORS para `http://localhost:4200`.
8. Configura respuestas genéricas para errores inesperados sin divulgar secretos.
9. Sirve los archivos de Angular cuando `SERVEANGULAR=true`.
10. Mapea los controladores REST.

`Controllers/FacturasController.cs` expone:

| Método | Ruta | Función | Respuesta principal |
| --- | --- | --- | --- |
| GET | `/api/Facturas` | Consultar todas las facturas | `200 OK` |
| POST | `/api/Facturas` | Crear una factura | `201 Created` |
| PUT | `/api/Facturas/{id}` | Actualizar una factura | `200 OK` |
| DELETE | `/api/Facturas/{id}` | Eliminar una factura | `204 No Content` |
| POST | `/api/Facturas/{id}/enviar-recordatorio` | Enviar el siguiente aviso | `200 OK` o `400 Bad Request` |
| POST | `/api/Facturas/procesar-pendientes` | Procesar todas las facturas activas | `200 OK` |

`Controllers/ConfiguracionController.cs` expone, solo en `Development`:

| Método | Ruta | Función |
| --- | --- | --- |
| GET | `/api/configuracion/smtp` | Consultar la configuración no sensible |
| PUT | `/api/configuracion/smtp` | Guardar la configuración SMTP cifrada |
| POST | `/api/configuracion/smtp/probar` | Enviar un correo de comprobación |

Swagger está disponible en `https://localhost:7275/swagger` mientras el entorno sea `Development`.

### 3.5. SistemaSuscripciones.Web

La aplicación Angular proporciona:

- Panel de resumen y tabla de facturas.
- Búsqueda y filtros.
- Formularios de creación y edición.
- Eliminación con confirmación.
- Envío individual y masivo de recordatorios.
- Configuración y prueba de SMTP.
- Edición de las plantillas del primer y segundo recordatorio.
- Vista previa y descarga de facturas en PDF.

`FacturasService` consume los endpoints de facturas. `ConfiguracionSmtpService` consume la configuración de correo. `FacturaPdfService` carga jsPDF de forma dinámica y genera el archivo en el navegador; este PDF es un resumen administrativo y no sustituye una factura electrónica fiscal.

En desarrollo separado, `proxy.conf.json` reenvía las rutas `/api` desde Angular hacia la API HTTPS. En modo integrado, el navegador consume `/api` en el mismo origen que sirve la interfaz.

## 4. Flujo completo de una solicitud

Ejemplo al enviar un recordatorio:

```text
Usuario
  ↓
Angular: FacturasService.enviarRecordatorio(id)
  ↓ POST /api/Facturas/{id}/enviar-recordatorio
FacturasController
  ↓
IRecordatorioService / RecordatorioService
  ├─→ IFacturaRepository / MongoDB
  ├─→ IEmailService / servidor SMTP
  └─→ IFacturaRepository.ActualizarAsync
  ↓
ResultadoRecordatorioDto
  ↓
Angular actualiza la tabla y muestra el resultado
```

La API no permite que Angular conozca la cadena de MongoDB ni la contraseña SMTP. Toda operación sensible ocurre en el servidor.

## 5. Configuración local de MongoDB Atlas

### 5.1. Crear el archivo `.env`

Desde la raíz del repositorio:

```bash
cp src/SistemaSuscripciones.Api/.env.example src/SistemaSuscripciones.Api/.env
```

Después, editar `src/SistemaSuscripciones.Api/.env` con esta estructura:

```dotenv
MONGODB__CONNECTIONSTRING=mongodb+srv://<usuario_prueba>:<contrasena_prueba>@cluster0.ocjrq8b.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0
MONGODB__DATABASENAME=SistemaSuscripcionesDb
MONGODB__COLLECTIONNAME=facturas
```

Para la demostración local deben reemplazarse `<usuario_prueba>` y `<contrasena_prueba>` por las credenciales del usuario de base de datos creado en Atlas.

> **Advertencia de seguridad:** estas credenciales son únicamente para pruebas locales. En un proyecto real jamás deben escribirse en el código, README, documentación versionada, capturas, commits ni mensajes. El archivo `.env` ya está incluido en `.gitignore`. En producción se debe usar el administrador de secretos del proveedor de despliegue, restringir las IP autorizadas y aplicar permisos mínimos al usuario de MongoDB.

La contraseña real no se reproduce en este documento deliberadamente. Si una contraseña se compartió previamente en texto plano, debe cambiarse en MongoDB Atlas antes de publicar o desplegar el proyecto.

### 5.2. Prioridad de configuración

.NET convierte el doble guion bajo en una jerarquía de configuración:

```text
MONGODB__CONNECTIONSTRING -> MongoDb:ConnectionString
MONGODB__DATABASENAME    -> MongoDb:DatabaseName
MONGODB__COLLECTIONNAME  -> MongoDb:CollectionName
```

`Program.cs` carga `.env` antes de crear la aplicación y después agrega las variables de entorno a la configuración. Por eso los valores locales sobrescriben los valores vacíos o de ejemplo de `appsettings.json`.

El archivo `.env` se carga únicamente en `Development`. Para producción deben configurarse variables de entorno desde la plataforma de despliegue.

### 5.3. Requisitos de Atlas

- El usuario debe ser un **Database User**, no el correo usado para iniciar sesión en Atlas.
- La IP del equipo debe estar autorizada en **Network Access**.
- `0.0.0.0/0` permite acceso desde cualquier IP y puede servir para una prueba temporal, pero no es recomendable en producción.
- La contraseña debe codificarse en formato URL si contiene caracteres especiales como `@`, `:`, `/`, `?` o `#`.

## 6. Instalación de dependencias

Desde la raíz del repositorio:

```bash
dotnet restore
```

Para Angular:

```bash
cd src/SistemaSuscripciones.Web
npm ci
```

Se usa `npm ci` porque el proyecto contiene `package-lock.json`; así se instalan exactamente las versiones bloqueadas. Si NVM está instalado:

```bash
nvm install
nvm use
```

El archivo `.nvmrc` selecciona la versión de Node recomendada.

## 7. Métodos de ejecución

Todos los comandos siguientes parten de:

```bash
cd /home/sergio/Documentos/repositorios/SistemaSuscripciones
```

### 7.1. API solamente

```bash
dotnet run --project src/SistemaSuscripciones.Api --launch-profile https
```

Direcciones:

- API HTTPS: `https://localhost:7275`
- API HTTP: `http://localhost:5062`
- Swagger: `https://localhost:7275/swagger`

Este modo es apropiado para probar endpoints con Swagger, REST Client o Postman. La raíz `/` no muestra Angular en este perfil.

### 7.2. API y Angular en terminales separadas

Terminal 1, desde la raíz:

```bash
dotnet run --project src/SistemaSuscripciones.Api --launch-profile https
```

Terminal 2:

```bash
cd /home/sergio/Documentos/repositorios/SistemaSuscripciones/src/SistemaSuscripciones.Web
nvm use
npm start
```

Direcciones:

- Angular: `http://localhost:4200`
- Swagger: `https://localhost:7275/swagger`

Este es el modo recomendado durante el desarrollo del frontend porque Angular recompila automáticamente los cambios. Si aparece `ECONNREFUSED 127.0.0.1:7275`, la API no está ejecutándose o no está escuchando en el puerto esperado.

### 7.3. Aplicación integrada en una sola terminal

```bash
./ejecutar-integrado.sh
```

El script:

1. Localiza la raíz del proyecto.
2. Carga NVM si está instalado.
3. Verifica Node.js, npm y .NET.
4. Exige Node 22 o posterior.
5. Ejecuta `npm ci` si todavía no existe `node_modules`.
6. Compila Angular con `npm run build:integrated`.
7. Copia la compilación a `src/SistemaSuscripciones.Api/wwwroot`.
8. Inicia la API con el perfil `integrated`.

La aplicación completa queda en `https://localhost:7275` y Swagger en `https://localhost:7275/swagger`.

Si el sistema pierde el permiso de ejecución del script:

```bash
chmod +x ejecutar-integrado.sh
```

Para detener cualquiera de los servidores se usa `Ctrl+C` en su terminal.

## 8. Compilación y validaciones

Compilar toda la solución .NET:

```bash
dotnet build --maxcpucount:1
```

Compilar Angular para producción:

```bash
cd src/SistemaSuscripciones.Web
npm run build
```

Compilar Angular para que ASP.NET Core lo sirva:

```bash
cd src/SistemaSuscripciones.Web
npm run build:integrated
```

## 9. Pruebas unitarias

### 9.1. Backend .NET

El archivo principal de pruebas es:

```text
tests/SistemaSuscripciones.Tests/RecordatorioServiceTests.cs
```

El proyecto que lo ejecuta es:

```text
tests/SistemaSuscripciones.Tests/SistemaSuscripciones.Tests.csproj
```

Ejecutar todas las pruebas .NET desde la raíz:

```bash
dotnet test --maxcpucount:1
```

Ejecutar solo el proyecto de pruebas:

```bash
dotnet test tests/SistemaSuscripciones.Tests/SistemaSuscripciones.Tests.csproj --maxcpucount:1
```

Ejecutar únicamente la clase `RecordatorioServiceTests`:

```bash
dotnet test tests/SistemaSuscripciones.Tests/SistemaSuscripciones.Tests.csproj \
  --filter "FullyQualifiedName~RecordatorioServiceTests" \
  --maxcpucount:1
```

El archivo `.cs` no se ejecuta directamente. `dotnet test` compila el proyecto, descubre los métodos marcados con `[Fact]` y los ejecuta mediante xUnit.

Actualmente las pruebas verifican:

- Primer recordatorio → segundo recordatorio.
- Segundo recordatorio → desactivado.
- Una factura desactivada no envía ni actualiza.
- Un fallo SMTP no cambia ni persiste el estado.
- Una factura inexistente devuelve un resultado controlado.
- El procesamiento masivo ignora facturas desactivadas.

Las pruebas usan repositorio y correo falsos definidos dentro del propio archivo. Por esa razón no requieren `.env`, MongoDB Atlas ni credenciales SMTP.

Para generar cobertura:

```bash
dotnet test tests/SistemaSuscripciones.Tests/SistemaSuscripciones.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --maxcpucount:1
```

### 9.2. Frontend Angular

El archivo principal de pruebas del frontend es:

```text
src/SistemaSuscripciones.Web/src/app/app.spec.ts
```

Ejecutar las pruebas una vez, sin modo interactivo:

```bash
cd src/SistemaSuscripciones.Web
npm test -- --watch=false
```

Ejecutarlas en modo observación durante el desarrollo:

```bash
npm test
```

Las pruebas Angular se ejecutan con el builder de Angular y Vitest usando `jsdom` como entorno del navegador.

## 10. Archivos de configuración importantes

| Archivo | Responsabilidad |
| --- | --- |
| `SistemaSuscripciones.slnx` | Agrupa proyectos .NET de la solución |
| `src/SistemaSuscripciones.Api/Program.cs` | Punto de entrada y composición del backend |
| `src/SistemaSuscripciones.Api/appsettings.json` | Valores no sensibles y configuración base |
| `src/SistemaSuscripciones.Api/.env` | Secretos locales de MongoDB; no se versiona |
| `src/SistemaSuscripciones.Api/.env.example` | Plantilla segura para crear `.env` |
| `src/SistemaSuscripciones.Api/Properties/launchSettings.json` | Perfiles `http`, `https` e `integrated` |
| `src/SistemaSuscripciones.Web/proxy.conf.json` | Proxy de `/api` durante `ng serve` |
| `src/SistemaSuscripciones.Web/angular.json` | Compilación, pruebas y salida integrada |
| `src/SistemaSuscripciones.Web/package.json` | Dependencias y scripts del frontend |
| `ejecutar-integrado.sh` | Construcción y ejecución con una terminal |
| `.gitignore` | Exclusión de secretos y artefactos generados |

## 11. Configuración SMTP local

En modo `Development`, abrir **Configuración** en la interfaz y completar:

- Servidor SMTP, por ejemplo `smtp.gmail.com`.
- Puerto, normalmente `587` con STARTTLS.
- Correo de autenticación.
- Correo remitente.
- Contraseña de aplicación, no la contraseña normal de la cuenta.
- Opción SSL/TLS.
- Asuntos y mensajes para ambos recordatorios.

Después se puede usar **Enviar correo de prueba**. La configuración permanece tras reiniciar la API porque se guarda cifrada localmente. Si se cambia de equipo o se eliminan las claves de Data Protection, el secreto debe configurarse otra vez.

Esta persistencia local está diseñada para desarrollo. En un despliegue real se deben usar secretos del proveedor y almacenamiento persistente apropiado.

## 12. Solución de problemas frecuentes

### MongoDB devuelve `TimeoutException`

- Confirmar que la IP está permitida en Atlas.
- Verificar que el clúster esté activo.
- Revisar usuario, contraseña y cadena de conexión.
- Reiniciar la API después de modificar `.env`.

### MongoDB indica que la cadena vacía no es válida

- Confirmar que existe `src/SistemaSuscripciones.Api/.env`.
- Ejecutar con el perfil `https` o `integrated`, que establece `Development`.
- Comprobar que la variable se llama exactamente `MONGODB__CONNECTIONSTRING`.

### Angular muestra `ng: not found`

Desde el directorio web:

```bash
nvm use
npm ci
npm start
```

El ejecutable local de Angular se instala dentro de `node_modules` y `npm start` lo encuentra automáticamente.

### El navegador advierte sobre el certificado HTTPS

Es normal con el certificado de desarrollo. Puede confiarse mediante:

```bash
dotnet dev-certs https --trust
```

En algunas distribuciones Linux también puede requerirse aceptar o importar el certificado en el navegador.

## 13. Consideraciones para producción

Antes de desplegar el sistema se debe:

- Rotar cualquier contraseña que se haya compartido o expuesto durante las pruebas.
- Configurar MongoDB y SMTP con secretos del proveedor, nunca mediante archivos versionados.
- Reemplazar `0.0.0.0/0` por una lista de red restrictiva cuando sea posible.
- Crear usuarios de base de datos con permisos mínimos.
- Definir CORS para el dominio real.
- Usar HTTPS con un certificado válido.
- Añadir autenticación y autorización antes de exponer operaciones administrativas.
- Deshabilitar o proteger Swagger según la política del entorno.
- Utilizar un proveedor de correo apto para producción y revisar sus límites.
- Añadir observabilidad, copias de seguridad y una estrategia de recuperación.
- Completar los datos fiscales antes de considerar el PDF una factura legal.

Esta documentación describe el funcionamiento técnico actual del proyecto y su configuración de demostración; no convierte las credenciales de prueba ni la apertura temporal de red en prácticas adecuadas para producción.
