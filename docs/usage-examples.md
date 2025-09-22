# Exemplos de Uso - GNA Private Mediator

## Configuração Básica

### 1. Configuração do Container de DI

```csharp
// Program.cs ou Startup.cs
using GNA.Private.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Opção 1: Registro Transient (padrão)
services.AddTransientPrivateMediator();

// Opção 2: Registro Scoped (para aplicações web)
services.AddScopedPrivateMediator();

// Opção 3: Filtragem por prefixo de namespace
services.AddTransientPrivateMediator("MyApp", "MyApp.Services");

// Opção 4: Assemblies específicos
services.AddTransientPrivateMediator(typeof(MyHandler).Assembly);

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IPrivateMediator>();
```

## Exemplos de Request/Response

### 1. Definindo um Request

```csharp
using GNA.Private.Mediator.Interfaces;

public class GetUserRequest : IRequest<UserDto>
{
    public int UserId { get; set; }
    public string Email { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### 2. Implementando o Handler

```csharp
using GNA.Private.Mediator.Interfaces;
using Microsoft.Extensions.Logging;

public class GetUserHandler : IRequestHandler<GetUserRequest, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserHandler> _logger;

    public GetUserHandler(IUserRepository userRepository, ILogger<GetUserHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserDto> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Buscando usuário com ID: {UserId}", request.UserId);
        
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null)
        {
            _logger.LogWarning("Usuário não encontrado: {UserId}", request.UserId);
            throw new UserNotFoundException($"Usuário com ID {request.UserId} não encontrado");
        }

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }
}
```

### 3. Usando o Request

```csharp
public class UserController : ControllerBase
{
    private readonly IPrivateMediator _mediator;

    public UserController(IPrivateMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var request = new GetUserRequest { UserId = id };
        
        try
        {
            var user = await _mediator.Send(request);
            return Ok(user);
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
```

## Exemplos de Notifications

### 1. Definindo uma Notification

```csharp
using GNA.Private.Mediator.Interfaces;

public class UserCreatedNotification : INotification
{
    public int UserId { get; set; }
    public string UserEmail { get; set; }
    public string UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 2. Implementando Handlers de Notificação

```csharp
// Handler para envio de email de boas-vindas
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendWelcomeEmailHandler> _logger;

    public SendWelcomeEmailHandler(IEmailService emailService, ILogger<SendWelcomeEmailHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Enviando email de boas-vindas para: {Email}", notification.UserEmail);
        
        var emailTemplate = new WelcomeEmailTemplate
        {
            UserName = notification.UserName,
            UserEmail = notification.UserEmail
        };

        await _emailService.SendAsync(emailTemplate, cancellationToken);
    }
}

// Handler para auditoria
public class AuditUserCreationHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditUserCreationHandler> _logger;

    public AuditUserCreationHandler(IAuditService auditService, ILogger<AuditUserCreationHandler> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registrando auditoria para criação de usuário: {UserId}", notification.UserId);
        
        var auditEntry = new AuditEntry
        {
            EntityType = "User",
            EntityId = notification.UserId,
            Action = "Created",
            Timestamp = notification.CreatedAt,
            Details = $"User created: {notification.UserName} ({notification.UserEmail})"
        };

        await _auditService.LogAsync(auditEntry, cancellationToken);
    }
}

// Handler para cache
public class InvalidateUserCacheHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<InvalidateUserCacheHandler> _logger;

    public InvalidateUserCacheHandler(ICacheService cacheService, ILogger<InvalidateUserCacheHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invalidando cache para novo usuário: {UserId}", notification.UserId);
        
        // Invalidar caches relacionados a usuários
        await _cacheService.RemoveAsync("users:list", cancellationToken);
        await _cacheService.RemoveAsync("users:count", cancellationToken);
    }
}
```

### 3. Publicando a Notificação

```csharp
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPrivateMediator _mediator;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, IPrivateMediator mediator, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Criando novo usuário: {Email}", request.Email);

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Publicar notificação - todos os handlers serão executados
        var notification = new UserCreatedNotification
        {
            UserId = user.Id,
            UserEmail = user.Email,
            UserName = user.Name,
            CreatedAt = user.CreatedAt
        };

        await _mediator.Publish(notification, cancellationToken);

        _logger.LogInformation("Usuário criado com sucesso: {UserId}", user.Id);
        return user;
    }
}
```

## Exemplo Completo de Aplicação

### Estrutura de Pastas

```
MyApp/
├── Requests/
│   ├── GetUserRequest.cs
│   ├── CreateUserRequest.cs
│   └── UpdateUserRequest.cs
├── Handlers/
│   ├── GetUserHandler.cs
│   ├── CreateUserHandler.cs
│   └── UpdateUserHandler.cs
├── Notifications/
│   ├── UserCreatedNotification.cs
│   ├── UserUpdatedNotification.cs
│   └── UserDeletedNotification.cs
├── NotificationHandlers/
│   ├── EmailHandlers/
│   ├── AuditHandlers/
│   └── CacheHandlers/
└── Services/
    ├── UserService.cs
    └── EmailService.cs
```

### Configuração no Program.cs

```csharp
using GNA.Private.Mediator.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços
builder.Services.AddControllers();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICacheService, CacheService>();

// Configurar Mediator
builder.Services.AddScopedPrivateMediator();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
```

## Tratamento de Erros

### Custom Exception

```csharp
public class UserNotFoundException : Exception
{
    public UserNotFoundException(string message) : base(message) { }
    public UserNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
```

### Handler com Tratamento de Erro

```csharp
public class GetUserHandler : IRequestHandler<GetUserRequest, UserDto>
{
    private readonly IUserRepository _userRepository;

    public async Task<UserDto> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            
            if (user == null)
                throw new UserNotFoundException($"Usuário com ID {request.UserId} não encontrado");

            return MapToDto(user);
        }
        catch (UserNotFoundException)
        {
            throw; // Re-throw exceptions específicas
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar usuário: {ex.Message}", ex);
        }
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email
    };
}
```

## Testes Unitários

### Teste de Handler

```csharp
[Test]
public async Task GetUserHandler_ShouldReturnUser_WhenUserExists()
{
    // Arrange
    var userId = 1;
    var user = new User { Id = userId, Name = "João", Email = "joao@test.com" };
    
    var mockRepository = new Mock<IUserRepository>();
    mockRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(user);

    var handler = new GetUserHandler(mockRepository.Object, Mock.Of<ILogger<GetUserHandler>>());
    var request = new GetUserRequest { UserId = userId };

    // Act
    var result = await handler.Handle(request, CancellationToken.None);

    // Assert
    Assert.That(result.Id, Is.EqualTo(userId));
    Assert.That(result.Name, Is.EqualTo("João"));
    Assert.That(result.Email, Is.EqualTo("joao@test.com"));
}
```

### Teste de Notification Handler

```csharp
[Test]
public async Task SendWelcomeEmailHandler_ShouldSendEmail_WhenNotificationReceived()
{
    // Arrange
    var mockEmailService = new Mock<IEmailService>();
    var handler = new SendWelcomeEmailHandler(mockEmailService.Object, Mock.Of<ILogger<SendWelcomeEmailHandler>>());
    
    var notification = new UserCreatedNotification
    {
        UserId = 1,
        UserName = "João",
        UserEmail = "joao@test.com",
        CreatedAt = DateTime.UtcNow
    };

    // Act
    await handler.Handle(notification, CancellationToken.None);

    // Assert
    mockEmailService.Verify(s => s.SendAsync(It.IsAny<WelcomeEmailTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
}
```
