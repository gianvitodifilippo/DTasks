# Motivation

## Async/Await in C#: A Brief History

The advent of `async`/`await` in C# 5 revolutionized the way developers write asynchronous code.
Before its introduction, different patterns were employed, such as callback-based approaches like `BeginInvoke`/`EndInvoke`, `IAsyncResult`, manual thread and thread pool management, or event-driven programming.
These approaches often resulted in code that was difficult to read, write, and maintain.

With `async`/`await` and `Task`, asynchronous programming in C# became more intuitive.
`Task` represents an operation that completes in the future and was designed with the concept of continuation baked into it: a `Task` can be used to attach one or more continuations that execute when the asynchronous operation completes.

For a deeper dive into this topic, it's recommended to read Stephen Toub's excellent article [How Async/Await Really Works](https://devblogs.microsoft.com/dotnet/how-async-await-really-works/).

## DTask: An Awaitable for Distributed Async Operations

Today, we see similar problems and patterns emerging in distributed computing as those that existed in local asynchronous programming before `async`/`await`.
Distributed architectures, microservices, and cloud infrastructure require handling long-running operations across different machines.
For example, Redis solves caching in distributed scenarios, while message queues facilitate communication between services.

Asynchronous programming is one of these challenges.
Many distributed systems rely on callback-based interactions (e.g., webhooks), request-reply messaging via queues, or external storage mechanisms like Redis to coordinate execution across multiple processes.

However, the `Task` object is inherently *local*: it represents an asynchronous operation within the same process or machine.
A `Task` resides in heap memory and cannot simply be stored in a distributed environment.
As a result, developers are forced to revert to manually writing callback-based code, handling state persistence, and managing event-driven execution, just as they did before `async`/`await`.

DTasks aims to solve this problem by extending the benefits of `async`/`await` to distributed asynchronous operations.
Its core principles are:

1. **Seamless integration with async/await** – Just as `Task` made asynchronous code feel synchronous, DTasks aims to make distributed asynchronous operations feel like local ones.
2. **A dedicated type for distributed operations** – `Task` was designed to represent a locally asynchronous operation: let it keep doing only that. Instead, let's introduce a new type to represent distributed asynchronous operations.
3. **A familiar API** – Distributed programming is hard. `Task` and `async`/`await` are rooted into most developers' background; DTasks should come with an API that minimizes the learning curve, making adoption easier.

This leads to the definition of the `DTask` type: a distributed task.
`DTask` closely resembles `Task` in both name and API: if `await Task.Delay(...)` suspends an operation and later resumes it, potentially on a different thread, then `await DTask.Delay(...)` suspends an operation and later resumes it, potentially on a different machine.

## Comparison with Microsoft DTFx

Microsoft's Durable Task Framework (DTFx) is similar in its intent: writing long running persistent workflows in C# using the async/await capabilities.

DTasks differs in key ways:

- **Dedicated type for distributed operations** – DTFx treats distributed tasks as standard `Task` objects and requires an orchestration context to track them.
- **No mandatory orchestration context** – With DTasks, you can await any `DTask` (or even `Task`) directly without needing an orchestration context, making the API feel more natural for developers familiar with `async`/`await`.
- **Flexible execution models** – While DTFx primarily follows a replay-based execution model, DTasks is not inherently tied to one approach. It can support snapshot-based persistence or a replay model depending on the implementation.

By taking an approach that aligns more closely with native async programming, DTasks aims to reduce friction for developers while still providing a robust framework for distributed workflows.
