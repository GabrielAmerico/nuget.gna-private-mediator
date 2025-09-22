# Referência da API - GNA Private Mediator

## Namespaces

### `GNA.Private.Mediator.Interfaces`
Contém todas as interfaces principais da biblioteca.

### `GNA.Private.Mediator.Extensions`
Contém métodos de extensão para configuração do DI container.

## Interfaces Principais

### IPrivateMediator

```csharp
public interface IPrivateMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
```

**Métodos:**

#### `Send<TResponse>`
Envia um request e aguarda uma resposta tipada.

**Parâmetros:**
- `request`: Request implementando `IRequest<TResponse>`
- `cancellationToken`: Token de cancelamento (opcional, padrão: `CancellationToken.None`)

**Retorno:** `Task<TResponse>`

**Exceções:**
- `InvalidOperationException`: Quando handler não é encontrado

#### `Publish<TNotification>`
Publica uma notificação para todos os handlers registrados.

**Parâmetros:**
- `notification`: Notificação implementando `INotification`
- `cancellationToken`: Token de cancelamento (opcional, padrão: `CancellationToken.None`)

**Retorno:** `Task`

**Exceções:**
- `InvalidOperationException`: Quando nenhum handler é encontrado

### IRequest<TResponse>

```csharp
public interface IRequest<TResponse> { }
```

Interface marcadora para requests que esperam uma resposta tipada.

### IRequestHandler<TRequest, TResponse>

```csharp
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
```

Interface para handlers que processam requests.

**Métodos:**

#### `Handle`
Processa um request e retorna uma resposta.

**Parâmetros:**
- `request`: Request a ser processado
- `cancellationToken`: Token de cancelamento

**Retorno:** `Task<TResponse>`

### INotification

```csharp
public interface INotification { }
```

Interface marcadora para notificações que não precisam de retorno.

### INotificationHandler<TNotification>

```csharp
public interface INotificationHandler<TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
```

Interface para handlers que processam notificações.

**Métodos:**

#### `Handle`
Processa uma notificação.

**Parâmetros:**
- `notification`: Notificação a ser processada
- `cancellationToken`: Token de cancelamento

**Retorno:** `Task`

## Extensões de Configuração

### IServiceCollectionExtensions

```csharp
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddTransientPrivateMediator(this IServiceCollection services, params object[] args);
    public static IServiceCollection AddScopedPrivateMediator(this IServiceCollection services, params object[] args);
}
```

#### `AddTransientPrivateMediator`

Registra o mediador e todos os handlers com lifetime Transient.

**Parâmetros:**
- `services`: Container de serviços
- `args`: Parâmetros opcionais para filtragem de assemblies:
  - `null` ou vazio: Todos os assemblies
  - `Assembly[]`: Assemblies específicos
  - `string[]`: Prefixos de namespace para filtragem

**Retorno:** `IServiceCollection`

#### `AddScopedPrivateMediator`

Registra o mediador e todos os handlers com lifetime Scoped.

**Parâmetros:**
- `services`: Container de serviços
- `args`: Parâmetros opcionais para filtragem de assemblies (mesmo comportamento do método Transient)

**Retorno:** `IServiceCollection`

## Implementações

### PrivateMediator

```csharp
public class PrivateMediator : IPrivateMediator
{
    public PrivateMediator(IServiceProvider provider);
}
```

Implementação concreta do mediador que utiliza reflection para localizar e executar handlers.

**Construtor:**
- `provider`: Provedor de serviços para resolução de dependências

## Tratamento de Erros

### InvalidOperationException

Lançada quando:
- Nenhum handler é encontrado para um request
- Nenhum handler é encontrado para uma notificação

### ArgumentException

Lançada pelo método de resolução de assemblies quando parâmetros inválidos são fornecidos.

## Considerações de Performance

- Utiliza reflection para descoberta de handlers (cache de reflection pode ser implementado)
- Handlers de notificação são executados sequencialmente
- Auto-descoberta de handlers em todos os assemblies pode impactar performance em aplicações grandes
