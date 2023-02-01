using Amazon.S3;
using Amazon.S3.Model;
using NSubstitute;
using NSubstitute.Core;

namespace Amazon.Sqs.Extended.Client.Tests;

public static class S3SubstituteExtensions
{
    public static void ReceivedGetObjectAsyncCalls(this IAmazonS3 amazonS3Substitute, int expectedCallCount,
        string s3BucketName, string s3Key)
    {
        var calls = GetReceivedGetObjectAsyncCalls(amazonS3Substitute, s3BucketName, s3Key);

        Assert.That(calls, Has.Exactly(expectedCallCount).Items);
    }

    public static void DidNotReceiveGetObjectAsyncCalls(this IAmazonS3 amazonS3Substitute, string s3BucketName, string s3Key)
    {
        var calls = GetReceivedGetObjectAsyncCalls(amazonS3Substitute, s3BucketName, s3Key);

        Assert.That(calls, Is.Empty);
    }

    public static void DidNotReceiveGetObjectAsyncCallsWithAnyArgs(this IAmazonS3 amazonS3Substitute)
    {
        var calls = GetReceivedGetObjectAsyncCallsWithAnyArgs(amazonS3Substitute);

        Assert.That(calls, Is.Empty);
    }

    static IEnumerable<ICall> GetReceivedGetObjectAsyncCallsWithAnyArgs(IAmazonS3 amazonS3Substitute)
    {
        return amazonS3Substitute.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IAmazonS3.GetObjectAsync))
            .Where(c =>
            {
                var args = c.GetArguments();
                return args[0] is GetObjectRequest || args[0] is string && args[1] is string;
            });
    }

    static IEnumerable<ICall> GetReceivedGetObjectAsyncCalls(IAmazonS3 amazonS3Substitute, string s3BucketName, string s3Key)
    {
        return amazonS3Substitute.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IAmazonS3.GetObjectAsync))
            .Where(c =>
            {
                var args = c.GetArguments();
                return args[0] is GetObjectRequest r && r.BucketName == s3BucketName && r.Key == s3Key
                       || (string?)args[0] == s3BucketName && (string?)args[1] == s3Key;
            });
    }
}