using Azure.Storage.Blobs;
using DTasks;
using DTasks.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Documents;

public class AsyncEndpoints(BlobContainerClient containerClient)
{
    [HttpPost("/process-document/{documentId}")]
    public async DTask<IResult> ProcessDocument(string documentId)
    {
        bool exists = await DocumentExistsAsync(documentId);
        if (!exists)
            return Results.NotFound();

        await DTask.Yield();

        // Simulating the processing with a delay
        await Task.Delay(TimeSpan.FromSeconds(15));

        return AsyncResults.Success();
    }

    private async Task<bool> DocumentExistsAsync(string documentId)
    {
        string blobName = $"{documentId}.pdf";
        BlobClient blobClient = containerClient.GetBlobClient(blobName);
        return await blobClient.ExistsAsync();
    }
}
