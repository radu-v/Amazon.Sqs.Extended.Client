using Amazon.Sqs.Extended.Client.Models;

namespace Amazon.Sqs.Extended.Client
{
    public interface IPayloadStore
    {
        Task DeletePayloadFromS3Async(PayloadPointer payloadPointer, CancellationToken cancellationToken = new());

        Task<string> ReadPayloadFromS3Async(PayloadPointer payloadPointer, CancellationToken cancellationToken = new());

        Task<PayloadPointer> StoreOriginalPayloadAsync(string payloadBody, CancellationToken cancellationToken = new());

        Task<PayloadPointer> StoreOriginalPayloadAsync(string payloadBody, string payloadKey, CancellationToken cancellationToken = new());
    }
}
