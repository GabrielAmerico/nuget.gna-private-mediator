# Boas Práticas - GNA Private Mediator

## Estrutura de Projeto

### Organização de Arquivos

```
MyApp/
├── Features/                    # Agrupamento por funcionalidade
│   ├── Users/
│   │   ├── Requests/
│   │   │   ├── GetUserRequest.cs
│   │   │   └── CreateUserRequest.cs
│   │   ├── Handlers/
│   │   │   ├── GetUserHandler.cs
│   │   │   └── CreateUserHandler.cs
│   │   └── Notifications/
│   │       └── UserCreatedNotification.cs
│   └── Orders/
│       ├── Requests/
│       ├── Handlers/
│       └── Notifications/
├── Shared/                      # Código compartilhado
│   ├── Common/
│   ├── Exceptions/
│   └── Validators/
└── Infrastructure/              # Infraestrutura
    ├── Persistence/
    ├── Services/
    └── NotificationHandlers/
```

### Convenções de Nomenclatura

```csharp
// Requests
public class GetUserByIdRequest : IRequest<UserDto>
public class CreateUserRequest : IRequest<int>
public class UpdateUserRequest : IRequest<bool>

// Handlers
public class GetUserByIdHandler : IRequestHandler<GetUserByIdRequest, UserDto>
public class CreateUserHandler : IRequestHandler<CreateUserRequest, int>
public class UpdateUserHandler : IRequestHandler<UpdateUserRequest, bool>

// Notifications
public class UserCreatedNotification : INotification
public class UserUpdatedNotification : INotification
public class UserDeletedNotification : INotification

// Notification Handlers
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
public class AuditUserCreationHandler : INotificationHandler<UserCreatedNotification>
public class InvalidateUserCacheHandler : INotificationHandler<UserUpdatedNotification>
```

## Design de Requests e Responses

### Requests Bem Estruturados

```csharp
// ✅ Bom: Request específico e bem definido
public class GetUserByIdRequest : IRequest<UserDto>
{
    public int UserId { get; set; }
    public bool IncludeInactive { get; set; } = false;
}

// ✅ Bom: Request com validação
public class CreateUserRequest : IRequest<int>
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }

    [Phone]
    public string? Phone { get; set; }
}

// ❌ Ruim: Request muito genérico
public class GenericRequest : IRequest<object>
{
    public string Action { get; set; }
    public object Data { get; set; }
}
```

### Responses Consistentes

```csharp
// ✅ Bom: DTOs específicos
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ✅ Bom: Result wrapper para casos complexos
public class CreateUserResult
{
    public int UserId { get; set; }
    public bool EmailSent { get; set; }
    public List<string> Warnings { get; set; } = new();
}

// ❌ Ruim: Retorno de entidades de domínio diretamente
public class User // Entidade de domínio não deve ser exposta
{
    // Propriedades internas...
}
```

## Handlers Eficientes

### Estrutura Padrão de Handler

```csharp
public class GetUserHandler : IRequestHandler<GetUserRequest, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserHandler> _logger;
    private readonly IMapper _mapper;

    public GetUserHandler(
        IUserRepository userRepository,
        ILogger<GetUserHandler> logger,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        // 1. Log da entrada
        _logger.LogInformation("Buscando usuário com ID: {UserId}", request.UserId);

        try
        {
            // 2. Validação de negócio (se necessário)
            if (request.UserId <= 0)
                throw new ArgumentException("User ID deve ser maior que zero");

            // 3. Operação principal
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            
            if (user == null)
            {
                _logger.LogWarning("Usuário não encontrado: {UserId}", request.UserId);
                throw new UserNotFoundException($"Usuário com ID {request.UserId} não encontrado");
            }

            // 4. Transformação de dados
            var result = _mapper.Map<UserDto>(user);

            // 5. Log de saída
            _logger.LogInformation("Usuário encontrado: {UserId}", request.UserId);
            
            return result;
        }
        catch (UserNotFoundException)
        {
            throw; // Re-throw exceptions específicas
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar usuário {UserId}", request.UserId);
            throw new InvalidOperationException($"Erro interno ao buscar usuário: {ex.Message}", ex);
        }
    }
}
```

### Princípios SOLID em Handlers

