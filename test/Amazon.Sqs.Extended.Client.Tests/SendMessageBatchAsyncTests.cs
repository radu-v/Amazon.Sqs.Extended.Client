using Amazon.S3.Model;
using Amazon.Sqs.Extended.Client.Extensions;
using Amazon.SQS.Model;
using NSubstitute;

namespace Amazon.Sqs.Extended.Client.Tests;

public class SendMessageBatchAsyncTests : AmazonSqsExtendedClientTestsBase
{
    [Test]
    public async Task PayloadIsStoredInS3WhenSendingLargeMessage()
    {
        // arrange
        var messageRequest = new SendMessageBatchRequest(SqsQueueUrl, new List<SendMessageBatchRequestEntry>
        {
            new("1", LargeMessageBody)
        });

        // act
        await ExtendedSqsWithLargePayloadEnabled.SendMessageBatchAsync(messageRequest);

        // assert
        await S3Sub.Received(1).PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PayloadIsNotStoredInS3WhenSendingSmallMessage()
    {
        // arrange
        var messageRequest = new SendMessageBatchRequest(SqsQueueUrl, new List<SendMessageBatchRequestEntry>
        {
            new("1", SmallMessageBody)
        });

        // act
        await ExtendedSqsWithLargePayloadEnabled.SendMessageBatchAsync(messageRequest);

        // assert
        await S3Sub.DidNotReceive().PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PayloadIsNotStoredInS3WhenSendingLargeMessageAndLargePayloadSupportIsDisabled()
    {
        // arrange
        var messageRequest = new SendMessageBatchRequest(SqsQueueUrl, new List<SendMessageBatchRequestEntry>
        {
            new("1", SmallMessageBody),
            new("2", LargeMessageBody)
        });

        // act
        await ExtendedSqsWithLargePayloadDisabled.SendMessageBatchAsync(messageRequest);

        // assert
        await S3Sub.DidNotReceive().PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PayloadIsStoredInS3WhenAlwaysThroughS3IsEnabled()
    {
        // arrange
        var messageRequest = new SendMessageBatchRequest(SqsQueueUrl, new List<SendMessageBatchRequestEntry>
        {
            new("1", SmallMessageBody),
            new("2", LargeMessageBody)
        });

        var client = new AmazonSqsExtendedClient(SqsClientSub,
            ExtendedClientConfiguration.WithLargePayloadSupportEnabled(S3Sub, S3BucketName).WithAlwaysThroughS3(true));

        // act
        await client.SendMessageBatchAsync(messageRequest);

        // assert
        await S3Sub.Received(2).PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OnlyTheLargePayloadIsStoredInS3()
    {
        // arrange
        var messageRequest = new SendMessageBatchRequest(SqsQueueUrl, new List<SendMessageBatchRequestEntry>
        {
            new("1", SmallMessageBody),
            new("2", LargeMessageBody)
        });

        // act
        await ExtendedSqsWithLargePayloadEnabled.SendMessageBatchAsync(messageRequest);

        // assert
        await S3Sub.Received(1).PutObjectAsync(Arg.Is<PutObjectRequest>(p => p.ContentBody == LargeMessageBody),
            Arg.Any<CancellationToken>());
    }
}