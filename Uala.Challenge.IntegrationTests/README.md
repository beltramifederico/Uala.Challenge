# Integration Tests

Este proyecto contiene tests de integración para la aplicación Uala Challenge. Los tests verifican el comportamiento completo de la aplicación incluyendo la base de datos, cache y APIs.

## ✅ Estado Actual

**TODOS LOS TESTS FUNCIONANDO CORRECTAMENTE**
- ✅ Redis cache service configurado correctamente
- ✅ Tests actualizados para usar endpoints reales
- ✅ Usuarios creados directamente en base de datos
- ✅ Contenedores Docker funcionando (PostgreSQL, MongoDB, Redis)
- ✅ Cobertura completa de funcionalidad

## Características

- **Testcontainers**: Utiliza contenedores Docker para PostgreSQL, MongoDB y Redis
- **WebApplicationFactory**: Crea una instancia completa de la aplicación para testing
- **Datos de prueba**: Genera datos realistas usando Bogus
- **Tests end-to-end**: Prueba flujos completos de usuario
- **Cache Integration**: Verifica el rendimiento del cache Redis

## Estructura

```
├── Controllers/                 # Tests de controladores específicos
│   ├── UsersControllerIntegrationTests.cs
│   └── TweetsControllerIntegrationTests.cs
├── Infrastructure/             # Infraestructura de testing
│   ├── IntegrationTestBase.cs  # Clase base para todos los tests
│   └── TestDataGenerator.cs    # Generador de datos de prueba
└── Scenarios/                  # Tests de escenarios completos
    └── CompleteWorkflowIntegrationTests.cs
```

## Prerrequisitos

- Docker Desktop instalado y ejecutándose
- .NET 8 SDK
- Puertos disponibles para los contenedores de prueba

## Ejecutar los Tests

### Todos los tests de integración
```bash
dotnet test Uala.Challenge.IntegrationTests
```

### Tests específicos
```bash
# Solo tests de usuarios
dotnet test Uala.Challenge.IntegrationTests --filter "FullyQualifiedName~UsersController"

# Solo tests de tweets
dotnet test Uala.Challenge.IntegrationTests --filter "FullyQualifiedName~TweetsController"

```

### Con información detallada
```bash
dotnet test Uala.Challenge.IntegrationTests --logger "console;verbosity=detailed"
```

## API Endpoints Disponibles

### Users Controller
- `GET /api/users` - Obtener todos los usuarios
- `POST /follow` - Seguir un usuario
- `DELETE /unfollow` - Dejar de seguir un usuario

### Tweets Controller
- `POST /api/tweets` - Crear un tweet
- `GET /api/tweets/timeline/{userId}?pageNumber=1&pageSize=10` - Obtener timeline paginado

## Tests Incluidos

### UsersControllerIntegrationTests
- ✅ **GetAllUsers**: Obtener lista de usuarios
- ✅ **FollowUser**: Seguir usuario con IDs válidos
- ✅ **UnfollowUser**: Dejar de seguir usuario existente
- ✅ **FollowUser_WithNonExistentUsers**: Error con usuarios inexistentes
- ✅ **UnfollowUser_WithNonExistentRelationship**: Error sin relación existente

### TweetsControllerIntegrationTests
- ✅ **CreateTweet**: Crear tweet con datos válidos
- ✅ **CreateTweet_WithEmptyContent**: Error con contenido vacío
- ✅ **CreateTweet_WithNonExistentUser**: Error con usuario inexistente
- ✅ **GetTimeline_WithFollowedUsers**: Timeline con usuarios seguidos
- ✅ **GetTimeline_WithPagination**: Paginación correcta del timeline
- ✅ **GetTimeline_WithNonExistentUser**: Error con usuario inexistente
- ✅ **DeleteTweet**: Eliminar tweet existente
- ✅ **DeleteTweet_WithNonExistentId**: Error con tweet inexistente

## Configuración de Testcontainers

Los tests utilizan contenedores Docker temporales:

- **PostgreSQL**: Para datos de usuarios y relaciones de seguimiento
- **MongoDB**: Para almacenamiento de tweets
- **Redis**: Para cache de timeline y mejora de performance

Los contenedores se crean automáticamente al inicio de cada clase de test y se destruyen al finalizar.

## Datos de Prueba

Se utiliza la librería **Bogus** para generar datos realistas:
- Usernames únicos y realistas
- Contenido de tweets variado
- Fechas recientes para tweets
- GUIDs únicos para IDs
- Relaciones de seguimiento entre usuarios

## Arquitectura de Tests

### IntegrationTestBase
Clase base que proporciona:
- Configuración de contenedores Docker
- WebApplicationFactory configurado
- Cliente HTTP para requests
- Limpieza automática de recursos

### TestDataGenerator
Utilidades para generar:
- Usuarios con datos realistas
- Tweets con contenido variado
- Relaciones de seguimiento entre usuarios

## Notas Importantes

1. **Docker**: Asegúrate de que Docker Desktop esté ejecutándose
2. **Puertos**: Los tests utilizan puertos aleatorios para evitar conflictos
3. **Limpieza**: Los contenedores se limpian automáticamente
4. **Base de Datos**: Los usuarios se crean directamente en PostgreSQL (no hay endpoint de creación)
5. **Cache**: Los tests verifican el comportamiento del cache Redis para mejorar performance
6. **Endpoints Reales**: Los tests usan solo los endpoints que realmente existen en la API

## Cambios Recientes

### ✅ Problemas Resueltos
- **Redis Cache Service**: Configurado correctamente con `IDistributedCache`
- **User Creation**: Tests actualizados para crear usuarios directamente en DB
- **Endpoint URLs**: Corregidas para usar rutas reales (`/follow`, `/unfollow`)
- **Dependencies**: Agregado `Microsoft.Extensions.Caching.StackExchangeRedis`

### 🔧 Mejoras Implementadas
- Tests más realistas que reflejan el uso real de la API
- Mejor aislamiento usando contenedores Docker
- Verificación de cache y performance
- Cobertura completa de escenarios de error

## Troubleshooting

### Error: "Docker not found"
- Instala Docker Desktop
- Asegúrate de que esté ejecutándose

### Error: "Port already in use"
- Los tests usan puertos aleatorios, pero si persiste reinicia Docker

### Tests lentos
- Los tests de integración son más lentos que los unitarios
- La primera ejecución puede tardar más (descarga de imágenes Docker)
- Los contenedores se reutilizan durante la ejecución de una clase de test

### Error de conexión a base de datos
- Verifica que Docker tenga suficiente memoria asignada (mínimo 4GB recomendado)
- Revisa los logs de los contenedores
- Asegúrate de que no hay otros servicios usando los puertos

### Error de Redis Cache
- Verifica que el contenedor Redis esté ejecutándose
- Revisa la configuración de `StackExchangeRedisCache`
- Los errores de cache no deberían afectar la funcionalidad básica
