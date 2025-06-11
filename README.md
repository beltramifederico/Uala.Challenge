# Uala Challenge - Microblogging Platform

Una plataforma de microblogging escalable construida con .NET 8, implementando Clean Architecture, Domain-Driven Design (DDD), CQRS y Event-Driven Architecture con optimizaciones de performance avanzadas.

## Arquitectura

### Clean Architecture + DDD + CQRS + Event-Driven
El proyecto sigue los principios de Clean Architecture con una clara separación de responsabilidades:

- **Domain**: Entidades de negocio, eventos de dominio y reglas de dominio
- **Application**: Casos de uso, comandos, queries, interfaces y servicios de aplicación
- **Infrastructure**: Implementación de repositorios, contextos de base de datos, servicios externos y productores/consumidores de eventos
- **API**: Controladores y configuración de la aplicación web

### Event-Driven Architecture con Kafka
- **Event Sourcing**: Los tweets generan eventos que son procesados asincrónicamente
- **Timeline Generation**: Cuando se crea un tweet, se publican eventos en Kafka para generar entradas de timeline para todos los followers
- **Desacoplamiento**: La creación de tweets está desacoplada de la generación de timelines
- **Escalabilidad**: Procesamiento asíncrono permite manejar grandes volúmenes de tweets

### Polyglot Persistence + Caching Strategy
- **PostgreSQL**: Datos relacionales (usuarios, relaciones de seguimiento)
- **MongoDB**: Tweets y **Timeline entries** (optimizado para lecturas masivas y escrituras distribuidas)
- **Redis**: Caché distribuido para timelines y optimización de performance
- **Kafka**: Message broker para eventos de dominio y procesamiento asíncrono

## Stack Tecnológico

### Backend
- **.NET 8** - Framework principal
- **MediatR** - Implementación de CQRS
- **Entity Framework Core** - ORM para PostgreSQL
- **MongoDB Driver** - Cliente para MongoDB
- **StackExchange.Redis** - Cliente para Redis
- **Confluent.Kafka** - Cliente para Apache Kafka
- **Serilog** - Logging estructurado
- **FluentValidation** - Validación de modelos

### Bases de Datos y Messaging
- **PostgreSQL 15** - Base de datos relacional
- **MongoDB 7** - Base de datos de documentos
- **Redis 7** - Caché en memoria
- **Apache Kafka** - Message broker para eventos
- **Zookeeper** - Coordinación para Kafka

### Testing
- **NUnit** - Framework de testing
- **Moq** - Mocking framework

### Containerización
- **Docker** - Containerización de aplicaciones
- **Docker Compose** - Orquestación de servicios

## Optimizaciones de Performance

### 1. **Event-Driven Timeline Generation**
- **Async Processing**: La generación de timelines es asíncrona via Kafka
- **Fan-out Strategy**: Un tweet genera N entradas de timeline (una por follower)
- **Horizontal Scaling**: Los consumers de Kafka pueden escalarse independientemente
- **Fault Tolerance**: Retry logic y dead letter queues para eventos fallidos

### 2. **Caché con Redis** 
- **Timeline Caching**: Los timelines se cachean por 5 minutos
- **Cache Keys**: `timeline:{userId}:page:{pageNumber}:size:{pageSize}`
- **Hit Rate Esperado**: 80-95% para usuarios activos
- **Latencia**: Reducción de 200-500ms a 5-50ms

## Instalación y Configuración

### Prerrequisitos
- Docker y Docker Compose
- .NET 8 SDK (para desarrollo local)

### Opción 1: Docker Compose (Recomendado)
```bash
# Clonar el repositorio
git clone <repository-url>
cd Uala.Challenge

# Levantar toda la infraestructura (PostgreSQL + MongoDB + Redis + Kafka + API)
docker-compose up -d

# Ver logs de la aplicación
docker-compose logs -f api

# Acceder a la API
curl http://localhost:8080/api/users
```

### Opción 2: Desarrollo Local
```bash
# 1. Levantar solo las bases de datos y Kafka
docker-compose up postgres mongodb redis kafka -d

# 2. Configurar connection strings en appsettings.json
{
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Database=UalaChallenge;Username=postgres;Password=yoursecurepassword",
    "MongoConnection": "mongodb://root:rootpassword@localhost:27017",
    "RedisConnection": "localhost:6379",
    "KafkaBootstrapServers": "localhost:9092"
  },
  "MongoDbName": "UalaChallenge"
}

# 3. Ejecutar migraciones y la aplicación
dotnet run --project Uala.Challenge.Api
```

### Comandos Docker Útiles
```bash
# Detener todos los servicios
docker-compose down

# Reiniciar solo la API
docker-compose restart api

# Ver logs de Kafka
docker-compose logs kafka

# Limpiar volúmenes (CUIDADO: elimina datos)
docker-compose down -v
```

## API Endpoints