```csharp
// ✅ Single Responsibility: Um handler, uma responsabilidade
public class CreateUserHandler : IRequestHandler<CreateUserRequest, int>
{
    // Apenas cria usuário, não envia email nem faz auditoria
}

// ✅ Dependency Inversion: Depende de abstrações
public class UserHandler : IRequestHandler<UserRequest, UserDto>
{
    private readonly IUserRepository _userRepository; // Interface, não implementação
    private readonly IEmailService _emailService;     // Interface, não implementação
}

// ✅ Open/Closed: Extensível através de notificações
public class CreateUserHandler : IRequestHandler<CreateUserRequest, int>
{
    private readonly IUserRepository _userRepository;
    private readonly IPrivateMediator _mediator;

    public async Task<int> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.AddAsync(request, cancellationToken);
        
        // Notificação permite extensão sem modificar o handler
        await _mediator.Publish(new UserCreatedNotification { UserId = user.Id }, cancellationToken);
        
        return user.Id;
    }
}
```

## Notifications e Eventos

### Design de Notifications

```csharp
// ✅ Bom: Notification específica e imutável
public class UserCreatedNotification : INotification
{
    public int UserId { get; init; }          // Init-only para imutabilidade
    public string UserEmail { get; init; }
    public string UserName { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; }
}

// ✅ Bom: Notification com contexto suficiente
public class OrderCompletedNotification : INotification
{
    public int OrderId { get; init; }
    public int CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public List<OrderItem> Items { get; init; }
    public DateTime CompletedAt { get; init; }
}

// ❌ Ruim: Notification muito genérica
public class GenericNotification : INotification
{
    public string Type { get; set; }
    public object Data { get; set; }
}
```

### Notification Handlers Idempotentes

```csharp
// ✅ Bom: Handler idempotente
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IEmailService _emailService;
    private readonly IEmailLogRepository _emailLogRepository;

    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Verificar se email já foi enviado
        var alreadySent = await _emailLogRepository.WasEmailSentAsync(
            notification.UserId, 
            "Welcome", 
            cancellationToken);

        if (alreadySent)
        {
            _logger.LogInformation("Email de boas-vindas já enviado para usuário {UserId}", notification.UserId);
            return;
        }

        // Enviar email
        await _emailService.SendWelcomeEmailAsync(notification.UserEmail, cancellationToken);
        
        // Registrar envio
        await _emailLogRepository.LogEmailSentAsync(
            notification.UserId, 
            "Welcome", 
            DateTime.UtcNow, 
            cancellationToken);
    }
}
```

## Configuração e Performance

### Configuração Otimizada

```csharp
// ✅ Bom: Configuração específica por ambiente
public static class MediatorConfiguration
{
    public static IServiceCollection AddMediator(this IServiceCollection services, IConfiguration configuration)
    {
        var environment = configuration["Environment"];
        
        if (environment == "Development")
        {
            // Desenvolvimento: Transient para facilitar debug
            services.AddTransientPrivateMediator("MyApp");
        }
        else
        {
            // Produção: Scoped para melhor performance
            services.AddScopedPrivateMediator("MyApp");
        }

        return services;
    }
}

// ✅ Bom: Filtragem de assemblies para performance
services.AddScopedPrivateMediator(
    typeof(MyApp.Features.Users.Handlers.GetUserHandler).Assembly,
    typeof(MyApp.Features.Orders.Handlers.CreateOrderHandler).Assembly
);
```

### Cache de Reflection

```csharp
// ✅ Bom: Cache de tipos para melhor performance
public static class HandlerTypeCache
{
    private static readonly ConcurrentDictionary<Type, Type> RequestHandlerCache = new();
    private static readonly ConcurrentDictionary<Type, Type[]> NotificationHandlerCache = new();

    public static Type GetHandlerType(Type requestType, Type responseType)
    {
        var key = (requestType, responseType);
        return RequestHandlerCache.GetOrAdd(key, k =>
        {
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(k.requestType, k.responseType);
            return handlerType;
        });
    }

    public static Type[] GetNotificationHandlerTypes(Type notificationType)
    {
        return NotificationHandlerCache.GetOrAdd(notificationType, type =>
        {
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(type);
            return new[] { handlerType };
        });
    }
}
```

