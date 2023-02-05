using System.Diagnostics.CodeAnalysis;

namespace Amazon.Sqs.Extended.Client.Models;

[ExcludeFromCodeCoverage(Justification = "Model")]
public record struct PayloadPointer(string BucketName, string Key);