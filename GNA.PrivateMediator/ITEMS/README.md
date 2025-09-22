# GNA Private Mediator

[![NuGet Version](https://img.shields.io/nuget/v/GNA.Private.Mediator.svg)](https://www.nuget.org/packages/GNA.Private.Mediator/)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Uma biblioteca .NET que implementa o padrão Mediator para desacoplar componentes de aplicações, promovendo baixo acoplamento e alta coesão através de comunicação indireta entre objetos.

## 🚀 Características

- **Padrão Mediator**: Implementação completa do padrão Mediator para comunicação desacoplada
- **Request/Response**: Suporte a operações com retorno tipado
- **Notifications**: Suporte a notificações fire-and-forget com múltiplos handlers
- **Dependency Injection**: Integração nativa com Microsoft.Extensions.DependencyInjection
- **Auto-descoberta**: Descoberta automática de handlers em assemblies
- **Async/Await**: Suporte completo a operações assíncronas
- **Type Safety**: Tipagem forte com genéricos
- **Flexível**: Configuração flexível de lifetimes e filtragem de assemblies

## 📦 Instalação

```bash
Install-Package GNA.Private.Mediator
```

```bash
dotnet add package GNA.Private.Mediator
```

## ⚡ Início Rápido

### 1. Configuração

```csharp
using GNA.Private.Mediator.Extensions;

// Configuração básica
services.AddTransientPrivateMediator();

// Ou para aplicações web (recomendado)
services.AddScopedPrivateMediator();
```

### 2. Definindo um Request

```csharp
using GNA.Private.Mediator.Interfaces;

public class GetUserRequest : IRequest<UserDto>
{
    public int UserId { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### 3. Implementando o Handler

```csharp
public class GetUserHandler : IRequestHandler<GetUserRequest, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }
}
```

### 4. Usando o Mediator

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
        var user = await _mediator.Send(request);
        return Ok(user);
    }
}
```

## 🔔 Notificações

### Definindo uma Notificação

```csharp
public class UserCreatedNotification : INotification
{
    public int UserId { get; set; }
    public string UserEmail { get; set; }
    public string UserName { get; set; }
}
```

### Implementando Handlers

```csharp
// Handler para envio de email
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(notification.UserEmail);
    }
}

// Handler para auditoria
public class AuditUserCreationHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _auditService.LogUserCreationAsync(notification.UserId);
    }
}
```

### Publicando Notificações

```csharp
public class UserService
{
    private readonly IPrivateMediator _mediator;

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var user = await _userRepository.AddAsync(request);
        
        // Publicar notificação - todos os handlers serão executados
        var notification = new UserCreatedNotification
        {
            UserId = user.Id,
            UserEmail = user.Email,
            UserName = user.Name
        };

        await _mediator.Publish(notification);
        
        return user;
    }
}
```

## 📚 Documentação Completa

A documentação completa está disponível na pasta [`docs/`](./docs/):

### 📖 [Visão Geral da Arquitetura](./docs/architecture-overview.md)
- Padrão Mediator e seus benefícios
- Componentes principais da biblioteca
- Fluxo de funcionamento
- Vantagens da arquitetura

### 🔧 [Referência da API](./docs/api-reference.md)
- Documentação completa de todas as interfaces
- Parâmetros e retornos de métodos
- Tratamento de erros
- Considerações de performance

### 💡 [Exemplos de Uso](./docs/usage-examples.md)
- Exemplos práticos de implementação
- Configurações avançadas
- Padrões de uso recomendados
- Testes unitários e de integração

### 🎯 [Boas Práticas](./docs/best-practices.md)
- Estrutura de projeto recomendada
- Convenções de nomenclatura
- Design de requests e responses
- Handlers eficientes e testáveis

### 🔍 [Solução de Problemas](./docs/troubleshooting.md)
- Problemas comuns e suas soluções
- Debugging e diagnóstico
- Checklist de troubleshooting
- Informações para reportar bugs

### ⚙️ [Especificações Técnicas](./docs/technical-specifications.md)
- Análise detalhada do código
- Métricas de performance
- Limitações conhecidas
- Roadmap de melhorias

## 🛠️ Configurações Avançadas

### Filtragem de Assemblies

```csharp
// Por prefixo de namespace (recomendado para performance)
services.AddScopedPrivateMediator("MyApp.Features", "MyApp.Services");

// Por assemblies específicos
services.AddScopedPrivateMediator(
    typeof(GetUserHandler).Assembly,
    typeof(CreateOrderHandler).Assembly
);
```

### Diferentes Lifetimes

```csharp
// Transient (padrão)
services.AddTransientPrivateMediator();

// Scoped (recomendado para aplicações web)
services.AddScopedPrivateMediator();

// Singleton (não recomendado)
services.AddSingleton<IPrivateMediator, PrivateMediator>();
```

## 🧪 Testes

### Teste de Handler

```csharp
[Test]
public async Task GetUserHandler_ShouldReturnUser_WhenUserExists()
{
    // Arrange
    var mockRepository = new Mock<IUserRepository>();
    mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new User { Id = 1, Name = "João" });

    var handler = new GetUserHandler(mockRepository.Object);
    var request = new GetUserRequest { UserId = 1 };

    // Act
    var result = await handler.Handle(request, CancellationToken.None);

    // Assert
    Assert.That(result.Name, Is.EqualTo("João"));
}
```

### Teste de Integração

```csharp
[Test]
public async Task Mediator_ShouldExecuteHandler_WhenRequestIsSent()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddTransientPrivateMediator();
    services.AddTransient<IUserRepository, InMemoryUserRepository>();
    
    var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IPrivateMediator>();

    // Act
    var request = new GetUserRequest { UserId = 1 };
    var result = await mediator.Send(request);

    // Assert
    Assert.That(result, Is.Not.Null);
}
```

## 🤝 Contribuindo

Contribuições são bem-vindas! Por favor, leia as diretrizes de contribuição antes de enviar pull requests.

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## 👨‍💻 Autor

**Gabriel Américo** - [GitHub](https://github.com/seuusuario)

## 🙏 Agradecimentos

- Inspirado no padrão Mediator e implementações como MediatR
- Comunidade .NET por feedback e sugestões
- Contribuidores e usuários da biblioteca

---

## 📊 Estatísticas

- **Versão Atual**: 1.0.1
- **Framework**: .NET Standard 2.1
- **Dependências**: Microsoft.Extensions.DependencyInjection 6.0.0
- **Licença**: MIT
- **Status**: Estável

Para mais informações, consulte a [documentação completa](./docs/).
