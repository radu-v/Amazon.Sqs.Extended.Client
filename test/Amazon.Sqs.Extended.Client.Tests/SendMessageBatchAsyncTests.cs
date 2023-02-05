using Amazon.S3.Model;
using Amazon.Sqs.Extended.Client.Extensions;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
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
        await PayloadStoreSub.Received(1).StoreOriginalPayloadAsync(LargeMessageBody, Arg.Any<CancellationToken>());
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

        var options = Options.Create(ExtendedClientConfiguration.WithLargePayloadSupportEnabled().WithAlwaysThroughS3(true));

        var client = new AmazonSqsExtendedClient(SqsClientSub, PayloadStoreSub, options, DummyLogger);

        // act
        await client.SendMessageBatchAsync(messageRequest);

        // assert
        Assert.Multiple(async () =>
        {
            await PayloadStoreSub.Received(1).StoreOriginalPayloadAsync(LargeMessageBody, Arg.Any<CancellationToken>());
            await PayloadStoreSub.Received(1).StoreOriginalPayloadAsync(SmallMessageBody, Arg.Any<CancellationToken>());
        });
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
        await PayloadStoreSub.Received(1).StoreOriginalPayloadAsync(LargeMessageBody, Arg.Any<CancellationToken>());
    }
}
