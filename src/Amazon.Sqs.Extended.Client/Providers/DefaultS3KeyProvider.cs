namespace Amazon.Sqs.Extended.Client.Providers;

public class DefaultS3KeyProvider : IS3KeyProvider
{
    public string GenerateKey() => Guid.NewGuid().ToString("N");
}