## Tratamento de Erros

### Hierarquia de Exceptions

```csharp
// ✅ Bom: Exceptions específicas do domínio
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(int userId) : base($"Usuário com ID {userId} não encontrado") { }
}

public class UserAlreadyExistsException : DomainException
{
    public UserAlreadyExistsException(string email) : base($"Usuário com email {email} já existe") { }
}

public class InvalidUserOperationException : DomainException
{
    public InvalidUserOperationException(string operation) : base($"Operação inválida: {operation}") { }
}
```

### Error Handling em Handlers

```csharp
public class CreateUserHandler : IRequestHandler<CreateUserRequest, int>
{
    public async Task<int> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validações de negócio
            if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
                throw new UserAlreadyExistsException(request.Email);

            // Operação principal
            var user = await _userRepository.AddAsync(request, cancellationToken);
            return user.Id;
        }
        catch (DomainException)
        {
            throw; // Re-throw exceptions de domínio
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno ao criar usuário {Email}", request.Email);
            throw new InvalidOperationException("Erro interno do sistema", ex);
        }
    }
}
```

## Testes

### Estrutura de Testes

```csharp
[TestFixture]
public class GetUserHandlerTests
{
    private Mock<IUserRepository> _mockRepository;
    private Mock<ILogger<GetUserHandler>> _mockLogger;
    private Mock<IMapper> _mockMapper;
    private GetUserHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<GetUserHandler>>();
        _mockMapper = new Mock<IMapper>();
        _handler = new GetUserHandler(_mockRepository.Object, _mockLogger.Object, _mockMapper.Object);
    }

    [Test]
    public async Task Handle_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Name = "João" };
        var userDto = new UserDto { Id = userId, Name = "João" };
        
        _mockRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(user);
        _mockMapper.Setup(m => m.Map<UserDto>(user))
                  .Returns(userDto);

        var request = new GetUserRequest { UserId = userId };

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.That(result.Id, Is.EqualTo(userId));
        Assert.That(result.Name, Is.EqualTo("João"));
    }

    [Test]
    public async Task Handle_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        var userId = 999;
        _mockRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((User)null);

        var request = new GetUserRequest { UserId = userId };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UserNotFoundException>(() => 
            _handler.Handle(request, CancellationToken.None));
        
        Assert.That(ex.Message, Does.Contain("não encontrado"));
    }
}
```

### Testes de Integração

```csharp
[TestFixture]
public class MediatorIntegrationTests
{
    private ServiceProvider _serviceProvider;
    private IPrivateMediator _mediator;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddTransientPrivateMediator();
        services.AddTransient<IUserRepository, InMemoryUserRepository>();
        services.AddTransient<ILogger<GetUserHandler>, NullLogger<GetUserHandler>>();
        
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IPrivateMediator>();
    }

    [Test]
    public async Task Send_ShouldExecuteHandler_WhenRequestIsValid()
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

## Monitoramento e Logging

### Logging Estruturado

```csharp
public class CreateUserHandler : IRequestHandler<CreateUserRequest, int>
{
    private readonly ILogger<CreateUserHandler> _logger;

    public async Task<int> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["UserId"] = request.UserId,
            ["Email"] = request.Email,
            ["Operation"] = "CreateUser"
        });

        _logger.LogInformation("Iniciando criação de usuário");
        
        try
        {
            var result = await ProcessUserCreation(request, cancellationToken);
            
            _logger.LogInformation("Usuário criado com sucesso com ID: {UserId}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar usuário");
            throw;
        }
    }
}
```

### Métricas de Performance

```csharp
public class MetricsHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IRequestHandler<TRequest, TResponse> _inner;
    private readonly IMetrics _metrics;

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _inner.Handle(request, cancellationToken);
            
            _metrics.IncrementCounter("mediator.requests.success", new Dictionary<string, string>
            {
                ["handler"] = typeof(TRequest).Name
            });
            
            return result;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter("mediator.requests.error", new Dictionary<string, string>
            {
                ["handler"] = typeof(TRequest).Name,
                ["error"] = ex.GetType().Name
            });
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordHistogram("mediator.requests.duration", stopwatch.ElapsedMilliseconds);
        }
    }
}
```
