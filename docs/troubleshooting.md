# Solução de Problemas - GNA Private Mediator

## Problemas Comuns e Soluções

### 1. Handler Não Encontrado

#### Sintoma
```
InvalidOperationException: Handler for GetUserRequest not found.
```

#### Causas Possíveis
- Handler não foi registrado no container de DI
- Handler não implementa a interface correta
- Assembly não foi incluído na configuração

#### Soluções

**Verificar Implementação do Handler:**
```csharp
// ✅ Correto
public class GetUserHandler : IRequestHandler<GetUserRequest, UserDto>
{
    public async Task<UserDto> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        // Implementação
    }
}

// ❌ Incorreto - não implementa a interface
public class GetUserHandler
{
    public async Task<UserDto> Process(GetUserRequest request)
    {
        // Implementação
    }
}
```

**Verificar Configuração do DI:**
```csharp
// ✅ Correto - inclui o assembly onde estão os handlers
services.AddTransientPrivateMediator(typeof(GetUserHandler).Assembly);

// ✅ Correto - usa prefixo de namespace
services.AddTransientPrivateMediator("MyApp.Features");

// ❌ Incorreto - não inclui o assembly dos handlers
services.AddTransientPrivateMediator(); // Só funciona se handlers estão no mesmo assembly
```

**Verificar Namespace:**
```csharp
// ✅ Correto - namespace correto
namespace GNA.Private.Mediator.Interfaces
{
    public class GetUserHandler : IRequestHandler<GetUserRequest, UserDto>
    {
        // ...
    }
}
```

### 2. Problemas de Lifetime do DI

#### Sintoma
```
ObjectDisposedException: Cannot access a disposed object
```

#### Causas Possíveis
- Incompatibilidade entre lifetime do mediator e handlers
- Uso de serviços Scoped em contexto Transient

#### Soluções

**Configuração Consistente:**
```csharp
// ✅ Correto - mesmo lifetime para mediator e handlers
services.AddScopedPrivateMediator();

// ❌ Incorreto - lifetimes diferentes
services.AddScoped<IPrivateMediator, PrivateMediator>();
services.AddTransientPrivateMediator(); // Conflito de lifetimes
```

**Para Aplicações Web (Recomendado):**
```csharp
services.AddScopedPrivateMediator();
```

**Para Aplicações Console/Desktop:**
```csharp
services.AddTransientPrivateMediator();
```

### 3. Problemas de Reflection

#### Sintoma
```
TargetInvocationException: Exception has been thrown by the target of an invocation
```

#### Causas Possíveis
- Erro interno no handler sendo executado via reflection
- Problema com tipos genéricos

#### Soluções

**Verificar Exceptions Internas:**
```csharp
public class GetUserHandler : IRequestHandler<GetUserRequest, UserDto>
{
    public async Task<UserDto> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Sua lógica aqui
            return await ProcessRequest(request, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log da exception interna
            _logger.LogError(ex, "Erro interno no handler");
            throw; // Re-throw para que seja visível
        }
    }
}
```

**Verificar Tipos Genéricos:**
```csharp
// ✅ Correto - tipos bem definidos
public class GetUserRequest : IRequest<UserDto>
public class UserDto { /* propriedades */ }

// ❌ Incorreto - tipos ambíguos
public class GetUserRequest : IRequest<object>
```

### 4. Problemas de Performance

#### Sintoma
- Aplicação lenta ao inicializar
- Tempo de resposta alto para requests

#### Causas Possíveis
- Auto-descoberta em muitos assemblies
- Falta de cache de reflection
- Handlers pesados sendo executados

#### Soluções

**Filtragem de Assemblies:**
```csharp
// ✅ Otimizado - assemblies específicos
services.AddScopedPrivateMediator(
    typeof(MyApp.Features.Users.Handlers.GetUserHandler).Assembly,
    typeof(MyApp.Features.Orders.Handlers.CreateOrderHandler).Assembly
);

// ✅ Otimizado - prefixo de namespace
services.AddScopedPrivateMediator("MyApp.Features");

// ❌ Lento - todos os assemblies
services.AddScopedPrivateMediator(); // Varre todos os assemblies
```

**Implementar Cache de Reflection:**
```csharp
public class CachedPrivateMediator : IPrivateMediator
{
    private readonly IPrivateMediator _inner;
    private static readonly ConcurrentDictionary<Type, Type> HandlerCache = new();

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var handlerType = HandlerCache.GetOrAdd(request.GetType(), type =>
            typeof(IRequestHandler<,>).MakeGenericType(type, typeof(TResponse)));

        // Usar handlerType cached...
        return await _inner.Send(request, cancellationToken);
    }
}
```

### 5. Problemas de Notificações