### Usuarios
- `GET /api/users` - Obtener todos los usuarios
- `POST /api/users/{followerId}/follow/{followedId}` - Seguir usuario
- `DELETE /api/users/{followerId}/unfollow/{followedId}` - Dejar de seguir

### Tweets
- `POST /api/tweets` - Crear tweet
- `GET /api/tweets/timeline/{userId}?pageNumber=1&pageSize=10` - Timeline paginado

### Ejemplo de Uso
```bash
# Crear un tweet
curl -X POST http://localhost:8080/api/tweets \
  -H "Content-Type: application/json" \
  -d '{"userId": "123e4567-e89b-12d3-a456-426614174000", "content": "Mi primer tweet!"}'

# Obtener timeline (con caché)
curl "http://localhost:8080/api/tweets/timeline/123e4567-e89b-12d3-a456-426614174000?pageNumber=1&pageSize=10"
```

## Testing

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar tests con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Tests específicos
dotnet test --filter "GetAllUsersQueryHandlerTests"
```

### Cobertura de Tests
- **GetAllUsersQueryHandler** - Casos exitosos y lista vacía
- **FollowUserCommandHandler** - Validaciones y casos de error
- **UnfollowUserCommandHandler** - Validaciones y casos de error
- **CreateTweetCommandHandler** - Validaciones de contenido

## Consideraciones de Escalabilidad

### Performance Metrics Esperadas
| Métrica | Sin Caché | Con Redis | Mejora |
|---------|-----------|-----------|---------|
| Timeline Query | 200-500ms | 5-50ms | **90% faster** |
| Cache Hit Rate | 0% | 80-95% | **Massive DB load reduction** |
| Concurrent Users | 1K | 100K+ | **100x scalability** |
| DB Connections | High | Low | **Resource efficiency** |

### Estrategias de Escalado

#### Horizontal Scaling
- **API**: Múltiples instancias detrás de un load balancer
- **MongoDB**: Sharding por `UserId` para distribución de tweets
- **Redis**: Redis Cluster para caché distribuido
- **PostgreSQL**: Read replicas para queries de usuarios
- **Kafka**: Múltiples brokers y particiones para eventos

#### Vertical Scaling
- **Memory**: Aumentar memoria para Redis (recomendado: 2-8GB)
- **CPU**: Más cores para procesamiento de agregaciones MongoDB
- **Storage**: SSD para PostgreSQL y MongoDB

#### Optimizaciones Futuras
1. **Fan-out Strategies**:
   - Fan-out on Write para usuarios con pocos followers
   - Fan-out on Read para usuarios con muchos followers
2. **Timeline Materialization**: Pre-computar timelines para usuarios muy activos
3. **CDN**: Para contenido estático y assets
4. **Message Queues**: Para procesamiento asíncrono de notificaciones

### Monitoring y Observabilidad
- **Logs**: Serilog con structured logging
- **Métricas**: Implementar Prometheus + Grafana
- **Health Checks**: Endpoints de salud para cada servicio
- **Alertas**: Configurar alertas para cache hit rate, latencia, errores

## Configuración Avanzada

### Redis Configuration
```yaml
# En docker-compose.yml
redis:
  command: redis-server --appendonly yes --maxmemory 512mb --maxmemory-policy allkeys-lru
```

### MongoDB Indexes
Los índices se configuran automáticamente al iniciar la aplicación:
- `idx_timeline_userid_createdat` - Para queries de timeline
- `idx_createdat_desc` - Para ordenamiento por fecha
- `idx_content_text` - Para búsquedas de texto (futuro)

### Environment Variables
```bash
# Producción
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__PostgresConnection=<production-postgres-url>
ConnectionStrings__MongoConnection=<production-mongo-url>
ConnectionStrings__RedisConnection=<production-redis-url>
ConnectionStrings__KafkaBootstrapServers=<production-kafka-url>
```

## Dockerfile

El proyecto incluye un Dockerfile multi-stage optimizado:
- **Stage 1**: Build con .NET SDK
- **Stage 2**: Runtime con ASP.NET Core
- **Optimizaciones**: Layer caching, non-root user, minimal image size

## Supuestos y Decisiones

Ver [assumptions.txt](./assumptions.txt) para una lista completa de supuestos de negocio y técnicos.

### Decisiones Clave de Performance
- **Cache TTL**: 5 minutos para timelines (balance entre freshness y performance)
- **Pagination**: 10 elementos por defecto para optimizar transferencia
- **Connection Pooling**: Configurado para alta concurrencia
- **Async/Await**: Todas las operaciones I/O son asíncronas

## Próximos Pasos

1. **Implementar métricas de performance** con Prometheus
2. **Agregar health checks** para todos los servicios
3. **Implementar rate limiting** para prevenir abuse
4. **Configurar CI/CD pipeline** con GitHub Actions
5. **Agregar integration tests** con TestContainers
6. **Implementar feature flags** para rollouts graduales

---

**Nota**: Esta implementación está optimizada para demostrar escalabilidad y mejores prácticas. En producción, considera implementar autenticación, autorización, y medidas de seguridad adicionales.