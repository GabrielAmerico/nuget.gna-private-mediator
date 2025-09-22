# Arquitetura da Biblioteca GNA Private Mediator

## Visão Geral

A **GNA Private Mediator** é uma biblioteca .NET que implementa o padrão Mediator para desacoplar componentes de aplicações. Ela permite comunicação indireta entre objetos através de um mediador centralizado, promovendo baixo acoplamento e alta coesão.

## Padrão Mediator

O padrão Mediator define como um conjunto de objetos interagem. Em vez de objetos se comunicarem diretamente uns com os outros, eles se comunicam através de um objeto mediador centralizado. Isso reduz as dependências entre objetos comunicantes, tornando-os mais reutilizáveis e mais fáceis de modificar.

## Componentes Principais

### 1. Core Interfaces

#### `IPrivateMediator`
Interface principal que define os contratos para:
- `Send<TResponse>()` - Envio de requests com retorno
- `Publish<TNotification>()` - Publicação de notificações sem retorno

#### `IRequest<TResponse>`
Interface marcadora para requests que esperam uma resposta do tipo `TResponse`.

#### `IRequestHandler<TRequest, TResponse>`
Interface para handlers que processam requests e retornam respostas tipadas.

#### `INotification`
Interface marcadora para notificações que não precisam de retorno.

#### `INotificationHandler<TNotification>`
Interface para handlers que processam notificações.

### 2. Implementação

#### `PrivateMediator`
Implementação concreta do mediador que:
- Utiliza reflection para localizar handlers dinamicamente
- Gerencia o ciclo de vida dos handlers através do `IServiceProvider`
- Suporta operações assíncronas com `CancellationToken`

### 3. Extensões de Configuração

#### `IServiceCollectionExtensions`
Fornece métodos de extensão para configurar o DI container:
- `AddTransientPrivateMediator()` - Registra handlers como Transient
- `AddScopedPrivateMediator()` - Registra handlers como Scoped
- Auto-descoberta de handlers em assemblies especificados

## Fluxo de Funcionamento

### Request/Response
1. Cliente chama `mediator.Send<TResponse>(request)`
2. Mediator localiza o handler apropriado usando reflection
3. Handler processa o request e retorna resposta tipada
4. Mediator retorna a resposta ao cliente

### Notifications
1. Cliente chama `mediator.Publish<TNotification>(notification)`
2. Mediator localiza todos os handlers registrados para o tipo de notificação
3. Executa todos os handlers encontrados em paralelo
4. Não retorna valor (fire-and-forget)

## Vantagens da Arquitetura

1. **Desacoplamento**: Objetos não precisam conhecer uns aos outros
2. **Flexibilidade**: Fácil adição de novos handlers
3. **Testabilidade**: Handlers podem ser testados isoladamente
4. **Manutenibilidade**: Mudanças em um handler não afetam outros
5. **Reutilização**: Handlers podem ser reutilizados em diferentes contextos

## Tecnologias Utilizadas

- **.NET Standard 2.1**: Compatibilidade com múltiplas plataformas
- **Microsoft.Extensions.DependencyInjection**: Container de injeção de dependência
- **Reflection**: Descoberta dinâmica de tipos e métodos
- **Async/Await**: Suporte completo a operações assíncronas
