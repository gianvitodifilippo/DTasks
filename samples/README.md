# Samples

Currently, **DTasks** integrates with ASP.NET Core only, but it has the potential to run in other environments in the future (for example, Azure Functions).
There are two samples that showcase how you can write asynchronous endpoints. Async endpoints are HTTP methods that may not immediately return the operation's result but instead provide a way for the client to be notified or retrieve the result later.

1. [**ApprovalWorkflow**](./ApprovalWorkflow) - The client (another .NET Web API) sends an HTTP request to start an approval process and provides a callback URL to get notified when the request has been reviewed. The server sends an email to the approver and waits for their input before notifying the client using the provided URL.
2. [**DocumentProcessing**](./DocumentProcessing) - The client (a web app) sends a request to process a document that it previously uploaded to a storage account. The server verifies that the document exists and immediately responds, then starts long-running processing and notifies the client when it's done using WebSockets.
