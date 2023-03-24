using Amazon.Sqs.Extended.Client.Models;

namespace Amazon.Sqs.Extended.Client
{
    public interface IPayloadStore : IDisposable
    {
        Task DeletePayloadAsync(PayloadPointer payloadPointer, CancellationToken cancellationToken = new());

        Task<string> ReadPayloadAsync(PayloadPointer payloadPointer, CancellationToken cancellationToken = new());

        Task<PayloadPointer> StorePayloadAsync(string payloadBody, CancellationToken cancellationToken = new());

        Task<PayloadPointer> StorePayloadAsync(string payloadBody, string payloadKey, CancellationToken cancellationToken = new());
    }
}
