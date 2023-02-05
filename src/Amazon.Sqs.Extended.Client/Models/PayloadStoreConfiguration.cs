using Amazon.S3;
using System.Diagnostics.CodeAnalysis;

namespace Amazon.Sqs.Extended.Client.Models
{
    [ExcludeFromCodeCoverage(Justification = "Model")]
    public record PayloadStoreConfiguration(string BucketName)
    {
        public S3CannedACL S3CannedAcl { get; init; } = S3CannedACL.BucketOwnerFullControl;
    }
}
