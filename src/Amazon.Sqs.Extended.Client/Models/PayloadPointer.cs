using System.Diagnostics.CodeAnalysis;

namespace Amazon.Sqs.Extended.Client.Models;

[ExcludeFromCodeCoverage]
public record struct PayloadPointer(string BucketName, string Key);