# Shared Kernel — Fase 0

## Contexto

O Shared Kernel é o primeiro entregável da Fase 0 (Fundação). Ele contém os tipos fundamentais que **todos** os módulos do sistema referenciam livremente, sem criar acoplamento entre módulos. É a camada mais baixa da arquitetura — tudo depende dela, ela não depende de nada (exceto `MediatR.Contracts` para `INotification`).

No DDD, o Shared Kernel é o subconjunto do domínio que múltiplos bounded contexts compartilham. Aqui ele inclui Value Objects comuns (`Money`, `Address`, `Cpf`, etc.), o padrão `Result<T>` para tratamento de erros, a entidade base com campos de auditoria e o contrato de Domain Events.

## Conceitos abordados

| Conceito | Tipo do .NET | Descrição |
|---|---|---|
| `record` | C# 9+ | Usado para Value Objects — imutável por padrão, equality por valor |
| `sealed` | C# | Todas as classes são sealed por padrão (design para composição, não herança) |
| `required` / `init` | C# 9+ | Propriedades obrigatórias no momento da construção, imutáveis depois |
| `ArgumentNullException.ThrowIfNull` | .NET 6+ | Validação de nulidade idiomática |
| `INotification` | MediatR.Contracts | Interface de marcação para eventos do MediatR — `IDomainEvent` a estende |
| `Result<T>` | Padrão próprio | Railway-Oriented Programming — sucesso ou erro, sem exceções |
| `Error` / `ErrorType` | Padrão próprio | Representação estruturada de erros de domínio com código, mensagem e tipo |

## Implementação

### Estrutura de arquivos criados

```
src/Shared/Kernel/
├── PelicanTown.SharedKernel.csproj
├── GlobalUsings.cs
├── ErrorType.cs
├── Error.cs
├── Result.cs
├── ResultT.cs
├── IDomainEvent.cs
├── BaseEntity.cs
├── Money.cs
├── Address.cs
├── Cpf.cs
├── Email.cs
└── Phone.cs
```

### Trechos principais

#### `Result.cs` — padrão Result sem valor

```csharp
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error) { ... }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);
}
```

O construtor é `protected` para forçar o uso das factory methods `Success()` e `Failure()`. Isso impede que alguém crie `new Result(true, someError)` — um Result de sucesso **nunca** pode ter erro, e um de falha **sempre** deve ter.

`Error` é nullable (`Error?`) porque um Result bem-sucedido não tem erro. O `default!` foi substituído por `null` explícito depois que os avisos de nullable reference types apontaram o problema.

#### `ResultT.cs` — Result genérico com `Match`

```csharp
public sealed class Result<T> : Result
{
    public T Value { get; }

    private Result(T value) : base(true, null) { ... }
    private Result(Error error) : base(false, error) { ... }

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value) : onFailure(Error!);
    }

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
```

`Match` é o substituto funcional para `if (result.IsSuccess) ... else ...`. Permite pattern matching sem verificação manual de flags. As validações `ThrowIfNull` evitam `NullReferenceException` nos callbacks.

Os operadores implícitos permitem:
- `Result<int> x = 42;` (sucesso automático)
- `Result<int> x = Error.NotFound("...", "...");` (falha automática)

Isso reduz boilerplate nos handlers do Application layer.

#### `Error.cs` — erros tipados

```csharp
public sealed record Error
{
    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    // ... Conflict, Unauthorized, Forbidden, Domain
}
```

Cada factory method mapeia para um `ErrorType` específico. Isso permite que o middleware de exceções da API converta automaticamente:
- `NotFound` → HTTP 404
- `Validation` → HTTP 400
- `Conflict` → HTTP 409
- `Unauthorized` → HTTP 401
- `Forbidden` → HTTP 403
- `Domain` → HTTP 422

O `Error` é um `record`, portanto imutável e com equality por valor.

#### `BaseEntity.cs` — entidade base

```csharp
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
    protected void MarkUpdated() => UpdatedAt = DateTime.UtcNow;
}
```

- `Id` usa `Guid.NewGuid()` como padrão — sem dependência de banco para gerar IDs
- `CreatedAt` é `protected init` — só pode ser setado na construção, nunca mais
- `UpdatedAt` é `protected set` — só a própria entidade (via `MarkUpdated()`) atualiza
- `_domainEvents` é um campo privado, exposto como `IReadOnlyCollection` — encapsulamento
- `ClearDomainEvents()` é chamado pelo DbContext após disparar os eventos

Campos `CreatedAt`/`UpdatedAt` são planejados para a Fase 35 (LGPD/Auditoria), mas já estruturados aqui para evitar refatoração futura.

#### Value Objects — `Money`, `Cpf`, `Email`, etc.

Todos seguem o mesmo padrão:

