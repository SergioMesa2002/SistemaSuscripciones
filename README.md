# Sistema de Suscripciones

La explicación completa de la arquitectura, dependencias, configuración, métodos de ejecución y pruebas está disponible en [DOCUMENTACION_TECNICA.md](DOCUMENTACION_TECNICA.md).

## Configuración local de MongoDB Atlas

La API carga variables desde `.env` únicamente cuando se ejecuta en el entorno `Development`. Las variables de entorno se agregan después de `appsettings.json`, por lo que sus valores tienen prioridad. Si el sistema operativo ya define una variable, ese valor tampoco será reemplazado por `.env`.

1. Copia `src/SistemaSuscripciones.Api/.env.example` como `src/SistemaSuscripciones.Api/.env`.
2. Pega la cadena de conexión de MongoDB Atlas solamente en `MONGODB__CONNECTIONSTRING` dentro de `.env`.
3. Sustituye los marcadores de usuario y contraseña por las credenciales de tu usuario de base de datos de Atlas.
4. Nunca subas `.env` a GitHub. El archivo está excluido mediante `.gitignore`.

La jerarquía de configuración de .NET convierte el separador doble `__` en `:`, por lo que estas variables alimentan directamente `MongoDbSettings`:

```text
MONGODB__CONNECTIONSTRING  -> MongoDb:ConnectionString
MONGODB__DATABASENAME     -> MongoDb:DatabaseName
MONGODB__COLLECTIONNAME   -> MongoDb:CollectionName
```

Los estados se escriben y se muestran en la API por nombre: `PrimerRecordatorio`, `SegundoRecordatorio` y `Desactivado`.

Para iniciar la API en desarrollo:

```bash
dotnet run --project src/SistemaSuscripciones.Api --launch-profile https
```

Swagger estará disponible en `https://localhost:7275/swagger`.

## Interfaz Angular

El frontend se encuentra en `src/SistemaSuscripciones.Web` y utiliza Angular 22. Incluye:

- Resumen de facturas, valores pendientes y recordatorios activos.
- Búsqueda por factura, socio, correo, plan o estado.
- Creación, edición y eliminación de facturas.
- Vista previa y descarga de cada factura como documento PDF.
- Envío de un recordatorio y procesamiento de todos los pendientes.
- Validación de formularios, mensajes de error y diseño adaptable a móviles.

Angular 22 requiere Node.js 22.22.3 o una versión compatible posterior. Si utilizas NVM, el archivo `.nvmrc` permite seleccionar la versión recomendada:

```bash
cd src/SistemaSuscripciones.Web
nvm install
nvm use
npm ci
npm start
```

La interfaz estará disponible en `http://localhost:4200`. El servidor de desarrollo utiliza `proxy.conf.json` para enviar las solicitudes `/api` a `https://localhost:7275`, por lo que la API debe estar ejecutándose con el perfil HTTPS en otra terminal.

## Modos de ejecución

El proyecto conserva tres formas de iniciarse:

1. Solo la API, disponible en `https://localhost:7275`:

   ```bash
   dotnet run --project src/SistemaSuscripciones.Api --launch-profile https
   ```

2. Solo Angular, disponible en `http://localhost:4200` y esperando que la API esté activa:

   ```bash
   cd src/SistemaSuscripciones.Web
   npm start
   ```

3. Aplicación integrada con un único comando y un único servidor:

   ```bash
   ./ejecutar-integrado.sh
   ```

El tercer modo compila Angular en `SistemaSuscripciones.Api/wwwroot` y luego inicia ASP.NET Core. La aplicación completa queda disponible en `https://localhost:7275` y Swagger continúa en `https://localhost:7275/swagger`.

Para validar el frontend:

```bash
npm run build
npm test -- --watch=false
```

Las acciones de recordatorio requieren una configuración SMTP válida en la API. El CRUD de facturas puede utilizarse con MongoDB sin configurar SMTP.

### Prueba local de SMTP

En el entorno `Development`, la opción **Configuración** de Angular permite guardar el servidor, puerto, usuario, remitente, contraseña de aplicación y uso de SSL/TLS. También permite enviar un correo de prueba sin modificar facturas.

La contraseña:

- Se envía únicamente a la API local.
- Se cifra mediante ASP.NET Core Data Protection antes de escribirse en `.smtp-settings.enc`.
- No se devuelve al navegador ni se guarda en MongoDB o Git.

La configuración se recupera automáticamente después de reiniciar la API y no depende de variables SMTP del archivo `.env`. El archivo cifrado está excluido de Git y solo puede descifrarse con las claves locales de Data Protection. Los endpoints de configuración responden únicamente en `Development`; en producción deben usarse secretos del proveedor de despliegue.

La misma pantalla permite personalizar el asunto y el contenido del primer y segundo recordatorio. Las plantillas admiten las variables `{NombreSocio}`, `{CorreoSocio}`, `{PlanSuscripcion}`, `{NumeroFactura}`, `{Valor}` y `{FechaVencimiento}`; la API las reemplaza con los datos de cada factura antes del envío.
