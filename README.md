# DTasks: a zero-abstraction durable task library

[![build](https://github.com/GianvitoDifilippo/DTasks/actions/workflows/ci.yml/badge.svg)](https://github.com/GianvitoDifilippo/DTasks/actions?query=workflow%3ACI)
[![NuGet](http://img.shields.io/nuget/vpre/DTasks.svg?label=NuGet)](https://www.nuget.org/packages/DTasks/)

**DTasks** is a _zero-abstraction_ library for writing long-running workflows across service boundaries.
It is built directly on top of the C# **async pattern**, allowing users to write persistent and distributed operations using `async`/`await` with no more effort than adding a "D" to your "Tasks."

By the way, if you're wondering what the **"D"** in **DTasks** stands for, you choose!
It can be "durable", "distributed", or "damn, that's amazing!"

## ❓ Why DTasks?

**DTasks** aims to simplify the way we write long-running asynchronous workflows in distributed environments.
Before `async`/`await`, writing asynchronous code meant starting an operation that assumed a result would be returned *in the future*, requiring a callback to continue execution once the result was available. This eventually leads, when composing multiple asynchronous operations, to the so-called _callback hell_.
The `async`/`await` pattern and the `Task` type elegantly solved the problem for _locally_ asynchronous operations, meaning operations that happen asynchronously on the same machine.
Today, we see the same callback pattern in distributed environments in many forms: _request-reply_, _webhooks_, and so on.
**DTasks** introduces a new awaitable type, `DTask`, designed for these distributed and durable operations.
It integrates seamlessly with the `async` and `await` keywords and serves as the distributed counterpart to `Task`.
**DTasks** is an alternative to Microsoft's **Durable Task Framework (DTFx)**, which it is inspired by, but it follows a different approach:

1. **Dedicated async types** - Async methods representing a durable operation (d-async methods) will return `DTask` instead of `Task`. You can still await a normal `Task` inside a d-async method (but not the other way around 🙂).
2. **Runs anywhere** - Works in ASP.NET Core and any environment that implements the **DTasks** pattern.
3. **No deterministic code constraints** – Workflows can contain non-deterministic code, and execution is not replayed after every yield.

### Why "D"?

- **Distributed** – A d-async method may start on one machine and continue execution on another, just like a regular async method may start on one thread and resume on a different one.
- **Durable** – The execution state of a d-async method is persisted, ensuring it survives failures and restarts without losing progress.
- **Damn, that's amazing!** – Because you get all of this without any new major abstractions — just pure `async`/`await` magic!

## 🚀 Getting started

**DTasks** is an *experimental* library, currently in its pre-alpha stage.
At this point, it's *not ready* to be integrated in any .NET project, as some of its features are still in development.
Despite this, you can already explore its capabilities through the provided [samples](./samples), so it's recommended that you check them out and, if you're curious, play with them a bit.

**DTasks** was made public at this early stage to gather feedback from the community.
And also because getting it this far took a lot of effort!
Any thoughts or contributions are more than welcome.

## 🧪 Samples

Currently, **DTasks** integrates with ASP.NET Core only, but it has the potential to run in other environments in the future (for example, Azure Functions).
There are two samples that showcase how you can write asynchronous endpoints. Async endpoints are HTTP methods that may not immediately return the operation's result but instead provide a way for the client to be notified or retrieve the result later.

1. [**ApprovalWorkflow**](./samples/ApprovalWorkflow) - The client (another .NET Web API) sends an HTTP request to start an approval process and provides a callback URL to get notified when the request has been reviewed. The server sends an email to the approver and waits for their input before notifying the client using the provided URL.
2. [**DocumentProcessing**](./samples/DocumentProcessing) - The client (a web app) sends a request to process a document that it previously uploaded to a storage account. The server verifies that the document exists and immediately responds, then starts long-running processing and notifies the client when it's done using WebSockets.

To run these samples, please refer to the relevant README files.

Here's a basic example of how you can define a durable workflow using **DTasks**, taken from the ApprovalWorkflow sample:

```csharp
using DTasks;

public class AsyncEndpoints(
    ApproverRepository repository, // Dependency injection is supported
    ApprovalService service)
{
    [HttpPost("approvals")] // Map your async endpoint using ASP.NET Core attributes
    public async DTask<IResult> NewApproval(NewApprovalRequest request) // Returning DTask allows you to write async endpoints
    {
        // Look up approver's email in the database
        string? email = await repository.GetEmailByIdAsync(request.ApproverId); // Await any "normal" Tasks, including those that are non-deterministic or have side effects
        if (email is null)
            return Results.BadRequest("Invalid approver id");

        // Send approval/rejection links to the approver
        DTask<ApprovalResult> approvalTask = service.SendApprovalRequestDAsync(request.Details, email); // This DTask will complete when the approver clicks on either link
        DTask timeout = DTask.Delay(TimeSpan.FromDays(7)); // Give them 7 days to review the request

        // Wait until either the approver reviews the request or the timeout expires
        DTask winner = await DTask.WhenAny(timeout, approvalTask); // DTasks has an API similar to Task, including DTask.WhenAny, DTask.WhenAll, etc.
        ApprovalResult result = winner == timeout
            ? ApprovalResult.Reject
            : approvalTask.Result; // "DTask.Result" can be accessed only if the DTask was awaited, otherwise it throws

        return AsyncResults.Success(result); // If you need to return IResult, use AsyncResults.Success to terminate the workflow
    }
}
```

This method will automatically persist its state and resume execution even after failures or restarts.

## 👩‍💻 Contributing

Contributions are deeply appreciated!
At this stage, the focus is on carefully designing the APIs and internals to ensure that the library is extensible and flexible.
Feel free to open issues to discuss design choices, propose features, or just to ask questions.
If you want to reach out, [drop me an email](mailto:gianvito.difilippo@gmail.com) - I’d love to hear your thoughts!

## ⚖️ License

**DTasks** is licensed under [Apache 2.0](LICENSE).