```csharp
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(...) { ... }          // Construtor privado

    public static Money Create(...) {    // Factory method com validação
        // validações...
        return new Money(...);
    }
}
```

- Construtor privado + factory method estático (`Create`) — validação no ponto de entrada
- `record` garante imutabilidade
- `sealed` impede herança acidental

`Money` vai além com operadores `+` e `-` sobrecarregados, com validação de mesma moeda.

### Fluxo de execução (uso típico em um handler)

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐     ┌──────────────┐
│  Controller │────▶│   MediatR    │────▶│   Handler   │────▶│  Repository  │
│  recebe DTO │     │  pipeline    │     │  retorna    │     │  retorna     │
│             │     │  behaviors   │     │  Result<T>  │     │  entidade    │
└─────────────┘     └──────────────┘     └──────┬──────┘     └──────────────┘
                                                │
                                    ┌───────────▼───────────┐
                                    │  Result<T>.Match(     │
                                    │    onSuccess: Map →   │
                                    │     200 OK + DTO,     │
                                    │    onFailure: Map →   │
                                    │     400/404/409/etc   │
                                    │  )                    │
                                    └───────────────────────┘
```

## Por que essa abordagem?

### Railway-Oriented Programming com `Result<T>`

Em vez de usar exceções para fluxo de controle (que é caro em .NET e obscurece o caminho feliz), o `Result<T>` torna o resultado de uma operação **explícito no sistema de tipos**. Quem chama um handler **sabe** que pode receber erro e é forçado pelo compilador a lidar com isso (via `Match`).

### Value Objects como `record`

`record` em C# fornece:
- `Equals`/`GetHashCode` por valor (dois `Money(10, "BRL")` são iguais)
- `ToString()` formatado automaticamente
- Imutabilidade (com `init` e `required`)
- Sintaxe concisa (`sealed record Money(decimal Amount, string Currency)`)

Alternativa seria `class` com `IEquatable<T>` implementado manualmente — muito boilerplate.

### `BaseEntity` com `Guid` vs `int`

`Guid` permite gerar IDs no lado da aplicação, sem round-trip no banco. Isso é essencial para:
- Eventos de domínio (precisa do ID antes do `SaveChanges`)
- Mensageria (mensagens referenciam entidades pelo ID)

### Alternativas consideradas

| Alternativa | Por que não usei |
|---|---|
| `Either<TLeft, TRight>` (LanguageExt) | Pacote externo pesado. `Result<T>` caseiro é 30 linhas e resolve 100% dos casos. |
| `FluentResults` (NuGet) | Bom, mas acopla o kernel inteiro a uma lib de terceiros. `Result<T>` próprio é zero-dependency. |
| Exceções para erros de domínio | Performance (stack trace + throw é ~10x mais lento que retornar um objeto), e perde-se a tipagem do erro. |
| `int Id` auto-increment | Depende do banco. `Guid` é gerado na aplicação e funciona com event sourcing, cache e mensageria. |
| `class` para Value Objects | `record` reduz boilerplate de equality em ~80% e expressa intent ("sou um valor, não uma identidade"). |

## Pontos de atenção

1. **`Error` como nome de tipo**: o analisador CA1716 reclama que `Error` conflita com palavra reservada em VB.NET. Como este projeto é C# puro, o warning foi suprimido. Se um dia isso virar NuGet público, renomear para `DomainError` seria prudente.

2. **`WarningsNotAsErrors`**: o `Directory.Build.props` degrada regras CA específicas de erro para warning. As regras suprimidas (CA1000, CA2225, CA1305, etc.) são estilísticas e não afetam segurança ou correção. Em produção, o ideal é revisar cada uma caso a caso, mas para um kernel compartilhado é overkill.

3. **`BaseEntity.DomainEvents`**: o campo `_domainEvents` é `List<IDomainEvent>`, não `ConcurrentBag`. Isso é seguro porque o EF Core processa eventos de forma síncrona dentro do `SaveChanges`. Se no futuro houver dispatch paralelo, trocar para coleção thread-safe.

4. **`CreatedAt` no construtor**: `DateTime.UtcNow` é capturado no momento da construção do objeto em memória, não no `SaveChanges`. Se a entidade ficar horas em memória antes de persistir, `CreatedAt` não reflete o momento real da inserção. Para a Fase 35, considere usar um interceptor do EF Core que sobrescreve na primeira inserção.

5. **`Money` não tem conversão de moeda**: o operador `+` falha se moedas diferentes. Isso é intencional — conversão de moeda depende de taxa de câmbio externa, que viria de um serviço de domínio, não do Value Object.

## Próximo tópico

[02-docker-compose.md](./02-docker-compose.md) — Infraestrutura local com Docker Compose.
