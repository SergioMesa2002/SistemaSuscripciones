# Sistema de Suscripciones

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
