using System.Diagnostics.CodeAnalysis;
using Amazon.Sqs.Extended.Client.Models;

namespace Amazon.Sqs.Extended.Client.Extensions;

[ExcludeFromCodeCoverage]
public static class ExtendedClientConfigurationExtensions
{
    public static ExtendedClientConfiguration WithLargePayloadSupportEnabled(this ExtendedClientConfiguration config,
        bool cleanupS3Payload)
        => config with
        {
            LargePayloadSupport = true,
            CleanupPayload = cleanupS3Payload
        };

    public static ExtendedClientConfiguration WithLargePayloadSupportEnabled(this ExtendedClientConfiguration config)
        => config with
        {
            LargePayloadSupport = true,
            CleanupPayload = true
        };

    public static ExtendedClientConfiguration WithLargePayloadSupportDisabled(this ExtendedClientConfiguration config)
        => config with
        {
            LargePayloadSupport = false
        };

    public static ExtendedClientConfiguration WithAlwaysThroughS3(this ExtendedClientConfiguration config,
        bool alwaysThroughS3)
        => config with
        {
            AlwaysThroughS3 = alwaysThroughS3
        };

    public static ExtendedClientConfiguration WithPayloadSizeThreshold(this ExtendedClientConfiguration config,
        int payloadSizeThreshold)
        => config with
        {
            PayloadSizeThreshold = payloadSizeThreshold
        };
}