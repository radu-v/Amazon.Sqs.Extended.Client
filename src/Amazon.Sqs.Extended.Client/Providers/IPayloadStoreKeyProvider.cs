namespace Amazon.Sqs.Extended.Client.Providers;

public interface IPayloadStoreKeyProvider
{
    string GenerateKey();
}