using Amazon.Runtime;
using Amazon.Sqs.Extended.Client.Extensions;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Amazon.Sqs.Extended.Client.Tests.AmazonSqsExtendedClient;

public class SendMessageAsyncTests : AmazonSqsExtendedClientTestsBase
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void ThrowsWhenMessageBodyIsNullOrEmpty(string? messageBody)
    {
        // arrange
        var messageRequest = new SendMessageRequest(SqsQueueUrl, messageBody);

        // act, assert
        Assert.ThrowsAsync<AmazonClientException>(() =>
            ExtendedSqsWithLargePayloadEnabled.SendMessageAsync(messageRequest));
    }

    [Test]
    public async Task PayloadIsStoredInS3WhenSendingLargeMessage()
    {
        // arrange
        var messageRequest = new SendMessageRequest(SqsQueueUrl, LargeMessageBody);

        // act
        await ExtendedSqsWithLargePayloadEnabled.SendMessageAsync(messageRequest);

        // assert
        await PayloadStoreSub.Received(1).StorePayloadAsync(LargeMessageBody, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PayloadIsStoredInS3WhenSendingSmallMessageAndAlwaysThroughS3IsEnabled()
    {
        // arrange
        var messageRequest = new SendMessageRequest(SqsQueueUrl, SmallMessageBody);
        var options = Options.Create(ExtendedClientConfiguration.WithLargePayloadSupportEnabled().WithAlwaysThroughS3(true));
        var client = new Client.AmazonSqsExtendedClient(SqsClientSub, PayloadStoreSub, options, DummyLogger);

        // act
        await client.SendMessageAsync(messageRequest);

        // assert
        await PayloadStoreSub.Received(1).StorePayloadAsync(SmallMessageBody, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PayloadIsNotStoredInS3WhenSendingSmallMessage()
    {
        // arrange
        var messageRequest = new SendMessageRequest(SqsQueueUrl, SmallMessageBody);

        // act
        await ExtendedSqsWithLargePayloadEnabled.SendMessageAsync(messageRequest);

        // assert
        Assert.Multiple(async () =>
        {
            await PayloadStoreSub.DidNotReceiveWithAnyArgs().StorePayloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
            await PayloadStoreSub.DidNotReceiveWithAnyArgs().StorePayloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task PayloadIsNotStoredInS3WhenSendingLargeMessageAndLargePayloadSupportIsDisabled()
    {
        // arrange
        var messageRequest = new SendMessageRequest(SqsQueueUrl, SmallMessageBody);

        // act
        await ExtendedSqsWithLargePayloadDisabled.SendMessageAsync(messageRequest);

        // assert
        Assert.Multiple(async () =>
        {
            await PayloadStoreSub.DidNotReceiveWithAnyArgs().StorePayloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
            await PayloadStoreSub.DidNotReceiveWithAnyArgs().StorePayloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        });
    }
}
