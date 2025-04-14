using System.Diagnostics.CodeAnalysis;

namespace Amazon.Sqs.Extended.Client.Providers;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public class GuidPayloadStoreKeyProvider : IPayloadStoreKeyProvider
{
    public string GenerateKey() => Guid.NewGuid().ToString("N");
}