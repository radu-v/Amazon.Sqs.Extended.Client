using System.Diagnostics.CodeAnalysis;

namespace Amazon.Sqs.Extended.Client.Models;

[ExcludeFromCodeCoverage(Justification = "Model")]
public record struct PayloadS3Pointer(string S3BucketName, string S3Key);