#### Sintoma
```
InvalidOperationException: Handler for UserCreatedNotification not found.
```

#### Causas Possíveis
- Notification handlers não registrados
- Múltiplos handlers para mesma notificação

#### Soluções

**Verificar Registration de Notification Handlers:**
```csharp
// ✅ Correto - handlers registrados automaticamente
services.AddScopedPrivateMediator();

public class SendEmailHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Implementação
    }
}

public class AuditHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Implementação
    }
}
```

**Verificar Execução de Múltiplos Handlers:**
```csharp
// ✅ Correto - todos os handlers são executados
await _mediator.Publish(new UserCreatedNotification { UserId = 1 });

// Isso executará:
// - SendEmailHandler
// - AuditHandler
// - Qualquer outro handler registrado
```

### 6. Problemas de Testes

#### Sintoma
- Testes falhando com "Handler not found"
- Mock não funcionando corretamente

#### Soluções

**Configuração de Testes:**
```csharp
[TestFixture]
public class MediatorTests
{
    private ServiceProvider _serviceProvider;
    private IPrivateMediator _mediator;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Registrar o mediator
        services.AddTransientPrivateMediator();
        
        // Registrar mocks
        services.AddTransient<IUserRepository, MockUserRepository>();
        services.AddTransient<ILogger<GetUserHandler>, NullLogger<GetUserHandler>>();
        
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IPrivateMediator>();
    }

    [Test]
    public async Task Send_ShouldWork_WithProperSetup()
    {
        // Arrange
        var request = new GetUserRequest { UserId = 1 };

        // Act
        var result = await _mediator.Send(request);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }
}
```

### 7. Problemas de Configuração em Produção

#### Sintoma
- Funciona em desenvolvimento, falha em produção
- Diferentes comportamentos entre ambientes

#### Soluções

**Configuração por Ambiente:**
```csharp
public static class MediatorConfiguration
{
    public static IServiceCollection AddMediator(this IServiceCollection services, IConfiguration configuration)
    {
        var environment = configuration["Environment"];
        
        switch (environment)
        {
            case "Development":
                services.AddTransientPrivateMediator("MyApp"); // Mais assemblies para debug
                break;
            case "Production":
                services.AddScopedPrivateMediator("MyApp.Features"); // Apenas features necessárias
                break;
            default:
                services.AddScopedPrivateMediator();
                break;
        }

        return services;
    }
}
```

**Verificar Assemblies em Produção:**
```csharp
// Debug helper para verificar quais assemblies estão sendo carregados
public static void LogLoadedAssemblies(ILogger logger)
{
    var assemblies = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.FullName))
        .OrderBy(a => a.FullName);

    foreach (var assembly in assemblies)
    {
        logger.LogInformation("Assembly: {AssemblyName}", assembly.FullName);
    }
}
```

## Debugging e Diagnóstico

### Logging Detalhado

```csharp
public class DebugPrivateMediator : IPrivateMediator
{
    private readonly IPrivateMediator _inner;
    private readonly ILogger<DebugPrivateMediator> _logger;

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enviando request: {RequestType}", typeof(TRequest).Name);
        
        try
        {
            var result = await _inner.Send(request, cancellationToken);
            _logger.LogInformation("Request processado com sucesso: {RequestType}", typeof(TRequest).Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar request: {RequestType}", typeof(TRequest).Name);
            throw;
        }
    }
}
```

### Verificação de Registros

```csharp
public static void VerifyMediatorSetup(IServiceProvider serviceProvider)
{
    var mediator = serviceProvider.GetService<IPrivateMediator>();
    if (mediator == null)
    {
        throw new InvalidOperationException("IPrivateMediator não foi registrado");
    }

    // Verificar se alguns handlers estão registrados
    var userHandler = serviceProvider.GetService<IRequestHandler<GetUserRequest, UserDto>>();
    if (userHandler == null)
    {
        throw new InvalidOperationException("GetUserHandler não foi registrado");
    }
}
```

## Checklist de Troubleshooting

### Antes de Reportar um Bug

- [ ] Verificar se o handler implementa a interface correta
- [ ] Verificar se o handler foi registrado no DI container
- [ ] Verificar se o assembly foi incluído na configuração
- [ ] Verificar se os lifetimes são consistentes
- [ ] Verificar se não há exceptions internas no handler
- [ ] Verificar se os tipos genéricos estão corretos
- [ ] Verificar se a configuração está sendo aplicada corretamente

### Informações para Debug

Ao reportar problemas, inclua:

1. **Versão da biblioteca**
2. **Configuração do DI container**
3. **Código do handler que está falhando**
4. **Stack trace completo**
5. **Ambiente (Development/Production)**
6. **Tipo de aplicação (Web/Console/Desktop)**
7. **Lista de assemblies carregados (se relevante)**
